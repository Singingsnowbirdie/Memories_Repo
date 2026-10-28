using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    [AddComponentMenu("Memories/Interaction/Quest Item Pickup")]
    public sealed class QuestItemPickup : Interactable
    {
        [SerializeField] private QuestItem item;
        private QuestInventory inventory;
        public override string Prompt => item != null ? "Взять: " + item.DisplayName : "Взять";
        public override bool CanInteract => base.CanInteract && inventory != null && item != null &&
            item.IsValid && !inventory.Contains(item.Id);

        public void Initialize(QuestInventory inventory)
        {
            this.inventory = inventory;
            if (item != null && inventory.Contains(item.Id)) gameObject.SetActive(false);
        }

        public override void Interact()
        {
            if (CanInteract && inventory.TryAdd(item)) gameObject.SetActive(false);
        }
    }
}
