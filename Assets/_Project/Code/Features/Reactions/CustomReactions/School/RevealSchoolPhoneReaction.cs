using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Явно вызывается при открытии четвёртого воспоминания.
    public sealed class RevealSchoolPhoneReaction : CustomReaction
    {
        [SerializeField] private ReactionInteractable phone;
        [SerializeField] private ReactionInteractable coin;
        [SerializeField] private QuestItem coinItem;
        private QuestInventory inventory;
        public void Initialize(QuestInventory value) => inventory = value;
        public override void Execute()
        {
            phone.SetInteractionEnabled(true);
            phone.gameObject.SetActive(true);
            phone.GetComponent<ObjectStateTracker>()?.Capture();
            if (inventory != null && !inventory.WasCollected(coinItem.Id))
            {
                coin.SetInteractionEnabled(true);
                coin.gameObject.SetActive(true);
                coin.GetComponent<ObjectStateTracker>()?.Capture();
            }
        }
    }
}
