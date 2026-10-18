using UnityEngine;
using XNode;

namespace Memories.Gameplay.Memories
{
    public enum MemoryMusicAction { Play, Stop }

    [Node.CreateNodeMenu("Музыка"), Node.NodeTint("#493D60"), Node.NodeWidth(300)]
    public sealed class MemoryMusicNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Override)] private Node previous;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node next;
        [SerializeField] private MemoryMusicAction action;
        [SerializeField] private AudioClip clip;
        [SerializeField] private bool loop = true;
        [SerializeField, Min(0)] private float fadeDuration = 1f;

        public MemoryMusicAction Action => action;
        public AudioClip Clip => clip;
        public bool Loop => loop;
        public float FadeDuration => fadeDuration;
        protected override void Init() => name = "Музыка";
        public override object GetValue(NodePort port) => null;
        private void OnValidate()
        {
            fadeDuration = float.IsNaN(fadeDuration) || float.IsInfinity(fadeDuration) ? 1f : Mathf.Max(0, fadeDuration);
        }
    }
}
