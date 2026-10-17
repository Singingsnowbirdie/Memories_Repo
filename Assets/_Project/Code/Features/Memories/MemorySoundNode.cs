using UnityEngine;
using XNode;

namespace Memories.Gameplay.Memories
{
    [Node.CreateNodeMenu("Звук"), Node.NodeTint("#493D60"), Node.NodeWidth(300)]
    public sealed class MemorySoundNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Override)] private Node previous;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node next;
        [SerializeField] private AudioClip clip;
        [SerializeField, Min(0)] private float delay;
        [SerializeField] private bool waitForCompletion;
        public AudioClip Clip => clip;
        public float Delay => delay;
        public bool WaitForCompletion => waitForCompletion;
        protected override void Init() => name = "Звук";
        public override object GetValue(NodePort port) => null;
        private void OnValidate()
            => delay = float.IsNaN(delay) || float.IsInfinity(delay) ? 0 : Mathf.Max(0, delay);
    }
}
