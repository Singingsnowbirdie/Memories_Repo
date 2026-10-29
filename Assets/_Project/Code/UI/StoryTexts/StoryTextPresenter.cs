using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Story;
using UnityEngine;

namespace Memories.Gameplay.UI
{
    public sealed class StoryTextPresenter : IDisposable
    {
        private sealed class Request
        {
            public StoryTextFragment[] Fragments;
            public float Pause;
            public bool Uninterruptible;
            public CancellationTokenSource Cancellation;
            public CancellationTokenRegistration Registration;
            public readonly UniTaskCompletionSource Completion = new UniTaskCompletionSource();
        }

        private readonly IStoryTextView view;
        private readonly Queue<Request> pending = new Queue<Request>();
        private readonly HashSet<Request> requests = new HashSet<Request>();
        private Request current;
        private bool pumping, disposed;
        private bool panelVisible;
        private const float MinimumReadTimeBeforeInterruption = 2f;

        public bool IsPlaying
        {
            get
            {
                foreach (var request in requests)
                    if (!request.Cancellation.IsCancellationRequested) return true;
                return false;
            }
        }
        public bool BlocksInteractions => current != null && !current.Cancellation.IsCancellationRequested && current.Uninterruptible;

        public StoryTextPresenter(IStoryTextView view) => this.view = view;

        public UniTask ShowAsync(StoryTextGraph graph, CancellationToken token = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(StoryTextPresenter));
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (!graph.TryGetSequence(out var fragments, out var error))
                throw new ArgumentException(error, nameof(graph));
            return ShowAsync(fragments, token, graph.PauseBetweenFragments, graph.BlockInteractionsForWholeSequence);
        }

        public UniTask ShowAsync(string text, float duration = 3f, CancellationToken token = default)
            => ShowAsync(new[] { new StoryTextFragment(text, duration) }, token);

        public UniTask ShowAsync(IReadOnlyList<StoryTextFragment> fragments, CancellationToken token = default,
            float pauseBetweenFragments = 0, bool blockInteractionsForWholeSequence = false)
        {
            if (disposed) throw new ObjectDisposedException(nameof(StoryTextPresenter));
            if (float.IsNaN(pauseBetweenFragments) || float.IsInfinity(pauseBetweenFragments) || pauseBetweenFragments < 0)
                throw new ArgumentOutOfRangeException(nameof(pauseBetweenFragments));
            if (fragments == null) throw new ArgumentNullException(nameof(fragments));
            if (fragments.Count == 0) throw new ArgumentException("The sequence must contain at least one fragment.", nameof(fragments));
            var snapshot = new StoryTextFragment[fragments.Count];
            for (int i = 0; i < snapshot.Length; i++)
            {
                var fragment = fragments[i] ?? throw new ArgumentException("A fragment cannot be null.", nameof(fragments));
                fragment.Validate();
                snapshot[i] = new StoryTextFragment(fragment.Text, fragment.Duration);
            }
            token.ThrowIfCancellationRequested();
            view.LifetimeToken.ThrowIfCancellationRequested();
            var request = new Request
            {
                Fragments = snapshot, Pause = pauseBetweenFragments,
                Uninterruptible = blockInteractionsForWholeSequence,
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, view.LifetimeToken)
            };
            // Capture the task before the pump can complete a synchronous view operation.
            var task = request.Completion.Task.AttachExternalCancellation(request.Cancellation.Token);
            request.Registration = request.Cancellation.Token.Register(() =>
            {
                if (ReferenceEquals(current, request)) HideImmediately();
            });
            requests.Add(request);
            pending.Enqueue(request);
            if (!pumping) PumpAsync().Forget();
            return task;
        }

        private async UniTask PumpAsync()
        {
            pumping = true;
            try
            {
                while (pending.Count > 0) await PlayAsync(pending.Dequeue());
            }
            finally
            {
                pumping = false;
                current = null;
                HideImmediately();
            }
        }

        private async UniTask PlayAsync(Request request)
        {
            var parent = current;
            current = request;
            var token = request.Cancellation.Token;
            Exception failure = null;
            try
            {
                token.ThrowIfCancellationRequested();
                for (int i = 0; i < request.Fragments.Length; i++)
                {
                    if (panelVisible) await view.ChangeTextAsync(request.Fragments[i].Text, token);
                    else await view.ShowAsync(request.Fragments[i].Text, token);
                    panelVisible = true;
                    float elapsed = 0;
                    while (elapsed < request.Fragments[i].Duration)
                    {
                        token.ThrowIfCancellationRequested();
                        if (!request.Uninterruptible && pending.Count > 0 && elapsed >= MinimumReadTimeBeforeInterruption) break;
                        await UniTask.NextFrame(cancellationToken: token);
                        elapsed += Time.deltaTime;
                    }
                    // Hand off the existing panel without sliding it out and back in.
                    while (i < request.Fragments.Length - 1 && !request.Uninterruptible && pending.Count > 0)
                    {
                        token.ThrowIfCancellationRequested();
                        await PlayAsync(pending.Dequeue());
                    }
                    if (i == request.Fragments.Length - 1) continue;
                    if (request.Pause <= 0 && (request.Uninterruptible || pending.Count == 0)) continue;
                    await view.ChangeTextAsync(string.Empty, token);
                    float remaining = request.Pause;
                    while (remaining > 0 || (!request.Uninterruptible && pending.Count > 0))
                    {
                        token.ThrowIfCancellationRequested();
                        if (!request.Uninterruptible && pending.Count > 0)
                        {
                            // Only one playback owns the view. Freeze the parent's remaining pause.
                            await PlayAsync(pending.Dequeue());
                            token.ThrowIfCancellationRequested();
                            await view.ChangeTextAsync(string.Empty, token);
                            continue;
                        }
                        await UniTask.NextFrame(cancellationToken: token);
                        remaining -= Time.deltaTime;
                    }
                }
                if (parent == null && pending.Count == 0)
                {
                    await view.HideAsync(token);
                    panelVisible = false;
                }
            }
            catch (Exception exception) { failure = exception; }
            finally
            {
                if (failure != null) HideImmediately();
                requests.Remove(request);
                request.Registration.Dispose();
                request.Cancellation.Dispose();
                current = parent;
            }
            if (failure is OperationCanceledException) request.Completion.TrySetCanceled(token);
            else if (failure != null) request.Completion.TrySetException(failure);
            else request.Completion.TrySetResult();
        }

        public void Stop()
        {
            // Cancels active, suspended and queued requests, including their waiting reaction runners.
            foreach (var request in new List<Request>(requests)) request.Cancellation.Cancel();
            HideImmediately();
        }

        private void HideImmediately()
        {
            panelVisible = false;
            view.HideImmediately();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Stop();
        }
    }
}
