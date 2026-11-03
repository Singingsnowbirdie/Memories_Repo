using Memories.Gameplay.Reactions;
using UnityEngine;
using UnityEngine.Events;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;

namespace Memories.Gameplay.Interaction
{
    public sealed class BaseballPlayArea : Interactable
    {
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private QuestItem bat;
        [SerializeField] private Renderer surface;
        [SerializeField] private Material readyMaterial;
        [SerializeField] private UnityEvent onInteract = new UnityEvent();
        [SerializeField] private ReactionGraph reactionGraph;
        [SerializeField] private string memoryId;
        [SerializeField] private GameObject intactWindow;
        [SerializeField] private GameObject brokenWindow;
        private MemoryPresenter memories;
        private ReactionRunner runner;
        private QuestInventory inventory;
        private CustomVariableStore variables;
        private Material originalMaterial;
        private bool highlighted;

        public override string Prompt => "Играть в бейсбол";
        public bool IsReady => inventory != null && variables != null && variableBank != null && bat != null &&
            variables.Get(variableBank, "ben_received_cap") &&
            variables.Get(variableBank, "ben_received_baseball") && inventory.Contains(bat.Id);
        public override bool CanInteract => base.CanInteract && IsReady &&
            (runner == null || runner.CanRun(reactionGraph)) &&
            (memories == null || !memories.HasCollected(memoryId));

        public void Initialize(QuestInventory questInventory, CustomVariableStore variableStore,
            StoryTextPresenter texts = null, MemoryPresenter memoryPresenter = null)
        {
            runner?.Dispose();
            memories = memoryPresenter;
            runner = reactionGraph != null ? new ReactionRunner(texts, memories, this,
                inventory: questInventory, variables: variableStore) : null;
            inventory = questInventory;
            variables = variableStore;
            if (surface != null && originalMaterial == null) originalMaterial = surface.sharedMaterial;
            Update();
        }

        private void Update()
        {
            if (memories != null && !string.IsNullOrWhiteSpace(memoryId))
            {
                bool completed = memories.HasCollected(memoryId);
                if (completed && bat != null && inventory != null && inventory.Contains(bat.Id))
                    inventory.TryConsume(bat);
                if (intactWindow != null && intactWindow.activeSelf == completed) intactWindow.SetActive(!completed);
                if (brokenWindow != null && brokenWindow.activeSelf != completed) brokenWindow.SetActive(completed);
            }
            bool ready = IsReady;
            if (surface == null || readyMaterial == null || ready == highlighted) return;
            surface.sharedMaterial = ready ? readyMaterial : originalMaterial;
            highlighted = ready;
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            if (runner != null && !runner.TryRun(reactionGraph)) return;
            onInteract.Invoke();
        }
        private void OnDisable() => runner?.Stop();
        private void OnDestroy() => runner?.Dispose();
    }
}
