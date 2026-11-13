using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Вызывается графом входного триггера, а не наблюдением за прогрессом задания.
    public sealed class GarageEntranceReaction : CustomReaction
    {
        [SerializeField] private Transform doorHinge;
        [SerializeField] private Vector3 closedDoorEuler;
        [SerializeField, Min(.1f)] private float closeDuration = 1.5f;
        [SerializeField] private global::Memories.Gameplay.Triggers.SceneTrigger entranceTrigger;
        private GameplayModeState state;
        private Func<UniTask> enterGarage;
        private bool completed;
        public bool IsRunning { get; private set; }

        public void Initialize(GameplayModeState mode,
            Func<UniTask> entry, bool alreadyEntered)
        {
            state = mode;
            enterGarage = entry;
            // При прямой загрузке локации восстанавливаем закрытый гараж без повторения входной последовательности.
            entranceTrigger.enabled = !alreadyEntered;
            if (alreadyEntered)
            {
                doorHinge.localRotation = Quaternion.Euler(closedDoorEuler);
                completed = true;
            }
        }

        public override void Execute()
        {
            if (completed || IsRunning || doorHinge == null || enterGarage == null) return;
            PlayAsync().Forget();
        }

        private async UniTask PlayAsync()
        {
            IsRunning = true;
            state.BlockControls(this);
            var cancellation = this.GetCancellationTokenOnDestroy();
            try
            {

                // Игрок уже смотрит на отца, отвернувшись от бокового входа.
                var fromRotation = doorHinge.localRotation;
                var closed = Quaternion.Euler(closedDoorEuler);
                for (float elapsed = 0; elapsed < closeDuration;)
                {
                    elapsed += Time.deltaTime;
                    doorHinge.localRotation = Quaternion.Slerp(fromRotation, closed,
                        Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / closeDuration)));
                    await UniTask.NextFrame(cancellationToken: cancellation);
                }
                doorHinge.localRotation = closed;
                Physics.SyncTransforms();
                await enterGarage();
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
