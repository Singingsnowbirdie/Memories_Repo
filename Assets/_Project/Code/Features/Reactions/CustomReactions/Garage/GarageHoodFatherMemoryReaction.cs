using Memories.Gameplay.Memories;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

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

        // Вызывается один раз после применения сохранённых состояний объектов, включая старые сохранения.
        public void Restore(MemoryCollection collection)
        {
            if (collection.Contains(MemoryId)) ShowHoodFather();
        }

        public override void Execute() => ExecuteAsync(this.GetCancellationTokenOnDestroy()).Forget();

        public override async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            // Комикс скрывает переключение. Компонент находится вне обоих объектов отца.
            if (presenter == null || !presenter.Open(MemoryId, memory, true)) return;
            await presenter.WaitForFirstFrameAsync(cancellationToken);
            ShowHoodFather();
        }

        private void ShowHoodFather()
        {
            hoodFather.SetActive(true);
            kneelingFather.SetActive(false);
        }
    }
}
