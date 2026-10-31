using System;
using System.Collections.Generic;
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
        private readonly CustomReactionBinding[] customReactions;
        private CancellationTokenSource playback;
        private bool fired;
        private ReactionRunner hintOwner;
        private readonly Dictionary<string, CancellationTokenSource> hintTimers = new Dictionary<string, CancellationTokenSource>();
        public bool IsRunning => playback != null;
        public bool IsHidingOwner { get; private set; }
        public ReactionRunner(StoryTextPresenter texts, MemoryPresenter memories, UnityEngine.Object owner,
            ILocationTransition locations = null, QuestInventory inventory = null, CustomVariableStore variables = null, ReactionObjectBinding[] objectBindings = null, CustomReactionBinding[] customReactions = null)
        { this.texts = texts; this.memories = memories; this.owner = owner; this.locations = locations; this.inventory = inventory; this.variables = variables; this.objectBindings = objectBindings; this.customReactions = customReactions; hintOwner = this; }
        public bool CanRun(ReactionGraph graph) => graph != null && !IsRunning &&
            !(graph.RunOnce && fired) && (memories == null || memories.CanStartPlayback);
        public bool TryRun(ReactionGraph graph)
        {
            if (!CanRun(graph)) return false;
            if (!graph.TryGetSequence(out var actions, out var error))
            { Debug.LogWarning("Цепочка реакций не запущена: " + error, owner); return false; }
            if (inventory == null && (graph.nodes.OfType<ReactionConditionNode>().Any(condition => condition.RequiresInventory) || actions.Any(action => action.Kind == ReactionKind.AddInventoryItem || action.Kind == ReactionKind.ConsumeInventoryItem)))
            { Debug.LogWarning("Для проверки предмета нужен инвентарь игрока.", owner); return false; }
            if (variables == null && (graph.nodes.OfType<ReactionConditionNode>().Any(condition => condition.Kind == ReactionConditionKind.CustomVariable) ||
                actions.Any(action => action.Kind == ReactionKind.SetCustomVariable)))
            { Debug.LogWarning("Хранилище кастомных переменных не подключено.", owner); return false; }
            foreach (var action in actions)
            {
                if (action.Kind == ReactionKind.ObjectInteractivity && !TryGetInteractable(action, out _))
                { Debug.LogWarning("На выбранном объекте нужен компонент Interactable: " + action.ObjectKey, owner); return false; }
                if (action.Kind == ReactionKind.StoryText && texts == null) return false;
                if (action.Kind == ReactionKind.Memory && memories == null) return false;
                if (action.Kind == ReactionKind.Memory && action.FirstCompletionText != null && texts == null) return false;
                if (action.Kind == ReactionKind.CustomReaction && !TryGetCustomReaction(action, out _))
                { Debug.LogWarning("Назначьте включённый компонент CustomReaction в привязках реакций: " + action.ObjectKey, owner); return false; }
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
        private bool TryGetCustomReaction(ReactionNode action, out CustomReaction reaction)
        {
            return CustomReactionBinding.TryResolve(customReactions, action.ObjectKey, out reaction);
        }

        private bool TryGetInteractable(ReactionNode action, out Interactable interactable)
        {
            interactable = null;
            if (string.IsNullOrEmpty(action.ObjectKey))
                interactable = owner as Interactable;
            else if (ReactionObjectBinding.TryResolve(objectBindings, action.ObjectKey, out var target))
                interactable = target.GetComponent<Interactable>();
            return interactable != null;
        }

        public void Stop()
        {
            playback?.Cancel();
            if (hintOwner != this) return;
            var timers = hintTimers.Values.ToArray();
            hintTimers.Clear();
            foreach (var timer in timers) timer.Cancel();
        }
        public void Dispose() => Stop();

        private void StopHint(string id)
        {
            if (!hintTimers.TryGetValue(id, out var timer)) return;
            hintTimers.Remove(id);
            timer.Cancel();
        }

        private void StartHint(ReactionNode action)
        {
            if (hintTimers.ContainsKey(action.HintId)) return;
            var timer = new CancellationTokenSource();
            hintTimers.Add(action.HintId, timer);
            RunHintAsync(action.HintId, action.HintGraph, action.HintTimeout, action.RepeatHint, timer).Forget();
        }

        private async UniTask RunHintAsync(string id, ReactionGraph graph, float timeout, bool repeat,
            CancellationTokenSource timer)
        {
            ReactionRunner child = null;
            try
            {
                do
                {
                    // Scaled time freezes in pause and memory views.
                    await UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: timer.Token);
                    await UniTask.WaitUntil(() => !IsRunning && (texts == null || !texts.IsPlaying) &&
                        (memories == null || memories.CanStartPlayback) && Time.timeScale > 0,
                        cancellationToken: timer.Token);
                    timer.Token.ThrowIfCancellationRequested();
                    child = new ReactionRunner(texts, memories, owner, locations, inventory, variables, objectBindings, customReactions);
                    child.hintOwner = this;
                    // Each traversal evaluates graph conditions against the current inventory.
                    if (!child.TryRun(graph)) return;
                    await UniTask.WaitUntil(() => !child.IsRunning, cancellationToken: timer.Token);
                    child.Dispose();
                    child = null;
                } while (repeat);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception, owner); }
            finally
            {
                child?.Dispose();
                if (hintTimers.TryGetValue(id, out var current) && current == timer) hintTimers.Remove(id);
                timer.Dispose();
            }
        }
        private async UniTask RunAsync(ReactionGraph graph, CancellationTokenSource source)
        {
            bool ownsMemory = false;
            try
            {
                var node = ReactionGraph.Next(graph.nodes.OfType<ReactionStartNode>().Single());
                while (!(node is ReactionEndNode))
                {
                    source.Token.ThrowIfCancellationRequested();
                    if (node is ReactionRandomNode random)
                    {
                        node = ReactionGraph.Next(random, random.SelectOutput(UnityEngine.Random.value));
                        continue;
                    }
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
                        IsHidingOwner = !action.Active && owner is Component component && target == component.gameObject;
                        try
                        {
                            if (action.Active && !target.activeSelf && action.RevealDuration > 0 && action.RevealShader != null)
                                ObjectReveal.Play(target, action.RevealShader, action.RevealDuration, action.RevealDelay);
                            else target.SetActive(action.Active);
                        }
                        finally { IsHidingOwner = false; }
                        node = ReactionGraph.Next(node);
                        continue;
                    }
                    if (action.Kind == ReactionKind.ObjectInteractivity)
                    {
                        if (!TryGetInteractable(action, out var interactable))
                            throw new InvalidOperationException("Интерактивный объект недоступен: " + action.ObjectKey);
                        interactable.SetInteractionEnabled(action.InteractionEnabled);
                    }
                    else if (action.Kind == ReactionKind.StartHints)
                        hintOwner.StartHint(action);
                    else if (action.Kind == ReactionKind.StopHints)
                        hintOwner.StopHint(action.HintId);
                    else if (action.Kind == ReactionKind.CustomReaction)
                    {
                        if (!TryGetCustomReaction(action, out var customReaction))
                            throw new InvalidOperationException("Компонент CustomReaction недоступен на объекте.");
                        customReaction.Execute();
                    }
                    else if (action.Kind == ReactionKind.AddInventoryItem)
                        inventory.TryAdd(action.Item);
                    else if (action.Kind == ReactionKind.ConsumeInventoryItem)
                        inventory.TryConsume(action.Item);
                    else if (action.Kind == ReactionKind.SetCustomVariable)
                    {
                        if (action.VariableBank.Find(action.VariableId).Type == CustomVariableType.Integer)
                            variables.Set(action.VariableBank, action.VariableId, action.AddToIntValue
                                ? checked(variables.GetInt(action.VariableBank, action.VariableId) + action.VariableIntValue)
                                : action.VariableIntValue);
                        else variables.Set(action.VariableBank, action.VariableId, action.VariableValue);
                    }
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
                        bool alreadyCollected = memories.HasCollected(action.MemoryId);
                        if (!memories.Open(action.MemoryId, action.MemoryGraph, true)) return;
                        ownsMemory = true;
                        await UniTask.WaitUntil(() => !memories.IsPlaying, cancellationToken: source.Token);
                        ownsMemory = false;
                        if (!alreadyCollected && memories.HasCollected(action.MemoryId) && action.FirstCompletionText != null)
                            await texts.ShowAsync(action.FirstCompletionText, source.Token);
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
