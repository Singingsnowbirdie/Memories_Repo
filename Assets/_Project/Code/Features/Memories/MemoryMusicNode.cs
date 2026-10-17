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
        [SerializeField, Range(0, 1)] private float volume = .7f;
        [SerializeField] private bool loop = true;
        [SerializeField, Min(0)] private float fadeDuration = 1f;

        public MemoryMusicAction Action => action;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public bool Loop => loop;
        public float FadeDuration => fadeDuration;
        protected override void Init() => name = "Музыка";
        public override object GetValue(NodePort port) => null;
        private void OnValidate()
        {
            volume = float.IsNaN(volume) || float.IsInfinity(volume) ? .7f : Mathf.Clamp01(volume);
            fadeDuration = float.IsNaN(fadeDuration) || float.IsInfinity(fadeDuration) ? 1f : Mathf.Max(0, fadeDuration);
        }
    }
}
