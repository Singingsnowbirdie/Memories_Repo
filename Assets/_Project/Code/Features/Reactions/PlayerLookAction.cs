using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Исполнитель явного действия графа LookAtObject, существующий на протяжении сессии.
    public sealed class PlayerLookAction
    {
        private readonly PlayerMotor player;
        private readonly PlayerMovementModel movement;
        private readonly GameplayModeState state;
        private readonly PlayerMovementSettings settings;
        private bool turning;

        public PlayerLookAction(PlayerMotor player, PlayerMovementModel movement,
            GameplayModeState state, PlayerMovementSettings settings)
        {
            this.player = player;
            this.movement = movement;
            this.state = state;
            this.settings = settings;
        }

        public async UniTask ExecuteAsync(Transform target, float minimumDuration, float maxSpeed,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (target == null) throw new InvalidOperationException("Look target is missing.");
            if (turning || player.CameraPoseControlled)
                throw new InvalidOperationException("The player camera is already controlled by another action.");
            var direction = target.position - player.CameraTransform.position;
            if (direction.sqrMagnitude < .000001f) return;
            float fromYaw = movement.Yaw, fromPitch = movement.Pitch;
            float horizontal = new Vector2(direction.x, direction.z).magnitude;
            float yaw = horizontal < .0001f ? fromYaw : Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Clamp(-Mathf.Atan2(direction.y, horizontal) * Mathf.Rad2Deg,
                -settings.PitchLimit, settings.PitchLimit);
            float angle = new Vector2(Mathf.DeltaAngle(fromYaw, yaw), pitch - fromPitch).magnitude;
            if (angle < .0001f) return;
            // Пиковая скорость SmoothStep в 1,5 раза выше средней.
            float duration = Mathf.Max(minimumDuration, angle * 1.5f / maxSpeed);
            turning = true;
            state.BlockControls(this);
            try
            {
                for (float elapsed = 0; elapsed < duration;)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (target == null) throw new InvalidOperationException("Look target was destroyed during the turn.");
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                    movement.SetInitialYaw(Mathf.LerpAngle(fromYaw, yaw, t));
                    movement.SetInitialPitch(Mathf.Lerp(fromPitch, pitch, t));
                    player.SetLook(movement.Yaw, movement.Pitch);
                    await UniTask.NextFrame(cancellationToken: cancellationToken);
                }
            }
            finally
            {
                state.ReleaseControls(this);
                turning = false;
            }
        }
    }
}
