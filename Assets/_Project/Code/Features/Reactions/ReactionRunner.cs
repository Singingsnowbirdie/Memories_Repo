using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Playback and one-shot state belong to the invoking object, never to the shared asset.
    public sealed class ReactionRunner : IDisposable
    {
        private readonly StoryTextPresenter texts;
        private readonly MemoryPresenter memories;
        private readonly UnityEngine.Object owner;
        private CancellationTokenSource playback;
        private bool fired;
        public bool IsRunning => playback != null;
        public ReactionRunner(StoryTextPresenter texts, MemoryPresenter memories, UnityEngine.Object owner)
        { this.texts = texts; this.memories = memories; this.owner = owner; }
        public bool CanRun(ReactionGraph graph) => graph != null && !IsRunning &&
            !(graph.RunOnce && fired) && texts != null && memories != null && memories.CanStartPlayback;
        public bool TryRun(ReactionGraph graph)
        {
            if (!CanRun(graph)) return false;
            if (!graph.TryGetSequence(out var actions, out var error))
            { Debug.LogWarning("Цепочка реакций не запущена: " + error, owner); return false; }
            fired = true;
            playback = new CancellationTokenSource();
            RunAsync(actions, playback).Forget();
            return true;
        }
        public void Stop() => playback?.Cancel();
        public void Dispose() => Stop();
        private async UniTask RunAsync(List<ReactionNode> actions, CancellationTokenSource source)
        {
            bool ownsMemory = false;
            try
            {
                foreach (var action in actions)
                {
                    source.Token.ThrowIfCancellationRequested();
                    if (action.Kind == ReactionKind.StoryText)
                        await texts.ShowAsync(action.TextGraph, source.Token);
                    else
                    {
                        // Other triggers may already be showing a memory; keep sequence order.
                        await UniTask.WaitUntil(() => memories.CanOpen(action.MemoryId, true), cancellationToken: source.Token);
                        if (!memories.Open(action.MemoryId, action.MemoryGraph, true)) return;
                        ownsMemory = true;
                        await UniTask.WaitUntil(() => !memories.IsPlaying, cancellationToken: source.Token);
                        ownsMemory = false;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception, owner); }
            finally
            {
                if (ownsMemory) memories.Close();
                if (playback == source) playback = null;
                source.Dispose();
            }
        }

    }
}
