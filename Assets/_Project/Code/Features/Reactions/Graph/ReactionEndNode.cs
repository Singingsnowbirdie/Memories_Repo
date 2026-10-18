using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Reactions
{
    [Node.CreateNodeMenu("Конец"), Node.DisallowMultipleNodes, Node.NodeWidth(140)]
    [MovedFrom(true, sourceNamespace: "Memories.Gameplay.Triggers", sourceAssembly: "Memories.Gameplay", sourceClassName: "TriggerEndNode")]
    public sealed class ReactionEndNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Override)] private Node previous;
        protected override void Init() => name = "Конец";
        public override object GetValue(NodePort port) => null;
    }
}
