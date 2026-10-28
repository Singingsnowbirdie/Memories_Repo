using UnityEngine.Scripting.APIUpdating;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Story;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Reactions
{
    public enum ReactionKind { StoryText, Memory, ChangeLocation, RevealPhoto }

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
            if (kind == ReactionKind.RevealPhoto) return true;
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
