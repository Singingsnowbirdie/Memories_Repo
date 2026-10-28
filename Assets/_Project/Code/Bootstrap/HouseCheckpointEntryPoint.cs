using UnityEngine;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class HouseCheckpointEntryPoint : ITickable
    {
        private readonly GameSession session;
        public HouseCheckpointEntryPoint(GameSession session) => this.session = session;
        public void Tick() => session.UpdateHouseCheckpoint();

        public static bool CrossedEntrance(Vector3 previous, Vector3 current)
        {
            if (previous.z > 0 || current.z <= 0) return false;
            var crossing = Vector3.Lerp(previous, current, -previous.z / (current.z - previous.z));
            return Mathf.Abs(crossing.x) <= 1.1f && crossing.y >= -.2f && crossing.y <= 2.5f;
        }
    }
}
