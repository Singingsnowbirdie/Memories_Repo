using UnityEngine;
using XNode;

namespace Memories.Gameplay.Triggers
{
    [Node.CreateNodeMenu("Начало"), Node.DisallowMultipleNodes, Node.NodeWidth(250)]
    public sealed class TriggerStartNode : Node
    {
        [SerializeField] private bool triggerOnce = true;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node next;
        public bool TriggerOnce => triggerOnce;
        protected override void Init() => name = "Начало";
        public override object GetValue(NodePort port) => null;
    }
}
