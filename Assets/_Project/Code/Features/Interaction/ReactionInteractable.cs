using Memories.Gameplay.Memories;
using Memories.Contracts;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Interaction/Reaction Interactable")]
    public sealed class ReactionInteractable : MonoBehaviour, IConditionalObjectStateParticipant
    {
        [SerializeField] private ReactionGraph graph;
        [SerializeField] private bool initiallyInteractable = true;
        [SerializeField] private bool initiallyVisible = true;
        [SerializeField] private string prompt = "Взаимодействовать";
        [SerializeField, InspectorName("Объекты графа")] private ReactionObjectBinding[] objectBindings = new ReactionObjectBinding[0];
        [SerializeField, InspectorName("Кастомные реакции")] private CustomReactionBinding[] customReactions = new CustomReactionBinding[0];
        private bool interactionEnabled = true;
        public bool InteractionEnabled => interactionEnabled;
        public void SetInteractionEnabled(bool value)
        {
            interactionEnabled = value;
            GetComponent<ObjectStateTracker>()?.Capture();
        }
        private ReactionManager reactions;
        public string StateKey => "reactionHints";
        public bool RequiresPersistence => RequiresSavedState(graph, new System.Collections.Generic.HashSet<ReactionGraph>());

        private bool RequiresSavedState(ReactionGraph candidate, System.Collections.Generic.HashSet<ReactionGraph> visited)
        {
            if (candidate == null || !visited.Add(candidate)) return false;
            foreach (var node in candidate.nodes)
            {
                if (!(node is ReactionNode action)) continue;
                if (action.Kind == ReactionKind.StartHints) return true;
                if (action.Kind == ReactionKind.RunGraph && RequiresSavedState(action.ReactionGraph, visited)) return true;
                if (action.Kind != ReactionKind.ObjectActivity && action.Kind != ReactionKind.ObjectInteractivity) continue;
                if (action.Kind == ReactionKind.ObjectInteractivity && string.IsNullOrEmpty(action.ObjectKey)) return true;
                if (ReactionObjectBinding.TryResolve(objectBindings, action.ObjectKey, out var target) && target == gameObject)
                    return true;
            }
            return false;
        }

        public string CaptureState() => reactions?.CaptureHints(this) ?? "";
        public void RestoreState(string state) => reactions?.RestoreHints(this, state);
        public string Prompt => prompt;
        public bool CanInteract => isActiveAndEnabled && interactionEnabled && reactions != null && reactions.CanRun(this);
        public bool IsRunning => reactions != null && reactions.IsRunning(this);

        public void Configure(ReactionGraph reactionGraph, string interactionPrompt)
        {
            graph = reactionGraph;
            prompt = interactionPrompt;
        }

        public void Initialize(ReactionManager reactions, QuestInventory inventory)
        {
            SetInteractionEnabled(initiallyInteractable);
            this.reactions = reactions;
            reactions.Bind(this, graph, objectBindings, customReactions);
            if (!initiallyVisible) gameObject.SetActive(false);
            if (IsCollectedPickup(inventory)) gameObject.SetActive(false);
        }
        // Вызывается один раз после восстановления состояний объектов, в том числе из старых сохранений,
        // где недоступные для подбора предметы ещё были видны. Во время игры объекты появляются по действиям графа.
        public void RestoreInitialVisibility(QuestInventory inventory)
        {
            if (initiallyVisible) return;
            // Показанный объект впоследствии может потерять интерактивность, оставаясь видимым.
            if (GetComponent<ObjectStateTracker>()?.HasRestoredState == true && !IsCollectedPickup(inventory)) return;
            gameObject.SetActive(false);
        }
        private bool IsCollectedPickup(QuestInventory inventory)
        {
            if (inventory == null || graph == null || !graph.TryGetSequence(out var actions, out _)) return false;
            // Восстанавливаем только предметы, которые явно скрывают себя после выдачи в инвентарь.
            // Выдача других предметов не должна скрывать объект, который их выдаёт.
            foreach (var action in actions)
            {
                if (action.Kind != ReactionKind.AddInventoryItem || !inventory.WasCollected(action.Item.Id)) continue;
                if (!(ReactionGraph.Next(action) is ReactionNode hide) ||
                    hide.Kind != ReactionKind.ObjectActivity || hide.Active) continue;
                if (ReactionObjectBinding.TryResolve(objectBindings, hide.ObjectKey, out var target) && target == gameObject)
                    return true;
            }
            return false;
        }
        public void Interact()
        {
            if (CanInteract) reactions.TryRun(this);
        }
    }
}
