using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается явно реакцией выхода из дома после пересечения дверного проёма.
    /// Забирает управление, поворачивает взгляд к надписи без приближения камеры,
    /// заменяет дом за спиной и зажигает фонари парами. Затем возвращает управление.
    /// При загрузке непосредственно во двор восстанавливает уже включённые фонари без вступления.
    /// </summary>
    public sealed class BackyardEntranceReaction : CustomReaction
    {
        [SerializeField] private Transform inscription;
        [SerializeField] private Light[] lights;
        [SerializeField] private Renderer[] lanterns;
        [SerializeField] private Material litGlass;
        [SerializeField, Min(.1f)] private float turnDuration = 1.2f;
        [SerializeField, Min(.1f)] private float pairInterval = .45f;
        private PlayerMotor player;
        private PlayerMovementModel movement;
        private GameplayModeState state;
        private Func<UniTask> replaceHouse;
        private bool completed;
        public bool IsRunning { get; private set; }
        public UniTask Completion { get; private set; }

        public void Initialize(PlayerMotor motor, PlayerMovementModel model, GameplayModeState mode,
            Func<UniTask> replacement, bool alreadyEntered)
        {
            player = motor; movement = model; state = mode; replaceHouse = replacement;
            if (alreadyEntered)
            {
                for (int i = 0; i < lights.Length; i++) LightLantern(i);
                completed = true;
            }
        }

        public override void Execute()
        {
            if (IsRunning || completed) return;
            Completion = PlayAsync();
        }

        private async UniTask PlayAsync()
        {
            IsRunning = true;
            state.BlockControls(this);
            var cancellation = this.GetCancellationTokenOnDestroy();
            try
            {
                var direction = inscription.position - player.CameraTransform.position;
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                float initialYaw = movement.Yaw, initialPitch = movement.Pitch;
                for (float elapsed = 0; elapsed < turnDuration;)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / turnDuration));
                    movement.SetInitialYaw(Mathf.LerpAngle(initialYaw, yaw, t));
                    movement.SetInitialPitch(Mathf.Lerp(initialPitch, pitch, t));
                    player.SetLook(movement.Yaw, movement.Pitch);
                    await UniTask.NextFrame(cancellationToken: cancellation);
                }
                // Игрок уже смотрит на забор: только теперь можно заменить дом.
                var replacement = replaceHouse();
                for (int i = 0; i < lights.Length; i += 2)
                {
                    LightLantern(i);
                    if (i + 1 < lights.Length) LightLantern(i + 1);
                    await UniTask.Delay(TimeSpan.FromSeconds(pairInterval), cancellationToken: cancellation);
                }
                await replacement;
                completed = true;
            }
            finally
            {
                state.ReleaseControls(this);
                IsRunning = false;
            }
        }

        private void LightLantern(int index)
        {
            lights[index].enabled = true;
            var materials = lanterns[index].sharedMaterials;
            materials[1] = litGlass;
            lanterns[index].sharedMaterials = materials;
        }
    }
}
