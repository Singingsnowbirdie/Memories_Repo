using UnityEngine;
using XNode;

namespace Memories.Gameplay.Memories
{
    [Node.CreateNodeMenu("Начало")]
    [Node.DisallowMultipleNodes]
    [Node.NodeTint("#163C58")]
    [Node.NodeWidth(140)]
    public sealed class MemoryStartNode : Node
    {
        public const string NextPort = nameof(next);
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)]
        private Node next;

        protected override void Init() => name = "Начало";

        // This port describes execution order; it does not evaluate a data value.
        public override object GetValue(NodePort port) => null;
    }
}
