using UnityEngine;
using XNode;

namespace Memories.Gameplay.Story
{
    [Node.CreateNodeMenu("Начало"), Node.DisallowMultipleNodes, Node.NodeWidth(140), Node.NodeTint("#163C58")]
    public sealed class StoryTextStartNode : Node
    {
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node next;
        protected override void Init() => name = "Начало";
        public override object GetValue(NodePort port) => null;
    }
}
