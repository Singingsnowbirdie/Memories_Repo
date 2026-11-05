using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Invoked by the entrance trigger's graph, never by observing quest progress.
    public sealed class GarageEntranceReaction : CustomReaction
    {
        [SerializeField] private Transform father;
        [SerializeField] private Transform doorHinge;
        [SerializeField] private Vector3 closedDoorEuler;
        [SerializeField, Min(.1f)] private float turnDuration = 1.2f;
        [SerializeField, Min(.1f)] private float closeDuration = 1.5f;
        private PlayerMotor player;
        private PlayerMovementModel movement;
        private GameplayModeState state;
        private Func<UniTask> enterGarage;
        private bool completed;
        public bool IsRunning { get; private set; }

        public void Initialize(PlayerMotor motor, PlayerMovementModel model, GameplayModeState mode,
            Func<UniTask> entry, bool alreadyEntered)
        {
            player = motor;
            movement = model;
            state = mode;
            enterGarage = entry;
            // Direct location loads restore the sealed garage without replaying the entrance.
            if (alreadyEntered)
            {
                doorHinge.localRotation = Quaternion.Euler(closedDoorEuler);
                completed = true;
            }
        }

        public override void Execute()
        {
            if (completed || IsRunning || player == null || father == null || doorHinge == null || enterGarage == null) return;
            PlayAsync().Forget();
        }

        private async UniTask PlayAsync()
        {
            IsRunning = true;
            state.BlockControls(this);
            var cancellation = this.GetCancellationTokenOnDestroy();
            try
            {
                var direction = father.position - player.CameraTransform.position;
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                float fromYaw = movement.Yaw, fromPitch = movement.Pitch;
                float angle = new Vector2(Mathf.DeltaAngle(fromYaw, yaw), pitch - fromPitch).magnitude;
                float duration = Mathf.Max(turnDuration, angle * 1.5f / 60f);
                for (float elapsed = 0; elapsed < duration;)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                    movement.SetInitialYaw(Mathf.LerpAngle(fromYaw, yaw, t));
                    movement.SetInitialPitch(Mathf.Lerp(fromPitch, pitch, t));
                    player.SetLook(movement.Yaw, movement.Pitch);
                    await UniTask.NextFrame(cancellationToken: cancellation);
                }

                // The player now faces the father, away from the side entrance.
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
