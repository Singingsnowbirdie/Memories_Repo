using UnityEngine;
using XNode;

namespace Memories.Gameplay.Memories
{
    public enum MemoryFrameTransition { Instant, FadeThroughBlack, CrossFade }

    [Node.CreateNodeMenu("Кадр")]
    [Node.NodeTint("#284C53")]
    [Node.NodeWidth(320)]
    public sealed class MemoryFrameNode : Node
    {
        public const string InputPort = nameof(previous);
        public const string NextPort = nameof(next);

        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Override)]
        private Node previous;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)]
        private Node next;
        [SerializeField] private Texture2D image;
        [SerializeField, TextArea(3, 8)] private string text;
        [SerializeField] private MemoryFrameTransition transition = MemoryFrameTransition.FadeThroughBlack;
        [SerializeField, Min(0)] private float transitionDuration = .5f;
        [SerializeField, Min(.1f)] private float displayDuration = 4f;

        public Texture2D Image => image;
        public string Text => text;
        public MemoryFrameTransition Transition => transition;
        public float TransitionDuration => transition == MemoryFrameTransition.Instant ? 0 : transitionDuration;
        // Fully visible time, after the incoming transition finishes. Continuation is always automatic.
        public float DisplayDuration => displayDuration;

        protected override void Init() => name = "Кадр";
        public override object GetValue(NodePort port) => null;

        private void OnValidate()
        {
            transitionDuration = float.IsNaN(transitionDuration) || float.IsInfinity(transitionDuration)
                ? .5f : Mathf.Max(0, transitionDuration);
            displayDuration = float.IsNaN(displayDuration) || float.IsInfinity(displayDuration)
                ? 4f : Mathf.Max(.1f, displayDuration);
        }
    }
}
