using Memories.Gameplay.Player;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class TeleportPlayerReaction : CustomReaction
    {
        [SerializeField] private Vector3 arrivalPosition;
        [SerializeField] private float arrivalYaw;
        private PlayerMotor player;
        private PlayerMovementModel movement;
        public override bool TransfersMemoryOwnership => true;

        public void Initialize(PlayerMotor player, PlayerMovementModel movement)
        {
            this.player = player;
            this.movement = movement;
        }

        public override void Execute()
        {
            player.Teleport(arrivalPosition, arrivalYaw);
            movement.ResetMotion(arrivalYaw);
        }
    }
}
