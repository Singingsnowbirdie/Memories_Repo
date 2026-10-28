using Memories.Gameplay.Reactions;
using UnityEditor;
using UnityEngine;

namespace Memories.Editor
{
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
            if (graph == null) EditorGUI.PropertyField(row, key, new GUIContent("Имя в графе"));
            else
            {
                var names = new string[graph.ObjectKeys.Count + 1];
                names[0] = string.IsNullOrEmpty(key.stringValue) ? "Выберите объект" : "Не найден: " + key.stringValue;
                int selected = 0;
                for (int i = 0; i < graph.ObjectKeys.Count; i++)
                { names[i + 1] = graph.ObjectKeys[i]; if (names[i + 1] == key.stringValue) selected = i + 1; }
                EditorGUI.BeginChangeCheck();
                int chosen = EditorGUI.Popup(row, "Имя в графе", selected, names);
                if (EditorGUI.EndChangeCheck()) key.stringValue = chosen == 0 ? "" : names[chosen];
            }
            row.y += 24;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("target"), new GUIContent("Объект сцены"));
            EditorGUI.EndProperty();
        }
    }
}
