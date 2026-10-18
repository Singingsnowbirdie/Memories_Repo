using System;
using Memories.Gameplay.Triggers;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeGraphEditor(typeof(TriggerGraph))]
    public sealed class TriggerGraphEditor : NodeGraphEditor
    {
        public override string GetNodeMenuName(Type type)
        {
            if (type == typeof(TriggerStartNode)) return "Начало";
            if (type == typeof(TriggerActionNode)) return "Событие";
            if (type == typeof(TriggerEndNode)) return "Конец";
            return null;
        }

        [MenuItem("Assets/Create/Memories/Trigger Graph")]
        private static void CreateGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject("Создать граф триггера", "TriggerGraph", "asset", "Выберите папку для графа.");
            if (string.IsNullOrEmpty(path)) return;
            var graph = ScriptableObject.CreateInstance<TriggerGraph>();
            var start = graph.AddNode<TriggerStartNode>();
            var action = graph.AddNode<TriggerActionNode>();
            var end = graph.AddNode<TriggerEndNode>();
            start.position = Vector2.zero;
            action.position = new Vector2(300, 0);
            end.position = new Vector2(700, 0);
            start.GetOutputPort("next").Connect(action.GetInputPort("previous"));
            action.GetOutputPort("next").Connect(end.GetInputPort("previous"));
            AssetDatabase.CreateAsset(graph, path);
            foreach (var node in graph.nodes) AssetDatabase.AddObjectToAsset(node, graph);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            Selection.activeObject = graph;
            NodeEditorWindow.Open(graph);
        }
    }

    [CustomNodeEditor(typeof(TriggerStartNode))]
    public sealed class TriggerStartNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Начало", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerOnce"), new GUIContent("Одноразовый"));
            EditorGUILayout.HelpBox("Выключено: повтор при новом входе. Состояние хранится отдельно у каждого триггера до выгрузки сцены.", MessageType.Info);
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomNodeEditor(typeof(TriggerActionNode))]
    public sealed class TriggerActionNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Событие", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
            var kind = serializedObject.FindProperty("kind");
            kind.enumValueIndex = EditorGUILayout.Popup("Действие", kind.enumValueIndex, new[] { "Показать текст", "Показать воспоминание" });
            if (kind.enumValueIndex == 0)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textGraph"), new GUIContent("Граф текста"));
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryGraph"), new GUIContent("Граф воспоминания"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryId"), new GUIContent("ID воспоминания"));
            }
            serializedObject.ApplyModifiedProperties();
            if (!((TriggerActionNode)target).Validate(out var error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }

    [CustomNodeEditor(typeof(TriggerEndNode))]
    public sealed class TriggerEndNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Конец", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI() => NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
    }

    [CustomEditor(typeof(TriggerGraph))]
    public sealed class TriggerGraphInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = (TriggerGraph)target;
            if (GUILayout.Button("Открыть граф триггера")) NodeEditorWindow.Open(graph);
            EditorGUILayout.HelpBox("Настройте повторяемость в ноде «Начало». События выполняются последовательно; следующее ждёт завершения предыдущего.", MessageType.Info);
            if (graph.TryGetSequence(out var actions, out var error))
                EditorGUILayout.HelpBox((graph.TriggerOnce ? "Одноразовый" : "Многоразовый") + ". Событий: " + actions.Count, MessageType.Info);
            else EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }

    [CustomEditor(typeof(SceneTrigger))]
    public sealed class SceneTriggerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var graph = serializedObject.FindProperty("graph");
            EditorGUILayout.PropertyField(graph, new GUIContent("Граф триггера"));
            serializedObject.ApplyModifiedProperties();
            if (graph.objectReferenceValue is TriggerGraph asset)
            {
                if (GUILayout.Button("Открыть граф")) NodeEditorWindow.Open(asset);
                if (!asset.TryGetSequence(out _, out var error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
            }
            else EditorGUILayout.HelpBox("Создайте Memories → Trigger Graph и назначьте его сюда. События и повторяемость задаются в графе.", MessageType.Info);
        }
    }
}
