using System;

namespace Memories.Gameplay.Player
{
    public sealed class PlayerMovementModel
    {
        private readonly PlayerMovementSettings settings;
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float VerticalSpeed { get; private set; }

        public PlayerMovementModel(PlayerMovementSettings settings)
        {
            settings.Validate();
            this.settings = settings;
        }

        public void SetInitialYaw(float yaw) => Yaw = yaw;

        public void Look(float horizontalDelta, float verticalDelta)
        {
            Yaw = (Yaw + horizontalDelta * settings.MouseSensitivity) % 360f;
            Pitch = Math.Max(-settings.PitchLimit,
                Math.Min(settings.PitchLimit, Pitch - verticalDelta * settings.MouseSensitivity));
        }

        public void ApplyGravity(bool grounded, float deltaTime)
        {
            if (grounded && VerticalSpeed < 0) VerticalSpeed = -2f;
            VerticalSpeed = Math.Max(-settings.TerminalSpeed, VerticalSpeed + settings.Gravity * deltaTime);
        }

        public void OnCeilingHit()
        {
            if (VerticalSpeed > 0) VerticalSpeed = 0;
        }
    }
}
