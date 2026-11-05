using Memories.Gameplay.Memories;
using Memories.Contracts;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Interaction/Reaction Interactable")]
    public sealed class ReactionInteractable : MonoBehaviour, IObjectStateParticipant
    {
        [SerializeField] private ReactionGraph graph;
        [SerializeField] private bool initiallyInteractable = true;
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
        private ReactionRunner runner;
        public string StateKey => "reactionHints";
        public string CaptureState() => runner?.CaptureHints() ?? "";
        public void RestoreState(string state) => runner?.RestoreHints(graph, state);
        public string Prompt => prompt;
        public bool CanInteract => isActiveAndEnabled && interactionEnabled && runner != null && runner.CanRun(graph);
        public bool IsRunning => runner != null && runner.IsRunning;

        public void Configure(ReactionGraph reactionGraph, string interactionPrompt)
        {
            graph = reactionGraph;
            prompt = interactionPrompt;
        }

        public void Initialize(StoryTextPresenter texts, MemoryPresenter memories, ILocationTransition locations = null, QuestInventory inventory = null, CustomVariableStore variables = null)
        {
            SetInteractionEnabled(initiallyInteractable);
            runner?.Dispose();
            runner = new ReactionRunner(texts, memories, this, locations, inventory, variables, objectBindings, customReactions);
            RestorePickupState(inventory);
        }
        private void RestorePickupState(QuestInventory inventory)
        {
            if (inventory == null || graph == null || !graph.TryGetSequence(out var actions, out _)) return;
            // Restore only a pickup that explicitly hides itself after granting the item.
            // Other item rewards must not hide the object that grants them.
            foreach (var action in actions)
            {
                if (action.Kind != ReactionKind.AddInventoryItem || !inventory.WasCollected(action.Item.Id)) continue;
                if (!(ReactionGraph.Next(action) is ReactionNode hide) ||
                    hide.Kind != ReactionKind.ObjectActivity || hide.Active) continue;
                if (ReactionObjectBinding.TryResolve(objectBindings, hide.ObjectKey, out var target) && target == gameObject)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }
        }
        public void Interact()
        {
            if (CanInteract) runner.TryRun(graph);
        }
        private void OnDisable()
        {
            // A pickup may hide itself before finishing its story-text branch.
            if (runner != null && !runner.IsHidingOwner) runner.Stop();
        }
        private void OnDestroy()
        {
            runner?.Dispose();
        }
    }
}
