using UnityEngine;

namespace Memories.Contracts
{
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        bool SprintHeld { get; }
        Vector2 LookDelta { get; }
        void Enable();
        void Disable();
        void Poll();
    }
}
