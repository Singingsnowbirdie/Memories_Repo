using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>Заменяет дом после поворота взгляда и удерживает управление до завершения графа входа.</summary>
    public sealed class BackyardEntranceReaction : CustomReaction
    {
        [SerializeField] private Triggers.SceneTrigger entranceTrigger;
        private GameplayModeState state;
        private Func<UniTask> replaceHouse;
        private bool completed;
        public bool IsRunning { get; private set; }
        public Triggers.SceneTrigger EntranceTrigger => entranceTrigger;

        public void Initialize(GameplayModeState mode,
            Func<UniTask> replacement, bool alreadyEntered)
        {
            state = mode; replaceHouse = replacement;
            entranceTrigger.enabled = !alreadyEntered;
            completed = alreadyEntered;
        }

        public override void Execute()
        {
            if (IsRunning || completed) return;
            PlayAsync().Forget();
        }

        private async UniTask PlayAsync()
        {
            IsRunning = true;
            state.BlockControls(this);
            var cancellation = this.GetCancellationTokenOnDestroy();
            try
            {
                // Игрок уже смотрит на забор: только теперь можно заменить дом.
                var replacement = replaceHouse();
                await replacement;
                await UniTask.WaitUntil(() => entranceTrigger == null || !entranceTrigger.IsRunning,
                    cancellationToken: cancellation);
                completed = true;
            }
            finally
            {
                state.ReleaseControls(this);
                IsRunning = false;
            }
        }

    }
}
