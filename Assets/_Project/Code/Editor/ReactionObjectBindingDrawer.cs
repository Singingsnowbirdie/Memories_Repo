using Memories.Gameplay.Reactions;
using UnityEditor;
using UnityEngine;

namespace Memories.Editor
{
    [CustomPropertyDrawer(typeof(CustomReactionBinding))]
    public sealed class CustomReactionBindingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 48;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var graphProperty = property.serializedObject.FindProperty("graph") ?? property.serializedObject.FindProperty("interactionGraph");
            var graph = graphProperty?.objectReferenceValue as ReactionGraph;
            var key = property.FindPropertyRelative("key");
            var row = new Rect(position.x, position.y, position.width, 18);
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.DelayedTextField(row, "Имя реакции", key.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                string oldName = key.stringValue;
                key.stringValue = newName.Trim();
                if (graph != null)
                {
                    using (var graphSettings = new SerializedObject(graph))
                    {
                        var names = graphSettings.FindProperty("objectKeys");
                        bool oldNameUsedByObject = graph.nodes.Exists(node => node is ReactionNode action &&
                            (action.Kind == ReactionKind.ObjectActivity || action.Kind == ReactionKind.ObjectInteractivity) &&
                            action.ObjectKey == oldName);
                        if (!oldNameUsedByObject && oldName != key.stringValue)
                            for (int i = names.arraySize - 1; i >= 0; i--)
                                if (names.GetArrayElementAtIndex(i).stringValue == oldName)
                                    names.DeleteArrayElementAtIndex(i);
                        bool exists = string.IsNullOrEmpty(key.stringValue);
                        for (int i = 0; i < names.arraySize; i++)
                            exists |= names.GetArrayElementAtIndex(i).stringValue == key.stringValue;
                        if (!exists)
                        {
                            names.InsertArrayElementAtIndex(names.arraySize);
                            names.GetArrayElementAtIndex(names.arraySize - 1).stringValue = key.stringValue;
                        }
                        graphSettings.ApplyModifiedProperties();
                    }
                    foreach (var node in graph.nodes)
                    {
                        if (!(node is ReactionNode action) || action.Kind != ReactionKind.CustomReaction ||
                            (action.ObjectKey ?? "") != oldName) continue;
                        using (var settings = new SerializedObject(action))
                        {
                            settings.FindProperty("objectKey").stringValue = key.stringValue;
                            settings.ApplyModifiedProperties();
                        }
                    }
                    XNodeEditor.NodeEditorWindow.RepaintAll();
                }
            }
            row.y += 24;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("reaction"), new GUIContent("CustomReaction"));
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ReactionObjectBinding))]
    public sealed class ReactionObjectBindingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 48;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var graphProperty = property.serializedObject.FindProperty("graph") ?? property.serializedObject.FindProperty("interactionGraph");
            var graph = graphProperty?.objectReferenceValue as ReactionGraph;
            var key = property.FindPropertyRelative("key");
            var row = new Rect(position.x, position.y, position.width, 18);
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.DelayedTextField(row, "Имя объекта", key.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                string oldName = key.stringValue;
                key.stringValue = newName.Trim();
                RenameObject(graph, oldName, key.stringValue);
                var initializationGraph = property.serializedObject.FindProperty("initializationGraph")?.objectReferenceValue as ReactionGraph;
                if (initializationGraph != graph) RenameObject(initializationGraph, oldName, key.stringValue);
                XNodeEditor.NodeEditorWindow.RepaintAll();
            }
            row.y += 24;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("target"), new GUIContent("Объект сцены"));
            EditorGUI.EndProperty();
        }

        private static void RenameObject(ReactionGraph graph, string oldName, string newName)
        {
            if (graph == null || oldName == newName) return;
            using (var settings = new SerializedObject(graph))
            {
                var names = settings.FindProperty("objectKeys");
                bool usedByReaction = graph.nodes.Exists(node => node is ReactionNode action &&
                    action.Kind == ReactionKind.CustomReaction && action.ObjectKey == oldName);
                if (!usedByReaction)
                    for (int i = names.arraySize - 1; i >= 0; i--)
                        if (names.GetArrayElementAtIndex(i).stringValue == oldName)
                            names.DeleteArrayElementAtIndex(i);
                bool exists = string.IsNullOrEmpty(newName);
                for (int i = 0; i < names.arraySize; i++)
                    exists |= names.GetArrayElementAtIndex(i).stringValue == newName;
                if (!exists)
                {
                    names.InsertArrayElementAtIndex(names.arraySize);
                    names.GetArrayElementAtIndex(names.arraySize - 1).stringValue = newName;
                }
                settings.ApplyModifiedProperties();
            }
            // An empty key means the invoking object, not a newly added binding.
            if (string.IsNullOrEmpty(oldName)) return;
            foreach (var node in graph.nodes)
            {
                if (!(node is ReactionNode action) || action.ObjectKey != oldName ||
                    (action.Kind != ReactionKind.ObjectActivity && action.Kind != ReactionKind.ObjectInteractivity)) continue;
                using (var settings = new SerializedObject(action))
                {
                    settings.FindProperty("objectKey").stringValue = newName;
                    settings.ApplyModifiedProperties();
                }
            }
        }
    }
}
