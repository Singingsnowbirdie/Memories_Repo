using Memories.Gameplay.Memories;
using Memories.Contracts;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Interaction/Reaction Interactable")]
    public sealed class ReactionInteractable : Interactable
    {
        [SerializeField] private ReactionGraph graph;
        [SerializeField] private string prompt = "Взаимодействовать";
        [SerializeField, InspectorName("Объекты графа")] private ReactionObjectBinding[] objectBindings = new ReactionObjectBinding[0];
        private ReactionRunner runner;
        public override string Prompt => prompt;
        public override bool CanInteract => base.CanInteract && runner != null && runner.CanRun(graph);
        public bool IsRunning => runner != null && runner.IsRunning;

        public void Configure(ReactionGraph reactionGraph, string interactionPrompt)
        {
            graph = reactionGraph;
            prompt = interactionPrompt;
        }

        public void Initialize(StoryTextPresenter texts, MemoryPresenter memories, ILocationTransition locations = null, QuestInventory inventory = null, CustomVariableStore variables = null)
        {
            runner?.Dispose();
            runner = new ReactionRunner(texts, memories, this, locations, inventory, variables, objectBindings);
            RestorePickupState(inventory);
        }
        private void RestorePickupState(QuestInventory inventory)
        {
            if (inventory == null || graph == null || !graph.TryGetSequence(out var actions, out _)) return;
            // Restore only a pickup that explicitly hides itself after granting the item.
            // Other item rewards must not hide the object that grants them.
            foreach (var action in actions)
            {
                if (action.Kind != ReactionKind.AddInventoryItem || !inventory.Contains(action.Item.Id)) continue;
                if (!(ReactionGraph.Next(action) is ReactionNode hide) ||
                    hide.Kind != ReactionKind.ObjectActivity || hide.Active) continue;
                if (ReactionObjectBinding.TryResolve(objectBindings, hide.ObjectKey, out var target) && target == gameObject)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }
        }
        public override void Interact()
        {
            if (CanInteract) runner.TryRun(graph);
        }
        private void OnDisable()
        {
            // A pickup may hide itself before finishing its story-text branch.
            if (runner != null && !runner.IsHidingOwner) runner.Stop();
        }
        private void OnDestroy() => runner?.Dispose();
    }
}
