using UnityEngine;
using XNode;

namespace Memories.Gameplay.Story
{
    [Node.CreateNodeMenu("Конец"), Node.DisallowMultipleNodes, Node.NodeWidth(140), Node.NodeTint("#563745")]
    public sealed class StoryTextEndNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Override)] private Node previous;
        protected override void Init() => name = "Конец";
        public override object GetValue(NodePort port) => null;
    }
}
