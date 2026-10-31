using System.Linq;
using Memories.Gameplay.Reactions;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeEditor(typeof(ReactionRandomNode))]
    public sealed class ReactionRandomNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Случайный выбор", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            var node = (ReactionRandomNode)target;
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), node.GetInputPort("previous"));
            var choices = serializedObject.FindProperty("choices");
            double total = node.Choices.Where(c => c.Weight > 0 && !float.IsInfinity(c.Weight)).Sum(c => (double)c.Weight);
            int remove = -1;
            for (int i = 0; i < choices.arraySize; i++)
            {
                var choice = choices.GetArrayElementAtIndex(i);
                var weight = choice.FindPropertyRelative("weight");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(weight, new GUIContent("Вес " + (i + 1)));
                if (GUILayout.Button("−", GUILayout.Width(24))) remove = i;
                EditorGUILayout.EndHorizontal();
                string chance = total > 0 && weight.floatValue >= 0 && !float.IsInfinity(weight.floatValue)
                    ? (100d * weight.floatValue / total).ToString("0.##") + "%" : "—";
                NodeEditorGUILayout.PortField(new GUIContent("Выход " + (i + 1) + " · " + chance),
                    node.GetOutputPort(choice.FindPropertyRelative("portName").stringValue));
            }
            serializedObject.ApplyModifiedProperties();
            bool add = GUILayout.Button("Добавить выход");
            if (add || remove >= 0)
            {
                Undo.RecordObjects(node.graph.nodes.Cast<UnityEngine.Object>().ToArray(), "Изменить выходы случайного выбора");
                if (remove >= 0) node.RemoveChoice(remove);
                if (add) node.AddChoice();
                foreach (var item in node.graph.nodes) EditorUtility.SetDirty(item);
                EditorUtility.SetDirty(node.graph);
                NodeEditorWindow.RepaintAll();
            }
            EditorGUILayout.HelpBox("Шанс = вес / сумма весов. Вес 0 отключает ветку. Каждый выход нужно подключить к следующей ноде.", MessageType.Info);
            if (!node.Validate(out var error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }
}
