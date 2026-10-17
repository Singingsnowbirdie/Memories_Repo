using UnityEngine;

namespace Memories.Gameplay.Player
{
    public interface IPlayerView
    {
        float InitialYaw { get; }
        bool IsGrounded { get; }
        void SetLook(float yaw, float pitch);
        CollisionFlags Move(Vector3 displacement);
    }
}
