using Memories.Gameplay.Memories;
using Memories.Gameplay.Story;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Triggers
{
    public enum TriggerActionKind { StoryText, Memory }

    [Node.CreateNodeMenu("Событие"), Node.NodeWidth(320)]
    public sealed class TriggerActionNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Override)] private Node previous;
        [SerializeField, Output(ShowBackingValue.Never, ConnectionType.Override)] private Node next;
        [SerializeField] private TriggerActionKind kind;
        [SerializeField] private StoryTextGraph textGraph;
        [SerializeField] private MemoryGraph memoryGraph;
        [SerializeField] private string memoryId;
        public TriggerActionKind Kind => kind;
        public StoryTextGraph TextGraph => textGraph;
        public MemoryGraph MemoryGraph => memoryGraph;
        public string MemoryId => memoryId;
        protected override void Init() => name = "Событие";
        public override object GetValue(NodePort port) => null;

        public bool Validate(out string error)
        {
            error = null;
            if (kind == TriggerActionKind.StoryText)
            {
                if (textGraph == null) { error = "Назначьте граф текста."; return false; }
                return textGraph.TryGetSequence(out _, out error);
            }
            if (kind == TriggerActionKind.Memory)
            {
                if (memoryGraph == null || string.IsNullOrWhiteSpace(memoryId))
                { error = "Назначьте граф воспоминания и его ID."; return false; }
                return memoryGraph.TryGetSequence(out _, out error);
            }
            error = "Неизвестный тип события.";
            return false;
        }
    }
}
