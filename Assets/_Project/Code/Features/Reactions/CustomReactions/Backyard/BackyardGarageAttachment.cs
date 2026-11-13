using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графом Baseball_PlayReaction после второго воспоминания во дворе (разбитое окно).
    /// При загрузке также вызывается из Initialize, если это воспоминание уже просмотрено.
    /// Подгружает и пристраивает гараж через загрузчик сессии, затем убирает секции забора; двор остаётся загруженным.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackyardGarageAttachment : CustomReaction
    {
        public const string MemoryId = "backyard_baseball_second_memory";
        [SerializeField] private GameObject[] fenceSections = Array.Empty<GameObject>();
        private Func<BackyardGarageAttachment, UniTask> load;
        private bool opening;
        public bool IsOpen { get; private set; }

        public void Initialize(MemoryCollection memories, Func<BackyardGarageAttachment, UniTask> loader)
        {
            load = loader;
            // Восстанавливаем только завершённый прогресс. Во время игры граф явно вызывает Execute.
            if (memories.Contains(MemoryId)) Execute();
        }

        public override void Execute()
        {
            if (load == null || opening || IsOpen) return;
            opening = true;
            OpenAsync().Forget();
        }

        private async UniTask OpenAsync()
        {
            try
            {
                // Сначала даём завершиться воспроизведению и инициализации сессии.
                await UniTask.NextFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
                await load(this);
                if (this == null) return;
                foreach (var section in fenceSections) if (section != null) section.SetActive(false);
                IsOpen = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception, this); }
            finally { if (this != null) opening = false; }
        }

    }
}
