using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class GarageFinalMemoryReaction : CustomReaction
    {
        [SerializeField] private GameObject workingFather;
        [SerializeField] private GameObject finalFather;
        [SerializeField] private OpenGarageGateReaction gate;
        [SerializeField] private QuestItem[] deliveredItems;

        public override void Execute()
        {
            finalFather.SetActive(true);
            workingFather.SetActive(false);
        }

        // Restore once after earlier father stages; never replay the memory on load.
        public void Restore(QuestInventory inventory, MemoryCollection memories)
        {
            bool delivered = deliveredItems != null && deliveredItems.Length == 3;
            if (delivered)
                foreach (var item in deliveredItems)
                    delivered &= item != null && inventory.IsConsumed(item.Id);
            if (delivered || memories.Contains("garage_final_memory"))
            {
                finalFather.SetActive(true);
                workingFather.SetActive(false);
                gate.RestoreOpen();
            }
            else finalFather.SetActive(false);
        }
    }
}
