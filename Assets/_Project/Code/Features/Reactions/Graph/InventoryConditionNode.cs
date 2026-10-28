using Memories.Gameplay.Interaction;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Reactions
{
    [Node.CreateNodeMenu("Предмет в инвентаре"), Node.NodeWidth(280)]
    public sealed class InventoryConditionNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Multiple)] private Node previous;
        [SerializeField] private QuestItem item;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node ifPresent;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node ifAbsent;

        public QuestItem Item => item;
        public string SelectOutput(QuestInventory inventory) => inventory.Contains(item.Id) ? "ifPresent" : "ifAbsent";
        protected override void Init() => name = "Предмет в инвентаре";
        public override object GetValue(NodePort port) => null;
    }
}
