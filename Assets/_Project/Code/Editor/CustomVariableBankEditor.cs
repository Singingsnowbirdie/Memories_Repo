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
            EditorGUILayout.HelpBox("Логические переменные. Начальное значение используется, пока в сохранении нет значения переменной. Переименование сохраняет её ID и связи в графах.", MessageType.Info);
            EditorGUILayout.Space(8);
            var variables = serializedObject.FindProperty("variables");
            for (int i = 0; i < variables.arraySize; i++)
            {
                var variable = variables.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(variable.FindPropertyRelative("name"), new GUIContent("Имя"));
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(variable.FindPropertyRelative("initialValue"), new GUIContent("Начальное значение"));
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
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
