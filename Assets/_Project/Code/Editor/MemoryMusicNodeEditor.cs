using Memories.Gameplay.Memories;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeEditor(typeof(MemoryMusicNode))]
    public sealed class MemoryMusicNodeEditor : NodeEditor
    {
        private static readonly string[] Actions = { "Запустить", "Остановить" };
        public override void OnHeaderGUI()
            => GUILayout.Label("Музыка", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
            var action = serializedObject.FindProperty("action");
            action.enumValueIndex = EditorGUILayout.Popup("Действие", action.enumValueIndex, Actions);
            if (action.enumValueIndex == (int)MemoryMusicAction.Play)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), new GUIContent("Аудиоклип"));
                var loop = serializedObject.FindProperty("loop");
                loop.boolValue = EditorGUILayout.ToggleLeft("Зацикливание", loop.boolValue);
                if (serializedObject.FindProperty("clip").objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Клип не назначен. Нода будет пропущена, воспоминание продолжится без новой музыки.", MessageType.Info);
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeDuration"),
                new GUIContent("Фейд, сек.", "Плавное появление или затухание. Следующая нода запускается сразу."));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
