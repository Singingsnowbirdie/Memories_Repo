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
        private readonly CustomVariableStore variables;
        private readonly ReactionObjectBinding[] objectBindings;
        private CancellationTokenSource playback;
        private bool fired;
        public bool IsRunning => playback != null;
        public ReactionRunner(StoryTextPresenter texts, MemoryPresenter memories, UnityEngine.Object owner,
            ILocationTransition locations = null, QuestInventory inventory = null, CustomVariableStore variables = null, ReactionObjectBinding[] objectBindings = null)
        { this.texts = texts; this.memories = memories; this.owner = owner; this.locations = locations; this.inventory = inventory; this.variables = variables; this.objectBindings = objectBindings; }
        public bool CanRun(ReactionGraph graph) => graph != null && !IsRunning &&
            !(graph.RunOnce && fired) && (memories == null || memories.CanStartPlayback);
        public bool TryRun(ReactionGraph graph)
        {
            if (!CanRun(graph)) return false;
            if (!graph.TryGetSequence(out var actions, out var error))
            { Debug.LogWarning("Цепочка реакций не запущена: " + error, owner); return false; }
            if (inventory == null && (graph.nodes.OfType<ReactionConditionNode>().Any(condition => condition.RequiresInventory) || actions.Any(action => action.Kind == ReactionKind.AddInventoryItem)))
            { Debug.LogWarning("Для проверки предмета нужен инвентарь игрока.", owner); return false; }
            if (variables == null && (graph.nodes.OfType<ReactionConditionNode>().Any(condition => condition.Kind == ReactionConditionKind.CustomVariable) ||
                actions.Any(action => action.Kind == ReactionKind.SetCustomVariable)))
            { Debug.LogWarning("Хранилище кастомных переменных не подключено.", owner); return false; }
            foreach (var action in actions)
            {
                if (action.Kind == ReactionKind.StoryText && texts == null) return false;
                if (action.Kind == ReactionKind.Memory && memories == null) return false;
                if (action.Kind == ReactionKind.CustomReaction && !CustomReaction.TryFind(owner, out _))
                { Debug.LogWarning("На объекте нужен ровно один включённый компонент CustomReaction.", owner); return false; }
                if (action.Kind == ReactionKind.ChangeLocation &&
                    (locations == null || !locations.CanChangeLocation(action.LocationScenePath))) return false;
            }
            foreach (var activity in actions.Where(action => action.Kind == ReactionKind.ObjectActivity))
                if (!ReactionObjectBinding.TryResolve(objectBindings, activity.ObjectKey, out _))
                { Debug.LogWarning("Не назначен объект графа или имя повторяется: " + activity.ObjectKey, owner); return false; }
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
                    if (node is ReactionConditionNode condition)
                    {
                        node = ReactionGraph.Next(condition, condition.SelectOutput(inventory, variables));
                        continue;
                    }
                    var action = (ReactionNode)node;
                    if (action.Kind == ReactionKind.ObjectActivity)
                    {
                        if (!ReactionObjectBinding.TryResolve(objectBindings, action.ObjectKey, out var target))
                            throw new InvalidOperationException("Объект графа недоступен: " + action.ObjectKey);
                        target.SetActive(action.Active);
                        node = ReactionGraph.Next(node);
                        continue;
                    }
                    if (action.Kind == ReactionKind.CustomReaction)
                    {
                        if (!CustomReaction.TryFind(owner, out var customReaction))
                            throw new InvalidOperationException("Компонент CustomReaction недоступен на объекте.");
                        customReaction.Execute();
                    }
                    else if (action.Kind == ReactionKind.AddInventoryItem)
                        inventory.TryAdd(action.Item);
                    else if (action.Kind == ReactionKind.SetCustomVariable)
                        variables.Set(action.VariableBank, action.VariableId, action.VariableValue);
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
