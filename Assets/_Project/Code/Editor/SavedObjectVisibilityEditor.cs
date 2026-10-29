using Memories.Gameplay.Reactions;
using UnityEditor;
using UnityEngine;

namespace Memories.Editor
{
    [CustomEditor(typeof(SavedObjectVisibility))]
    public sealed class SavedObjectVisibilityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var bankProperty = serializedObject.FindProperty("variableBank");
            var id = serializedObject.FindProperty("variableId");
            EditorGUILayout.LabelField("Банк переменных");
            EditorGUILayout.PropertyField(bankProperty, GUIContent.none);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Переменная");
            var bank = bankProperty.objectReferenceValue as CustomVariableBank;
            if (bank == null || bank.Variables.Count == 0)
                EditorGUILayout.HelpBox(bank == null ? "Назначьте банк переменных." : "В банке нет переменных.", MessageType.Info);
            else
            {
                var names = new string[bank.Variables.Count];
                int selected = -1;
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = bank.Variables[i].Name;
                    if (bank.Variables[i].Id == id.stringValue) selected = i;
                }
                EditorGUI.BeginChangeCheck();
                int chosen = EditorGUILayout.Popup(selected, names);
                if (EditorGUI.EndChangeCheck() && chosen >= 0)
                    id.stringValue = bank.Variables[chosen].Id;
                if (selected < 0)
                    EditorGUILayout.HelpBox("Выберите существующую переменную из банка.", MessageType.Warning);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
