using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    [AddComponentMenu("")]
    public sealed class ObjectStateSaveDriver : MonoBehaviour
    {
        public ObjectStateStore Store { private get; set; }
        private void LateUpdate() => Store?.Flush();
        private void OnApplicationPause(bool paused) { if (paused) Store?.Flush(); }
        private void OnApplicationQuit() => Store?.Flush();
    }
}
