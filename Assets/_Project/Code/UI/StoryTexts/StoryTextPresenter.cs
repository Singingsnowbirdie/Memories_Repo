using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Story;

namespace Memories.Gameplay.UI
{
    public sealed class StoryTextPresenter : IDisposable
    {
        private readonly IStoryTextView view;
        private CancellationTokenSource current;
        private bool disposed;

        public bool IsPlaying => current != null;

        public StoryTextPresenter(IStoryTextView view) => this.view = view;

        public UniTask ShowAsync(StoryTextGraph graph, CancellationToken token = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(StoryTextPresenter));
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (!graph.TryGetSequence(out var fragments, out var error))
                throw new ArgumentException(error, nameof(graph));
            return ShowAsync(fragments, token, graph.PauseBetweenFragments);
        }

        public UniTask ShowAsync(string text, float duration = 3f, CancellationToken token = default)
            => ShowAsync(new[] { new StoryTextFragment(text, duration) }, token);

        // A new request replaces the active sequence. Invalid requests leave it untouched.
        public async UniTask ShowAsync(IReadOnlyList<StoryTextFragment> fragments, CancellationToken token = default,
            float pauseBetweenFragments = 0)
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
            Stop();
            using var playback = CancellationTokenSource.CreateLinkedTokenSource(token, view.LifetimeToken);
            current = playback;
            try
            {
                await view.ShowAsync(snapshot[0].Text, playback.Token);
                for (int i = 0; i < snapshot.Length; i++)
                {
                    if (i > 0)
                    {
                        if (pauseBetweenFragments > 0)
                        {
                            await view.ChangeTextAsync(string.Empty, playback.Token);
                            await UniTask.Delay(TimeSpan.FromSeconds(pauseBetweenFragments), cancellationToken: playback.Token);
                        }
                        await view.ChangeTextAsync(snapshot[i].Text, playback.Token);
                    }
                    // Read time starts after the text is fully visible and freezes with the game.
                    await UniTask.Delay(TimeSpan.FromSeconds(snapshot[i].Duration), cancellationToken: playback.Token);
                }
                await view.HideAsync(playback.Token);
            }
            finally
            {
                // A cancelled older request must never hide a newer one.
                if (ReferenceEquals(current, playback))
                {
                    current = null;
                    view.HideImmediately();
                }
            }
        }

        public void Stop()
        {
            var previous = current;
            current = null;
            previous?.Cancel();
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
