using UnityEngine;

namespace Memories.Gameplay.Memories
{
    [CreateAssetMenu(menuName = "Memories/Memory Comic")]
    public sealed class MemoryComic : ScriptableObject
    {
        [SerializeField] private Texture2D[] frames;
        public int FrameCount => frames == null ? 0 : frames.Length;
        public Texture2D GetFrame(int index) => frames[index];
        public bool IsValid
        {
            get
            {
                if (FrameCount == 0) return false;
                foreach (var frame in frames) if (frame == null) return false;
                return true;
            }
        }
    }
}
