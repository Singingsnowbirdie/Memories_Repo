using Memories.Gameplay.Memories;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeEditor(typeof(MemorySoundNode))]
    public sealed class MemorySoundNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI()
            => GUILayout.Label("Звук", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), new GUIContent("Аудиоклип"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("delay"), new GUIContent("Задержка, сек."));
            var wait = serializedObject.FindProperty("waitForCompletion");
            wait.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Ждать окончания",
                "Включено: переход после задержки и звука. Выключено: переход сразу, звук запускается параллельно после задержки."), wait.boolValue);
            if (serializedObject.FindProperty("clip").objectReferenceValue == null)
                EditorGUILayout.HelpBox("Клип не назначен. Нода будет пропущена без ожидания.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
