using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class GarageHoodFatherMemoryReaction : CustomReaction
    {
        public const string MemoryId = "garage_second_memory";
        [SerializeField] private MemoryGraph memory;
        [SerializeField] private GameObject kneelingFather;
        [SerializeField] private GameObject hoodFather;
        private MemoryPresenter presenter;

        public void Initialize(MemoryPresenter memoryPresenter) => presenter = memoryPresenter;

        // Called once after saved object states have been bound, including older saves.
        public void Restore(MemoryCollection collection)
        {
            if (collection.Contains(MemoryId)) ShowHoodFather();
        }

        public override void Execute()
        {
            // The comic covers the switch. This component lives outside either father.
            if (presenter == null || !presenter.Open(MemoryId, memory, true)) return;
            ShowHoodFather();
        }

        private void ShowHoodFather()
        {
            hoodFather.SetActive(true);
            kneelingFather.SetActive(false);
        }
    }
}
