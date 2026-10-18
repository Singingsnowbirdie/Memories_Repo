using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Reactions
{
    [Node.CreateNodeMenu("Начало"), Node.DisallowMultipleNodes, Node.NodeWidth(250)]
    [MovedFrom(true, sourceNamespace: "Memories.Gameplay.Triggers", sourceAssembly: "Memories.Gameplay", sourceClassName: "TriggerStartNode")]
    public sealed class ReactionStartNode : Node
    {
        [FormerlySerializedAs("triggerOnce"), SerializeField] private bool runOnce = true;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node next;
        public bool RunOnce => runOnce;
        protected override void Init() => name = "Начало";
        public override object GetValue(NodePort port) => null;
    }
}
