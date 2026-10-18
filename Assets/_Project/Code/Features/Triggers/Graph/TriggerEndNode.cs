using UnityEngine;
using XNode;

namespace Memories.Gameplay.Triggers
{
    [Node.CreateNodeMenu("Конец"), Node.DisallowMultipleNodes, Node.NodeWidth(140)]
    public sealed class TriggerEndNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Override)] private Node previous;
        protected override void Init() => name = "Конец";
        public override object GetValue(NodePort port) => null;
    }
}
