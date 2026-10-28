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
        private ReactionRunner runner;
        public override string Prompt => prompt;
        public override bool CanInteract => base.CanInteract && runner != null && runner.CanRun(graph);
        public bool IsRunning => runner != null && runner.IsRunning;

        public void Configure(ReactionGraph reactionGraph, string interactionPrompt)
        {
            graph = reactionGraph;
            prompt = interactionPrompt;
        }

        public void Initialize(StoryTextPresenter texts, MemoryPresenter memories, ILocationTransition locations = null, QuestInventory inventory = null)
        {
            runner?.Dispose();
            runner = new ReactionRunner(texts, memories, this, locations, inventory);
        }
        public override void Interact()
        {
            if (CanInteract) runner.TryRun(graph);
        }
        private void OnDisable() => runner?.Stop();
    }
}
