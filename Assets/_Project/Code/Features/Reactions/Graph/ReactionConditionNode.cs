using Memories.Gameplay.Interaction;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Reactions
{
    public enum ReactionConditionKind { InventoryItem = 0, CustomVariable = 2 }

    [Node.CreateNodeMenu("Условие"), Node.NodeWidth(280), Node.NodeTint("#493D60")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Memories.Gameplay.Reactions", sourceAssembly: "Memories.Gameplay", sourceClassName: "InventoryConditionNode")]
    public sealed class ReactionConditionNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Multiple)] private Node previous;
        [SerializeField] private ReactionConditionKind kind;
        [SerializeField] private QuestItem item;
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private string variableId;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node ifPresent;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node ifAbsent;

        public QuestItem Item => item;
        public ReactionConditionKind Kind => kind;
        public bool RequiresInventory => kind != ReactionConditionKind.CustomVariable;

        public bool Validate(out string error)
        {
            error = null;
            switch (kind)
            {
                case ReactionConditionKind.CustomVariable:
                    if (variableBank != null && variableBank.Find(variableId) != null) return true;
                    error = "Выберите существующую переменную из банка.";
                    return false;
                case ReactionConditionKind.InventoryItem:
                    if (item != null && item.IsValid) return true;
                    error = "Назначьте предмет для проверки инвентаря.";
                    return false;
                default:
                    error = "Неизвестный тип проверки.";
                    return false;
            }
        }

        public string SelectOutput(QuestInventory inventory, CustomVariableStore variables = null)
        {
            bool result;
            switch (kind)
            {
                case ReactionConditionKind.CustomVariable:
                    if (variables == null) throw new System.InvalidOperationException("Хранилище кастомных переменных не подключено.");
                    result = variables.Get(variableBank, variableId);
                    break;
                case ReactionConditionKind.InventoryItem:
                    result = inventory.Contains(item.Id);
                    break;
                default:
                    throw new System.InvalidOperationException("Неизвестный тип проверки.");
            }
            return result ? "ifPresent" : "ifAbsent";
        }

        protected override void Init() => name = "Условие";
        public override object GetValue(NodePort port) => null;
    }
}
