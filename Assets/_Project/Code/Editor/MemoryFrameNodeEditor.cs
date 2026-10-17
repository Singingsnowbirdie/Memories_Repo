using Memories.Gameplay.Memories;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeEditor(typeof(MemoryFrameNode))]
    public sealed class MemoryFrameNodeEditor : NodeEditor
    {
        private static readonly string[] Transitions = { "Мгновенно", "Через затемнение", "Перекрёстный фейд" };

        public override void OnHeaderGUI()
            => GUILayout.Label("Кадр", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));

        public override void OnBodyGUI()
        {
            serializedObject.Update();
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort(MemoryFrameNode.InputPort));
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort(MemoryFrameNode.NextPort));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("image"), new GUIContent("Изображение"));
            var image = serializedObject.FindProperty("image").objectReferenceValue as Texture2D;
            if (image != null)
            {
                var preview = GUILayoutUtility.GetRect(1, 150, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(preview, image, null, ScaleMode.ScaleToFit);
            }
            else EditorGUILayout.HelpBox("Выберите изображение кадра.", MessageType.Warning);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("text"),
                new GUIContent("Текст", "Необязательная подпись под изображением. Появляется и исчезает вместе с кадром."));
            var transition = serializedObject.FindProperty("transition");
            transition.enumValueIndex = EditorGUILayout.Popup("Переход", transition.enumValueIndex, Transitions);
            using (new EditorGUI.DisabledScope(transition.enumValueIndex == (int)MemoryFrameTransition.Instant))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionDuration"),
                    new GUIContent("Переход, сек.", "Полная длительность перехода. Для затемнения включает исчезновение предыдущего и появление нового кадра."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayDuration"),
                new GUIContent("Показ, сек.", "Время полностью видимого кадра после перехода. Затем автоматически выполняется следующая нода."));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
