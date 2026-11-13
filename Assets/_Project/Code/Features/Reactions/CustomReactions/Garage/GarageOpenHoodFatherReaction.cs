using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class GarageOpenHoodFatherReaction : CustomReaction
    {
        [SerializeField] private GameObject waitingFather;
        [SerializeField] private GameObject workingFather;
        [SerializeField] private SetCarHoodReaction hood;
        [SerializeField] private CustomVariableBank variables;

        public override void Execute() => ShowWorkingFather();

        private void ShowWorkingFather()
        {
            hood.SetOpen(true);
            workingFather.SetActive(true);
            waitingFather.SetActive(false);
        }

        // Вызывается один раз после восстановления состояний объектов и предыдущих этапов отца.
        public void Restore(CustomVariableStore store, MemoryCollection memories)
        {
            if (store.Get(variables, "father_hood_items_delivered") || memories.Contains("garage_third_memory"))
                ShowWorkingFather();
            else
                workingFather.SetActive(false);
        }
    }
}
