using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class GarageFatherMemoryReaction : CustomReaction
    {
        public const string MemoryId = "garage_first_memory";
        [SerializeField] private MemoryGraph memory;
        [SerializeField] private GameObject standingFather;
        [SerializeField] private GameObject kneelingFather;
        private MemoryPresenter presenter;

        public void Initialize(MemoryPresenter memoryPresenter, MemoryCollection collection)
        {
            presenter = memoryPresenter;
            // Restore completed progress once; live changes are invoked only by the graph.
            bool completed = collection.Contains(MemoryId);
            standingFather.SetActive(!completed);
            kneelingFather.SetActive(completed);
        }

        public override void Execute()
        {
            // Open shows the full-screen comic before changing the scene behind it.
            // This reaction lives outside the father, so hiding him cannot destroy playback.
            if (presenter == null || !presenter.Open(MemoryId, memory, true)) return;
            kneelingFather.SetActive(true);
            standingFather.SetActive(false);
        }
    }
}
