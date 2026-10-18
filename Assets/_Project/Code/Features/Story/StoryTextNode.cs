using UnityEngine;
using XNode;

namespace Memories.Gameplay.Story
{
    [Node.CreateNodeMenu("Текст"), Node.NodeWidth(340), Node.NodeTint("#284C53")]
    public sealed class StoryTextNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Override)] private Node previous;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node next;
        [SerializeField, TextArea(3, 8)] private string text = "Новый фрагмент текста.";
        [SerializeField, Min(.1f)] private float duration = 3f;

        public string Text => text;
        public float Duration => duration;

        protected override void Init() => name = "Текст";
        public override object GetValue(NodePort port) => null;
    }
}
