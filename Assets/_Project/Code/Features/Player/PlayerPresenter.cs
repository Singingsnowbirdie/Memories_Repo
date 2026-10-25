using System;
using Memories.Contracts;
using UnityEngine;

namespace Memories.Gameplay.Player
{
    public sealed class PlayerPresenter : IDisposable
    {
        private readonly IPlayerInput input;
        private readonly PlayerMovementModel model;
        private readonly IPlayerView view;
        private readonly PlayerMovementSettings settings;
        private bool started;
        public bool ControlsEnabled { get; set; } = true;

        public PlayerPresenter(IPlayerInput input, PlayerMovementModel model, IPlayerView view, PlayerMovementSettings settings)
        {
            this.input = input;
            this.model = model;
            this.view = view;
            this.settings = settings;
        }

        public void Start()
        {
            if (started) return;
            model.SetInitialYaw(view.InitialYaw);
            input.Enable();
            started = true;
        }

        public void Tick(float deltaTime)
        {
            if (!started) return;
            input.Poll();
            if (deltaTime <= 0) return;
            var look = ControlsEnabled ? input.LookDelta : Vector2.zero;
            // Mouse delta is already a displacement; do not multiply it by deltaTime.
            model.Look(look.x, look.y);
            view.SetLook(model.Yaw, model.Pitch);
            var move = ControlsEnabled ? Vector2.ClampMagnitude(input.Move, 1f) : Vector2.zero;
            var speed = ControlsEnabled && input.SprintHeld ? settings.RunSpeed : settings.WalkSpeed;
            var velocity = Quaternion.Euler(0, model.Yaw, 0) * new Vector3(move.x, 0, move.y) * speed;
            model.ApplyGravity(view.IsGrounded, deltaTime);
            velocity.y = model.VerticalSpeed;
            if ((view.Move(velocity * deltaTime) & CollisionFlags.Above) != 0) model.OnCeilingHit();
        }

        public void Dispose()
        {
            if (!started) return;
            started = false;
            input.Disable();
        }
    }
}
