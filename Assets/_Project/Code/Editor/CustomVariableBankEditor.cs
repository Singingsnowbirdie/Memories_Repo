using Memories.Gameplay.Reactions;
using UnityEditor;
using UnityEngine;

namespace Memories.Editor
{
    [CustomEditor(typeof(CustomVariableBank))]
    public sealed class CustomVariableBankEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Логические и целочисленные переменные. Начальное значение используется, пока нет сохранённого значения того же типа. Переименование сохраняет ID и связи в графах.", MessageType.Info);
            EditorGUILayout.Space(8);
            var variables = serializedObject.FindProperty("variables");
            for (int i = 0; i < variables.arraySize; i++)
            {
                var variable = variables.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(variable.FindPropertyRelative("name"), new GUIContent("Имя"));
                EditorGUILayout.Space(4);
                var type = variable.FindPropertyRelative("type");
                type.enumValueIndex = EditorGUILayout.Popup("Тип", type.enumValueIndex, new[] { "Логическая", "Целочисленная" });
                if (type.intValue == (int)CustomVariableType.Integer)
                    EditorGUILayout.PropertyField(variable.FindPropertyRelative("initialIntValue"), new GUIContent("Начальное значение"));
                else
                {
                    var initialValue = variable.FindPropertyRelative("initialValue");
                    initialValue.boolValue = EditorGUILayout.Popup("Начальное значение", initialValue.boolValue ? 0 : 1, new[] { "Да", "Нет" }) == 0;
                }
                EditorGUILayout.Space(8);
                bool remove = GUILayout.Button("Удалить переменную");
                EditorGUILayout.Space(4);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(8);
                if (remove) { variables.DeleteArrayElementAtIndex(i--); }
            }
            if (GUILayout.Button("Добавить переменную"))
            {
                int index = variables.arraySize++;
                var variable = variables.GetArrayElementAtIndex(index);
                variable.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString("N");
                variable.FindPropertyRelative("name").stringValue = "Новая переменная";
                variable.FindPropertyRelative("initialValue").boolValue = false;
                variable.FindPropertyRelative("type").intValue = 0;
                variable.FindPropertyRelative("initialIntValue").intValue = 0;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
