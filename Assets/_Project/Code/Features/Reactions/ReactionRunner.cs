using System;
using System.Linq;
using Memories.Gameplay.Interaction;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
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
        private readonly ILocationTransition locations;
        private readonly QuestInventory inventory;
        private CancellationTokenSource playback;
        private bool fired;
        public bool IsRunning => playback != null;
        public ReactionRunner(StoryTextPresenter texts, MemoryPresenter memories, UnityEngine.Object owner,
            ILocationTransition locations = null, QuestInventory inventory = null)
        { this.texts = texts; this.memories = memories; this.owner = owner; this.locations = locations; this.inventory = inventory; }
        public bool CanRun(ReactionGraph graph) => graph != null && !IsRunning &&
            !(graph.RunOnce && fired) && texts != null && memories != null && memories.CanStartPlayback;
        public bool TryRun(ReactionGraph graph)
        {
            if (!CanRun(graph)) return false;
            if (!graph.TryGetSequence(out var actions, out var error))
            { Debug.LogWarning("Цепочка реакций не запущена: " + error, owner); return false; }
            if (inventory == null && graph.nodes.OfType<InventoryConditionNode>().Any())
            { Debug.LogWarning("Для проверки предмета нужен инвентарь игрока.", owner); return false; }
            foreach (var action in actions)
            {
                if (action.Kind == ReactionKind.RevealPhoto && !(owner is PhotoMemory)) return false;
                if (action.Kind == ReactionKind.ChangeLocation &&
                    (locations == null || !locations.CanChangeLocation(action.LocationScenePath))) return false;
            }
            fired = true;
            playback = new CancellationTokenSource();
            RunAsync(graph, playback).Forget();
            return true;
        }
        public void Stop() => playback?.Cancel();
        public void Dispose() => Stop();
        private async UniTask RunAsync(ReactionGraph graph, CancellationTokenSource source)
        {
            bool ownsMemory = false;
            try
            {
                var node = ReactionGraph.Next(graph.nodes.OfType<ReactionStartNode>().Single());
                while (!(node is ReactionEndNode))
                {
                    source.Token.ThrowIfCancellationRequested();
                    if (node is InventoryConditionNode condition)
                    {
                        node = ReactionGraph.Next(condition, condition.SelectOutput(inventory));
                        continue;
                    }
                    var action = (ReactionNode)node;
                    if (action.Kind == ReactionKind.RevealPhoto)
                        ((global::Memories.Gameplay.Interaction.PhotoMemory)owner).Remember();
                    else if (action.Kind == ReactionKind.StoryText)
                        await texts.ShowAsync(action.TextGraph, source.Token);
                    else if (action.Kind == ReactionKind.ChangeLocation)
                    {
                        await locations.ChangeLocationAsync(action.LocationScenePath);
                        return;
                    }
                    else
                    {
                        // Other triggers may already be showing a memory; keep sequence order.
                        await UniTask.WaitUntil(() => memories.CanOpen(action.MemoryId, true), cancellationToken: source.Token);
                        if (!memories.Open(action.MemoryId, action.MemoryGraph, true)) return;
                        ownsMemory = true;
                        await UniTask.WaitUntil(() => !memories.IsPlaying, cancellationToken: source.Token);
                        ownsMemory = false;
                    }
                    node = ReactionGraph.Next(node);
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
