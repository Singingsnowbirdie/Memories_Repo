using System;
using Memories.Gameplay.Memories;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeGraphEditor(typeof(MemoryGraph))]
    public sealed class MemoryGraphEditor : NodeGraphEditor
    {
        public override string GetNodeMenuName(Type type)
            => type == typeof(MemoryStartNode) ? "Начало" : null;
    }
}
