using UnityEngine.Scripting.APIUpdating;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Story;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Reactions
{
    public enum ReactionKind { StoryText, Memory, ChangeLocation, CustomReaction, SetCustomVariable, ObjectActivity, AddInventoryItem, StartHints, StopHints, ConsumeInventoryItem }

    [Node.CreateNodeMenu("Реакция"), Node.NodeWidth(320), Node.NodeTint("#284C53")]
    [MovedFrom(true, sourceNamespace: "Memories.Gameplay.Triggers", sourceAssembly: "Memories.Gameplay", sourceClassName: "TriggerActionNode")]
    public sealed class ReactionNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Multiple)] private Node previous;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node next;
        [SerializeField] private ReactionKind kind;
        [SerializeField] private StoryTextGraph textGraph;
        [SerializeField] private MemoryGraph memoryGraph;
        [SerializeField] private string memoryId;
        [SerializeField] private string locationScenePath;
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private string variableId;
        [SerializeField] private bool variableValue = true;
        [SerializeField] private string objectKey;
        [SerializeField] private bool active = true;
        [SerializeField] private global::Memories.Gameplay.Interaction.QuestItem item;
        public global::Memories.Gameplay.Interaction.QuestItem Item => item;
        public string ObjectKey => objectKey;
        [SerializeField] private string hintId;
        [SerializeField] private ReactionGraph hintGraph;
        [SerializeField, Min(0.01f)] private float hintTimeout = 180f;
        [SerializeField] private bool repeatHint = true;
        public string HintId => hintId;
        public ReactionGraph HintGraph => hintGraph;
        public float HintTimeout => hintTimeout;
        public bool RepeatHint => repeatHint;
        public bool Active => active;
        public CustomVariableBank VariableBank => variableBank;
        public string VariableId => variableId;
        public bool VariableValue => variableValue;
        public ReactionKind Kind => kind;
        public StoryTextGraph TextGraph => textGraph;
        public MemoryGraph MemoryGraph => memoryGraph;
        public string MemoryId => memoryId;
        public string LocationScenePath => locationScenePath;
        protected override void Init() => name = "Реакция";
        public override object GetValue(NodePort port) => null;

        public bool Validate(out string error)
        {
            error = null;
            if (kind == ReactionKind.StartHints || kind == ReactionKind.StopHints)
            {
                if (string.IsNullOrWhiteSpace(hintId))
                { error = "Задайте ID подсказки."; return false; }
                if (kind == ReactionKind.StopHints) return true;
                if (hintGraph == null || hintGraph == graph || hintTimeout <= 0 ||
                    float.IsNaN(hintTimeout) || float.IsInfinity(hintTimeout))
                { error = "Назначьте отдельный граф подсказки и положительный таймаут в секундах."; return false; }
                foreach (var node in hintGraph.nodes)
                    if (node is ReactionNode action && (action.Kind == ReactionKind.StartHints || action.Kind == ReactionKind.ChangeLocation))
                    { error = "Граф подсказки не должен запускать таймеры или менять локацию."; return false; }
                return hintGraph.TryGetSequence(out _, out error);
            }
            if (kind == ReactionKind.AddInventoryItem || kind == ReactionKind.ConsumeInventoryItem)
            {
                if (item != null && item.IsValid) return true;
                error = "Назначьте предмет для добавления в инвентарь.";
                return false;
            }
            if (kind == ReactionKind.ObjectActivity)
            {
                if (graph is ReactionGraph reaction && reaction.HasObjectKey(objectKey)) return true;
                error = "Выберите объект из списка объектов графа.";
                return false;
            }
            if (kind == ReactionKind.SetCustomVariable)
            {
                if (variableBank != null && variableBank.Find(variableId) != null) return true;
                error = "Выберите существующую переменную из банка.";
                return false;
            }
            if (kind == ReactionKind.CustomReaction) return true;
            if (kind == ReactionKind.ChangeLocation)
            {
                if (string.IsNullOrWhiteSpace(locationScenePath) || !locationScenePath.EndsWith(".unity"))
                { error = "Назначьте сцену следующей локации."; return false; }
                return true;
            }
            if (kind == ReactionKind.StoryText)
            {
                if (textGraph == null) { error = "Назначьте граф текста."; return false; }
                return textGraph.TryGetSequence(out _, out error);
            }
            if (kind == ReactionKind.Memory)
            {
                if (memoryGraph == null || string.IsNullOrWhiteSpace(memoryId))
                { error = "Назначьте граф воспоминания и его ID."; return false; }
                return memoryGraph.TryGetSequence(out _, out error);
            }
            error = "Неизвестный тип реакции.";
            return false;
        }
    }
}
