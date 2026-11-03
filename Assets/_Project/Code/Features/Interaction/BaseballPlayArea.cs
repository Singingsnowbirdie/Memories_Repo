using Memories.Gameplay.Reactions;
using UnityEngine;
using UnityEngine.Events;

namespace Memories.Gameplay.Interaction
{
    public sealed class BaseballPlayArea : Interactable
    {
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private QuestItem bat;
        [SerializeField] private Renderer surface;
        [SerializeField] private Material readyMaterial;
        [SerializeField] private UnityEvent onInteract = new UnityEvent();
        private QuestInventory inventory;
        private CustomVariableStore variables;
        private Material originalMaterial;
        private bool highlighted;

        public override string Prompt => "Играть в бейсбол";
        public bool IsReady => inventory != null && variables != null && variableBank != null && bat != null &&
            variables.Get(variableBank, "ben_received_cap") &&
            variables.Get(variableBank, "ben_received_baseball") && inventory.Contains(bat.Id);
        public override bool CanInteract => base.CanInteract && IsReady;

        public void Initialize(QuestInventory questInventory, CustomVariableStore variableStore)
        {
            inventory = questInventory;
            variables = variableStore;
            if (surface != null && originalMaterial == null) originalMaterial = surface.sharedMaterial;
            Update();
        }

        private void Update()
        {
            bool ready = IsReady;
            if (surface == null || readyMaterial == null || ready == highlighted) return;
            surface.sharedMaterial = ready ? readyMaterial : originalMaterial;
            highlighted = ready;
        }

        public override void Interact()
        {
            if (CanInteract) onInteract.Invoke();
        }
    }
}
