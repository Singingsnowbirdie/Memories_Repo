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
            // Однократно восстанавливаем завершённый прогресс; изменения во время игры вызывает только граф.
            bool completed = collection.Contains(MemoryId);
            standingFather.SetActive(!completed);
            kneelingFather.SetActive(completed);
        }

        public override void Execute()
        {
            // Open показывает полноэкранный комикс до изменения сцены за ним.
            // Реакция находится вне объекта отца, поэтому его скрытие не прерывает воспроизведение.
            if (presenter == null || !presenter.Open(MemoryId, memory, true)) return;
            kneelingFather.SetActive(true);
            standingFather.SetActive(false);
        }
    }
}
