using Memories.Gameplay.Memories;
using UnityEngine;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeEditor(typeof(MemoryStartNode))]
    public sealed class MemoryStartNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI()
            => GUILayout.Label("Начало", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));

        public override void OnBodyGUI()
            => NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort(MemoryStartNode.NextPort));
    }
}
