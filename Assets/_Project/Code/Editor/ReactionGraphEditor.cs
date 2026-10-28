using System;
using Memories.Gameplay.Triggers;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Interaction;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeGraphEditor(typeof(ReactionGraph))]
    public sealed class ReactionGraphEditor : NodeGraphEditor
    {
        public override string GetNodeMenuName(Type type)
        {
            if (type == typeof(ReactionStartNode)) return "Начало";
            if (type == typeof(ReactionNode)) return "Реакция";
            if (type == typeof(ReactionConditionNode)) return "Условие";
            if (type == typeof(ReactionEndNode)) return "Конец";
            return null;
        }

        [MenuItem("Assets/Create/Memories/Reaction Graph")]
        private static void CreateGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject("Создать граф реакций", "ReactionGraph", "asset", "Выберите папку для графа.");
            if (string.IsNullOrEmpty(path)) return;
            var graph = ScriptableObject.CreateInstance<ReactionGraph>();
            var start = graph.AddNode<ReactionStartNode>();
            var action = graph.AddNode<ReactionNode>();
            var end = graph.AddNode<ReactionEndNode>();
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

    [CustomNodeEditor(typeof(ReactionStartNode))]
    public sealed class ReactionStartNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Начало", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("runOnce"), new GUIContent("Одноразовый"));
            EditorGUILayout.HelpBox("Выключено: повторный запуск разрешён. Состояние хранится отдельно у каждого объекта до выгрузки сцены.", MessageType.Info);
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomNodeEditor(typeof(ReactionNode))]
    public sealed class ReactionNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Реакция", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
            var kind = serializedObject.FindProperty("kind");
            kind.enumValueIndex = EditorGUILayout.Popup("Действие", kind.enumValueIndex, new[] { "Показать текст", "Показать воспоминание", "Перейти в локацию", "Вспомнить фотографию" });
            if (kind.enumValueIndex == 0)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textGraph"), new GUIContent("Граф текста"));
            else if (kind.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryGraph"), new GUIContent("Граф воспоминания"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryId"), new GUIContent("ID воспоминания"));
            }
            else if (kind.enumValueIndex == 2)
            {
                var path = serializedObject.FindProperty("locationScenePath");
                var current = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
                EditorGUI.BeginChangeCheck();
                var scene = (SceneAsset)EditorGUILayout.ObjectField("Сцена локации", current, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck()) path.stringValue = scene == null ? "" : AssetDatabase.GetAssetPath(scene);
                EditorGUILayout.HelpBox("Переход завершает цепочку. Сцена должна быть включена в Build Settings.", MessageType.Info);
            }
            else EditorGUILayout.HelpBox("Для PhotoMemory: сохраняет воспоминание, проявляет фотографию и включает связанного персонажа. Следующая реакция запускается сразу.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            if (!((ReactionNode)target).Validate(out var error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }

    [CustomNodeEditor(typeof(ReactionConditionNode))]
    public sealed class ReactionConditionNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Условие", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
            var kind = serializedObject.FindProperty("kind");
            kind.enumValueIndex = EditorGUILayout.Popup("Тип проверки", kind.enumValueIndex,
                new[] { "Предмет в инвентаре" });
            if (kind.intValue == (int)ReactionConditionKind.InventoryItem)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("item"), new GUIContent("Предмет"));
            NodeEditorGUILayout.PortField(new GUIContent("Если да"), target.GetOutputPort("ifPresent"));
            NodeEditorGUILayout.PortField(new GUIContent("Если нет"), target.GetOutputPort("ifAbsent"));
            serializedObject.ApplyModifiedProperties();
            if (!((ReactionConditionNode)target).Validate(out var error))
                EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }

    [CustomNodeEditor(typeof(ReactionEndNode))]
    public sealed class ReactionEndNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Конец", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI() => NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
    }

    [CustomEditor(typeof(ReactionGraph))]
    public sealed class ReactionGraphInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = (ReactionGraph)target;
            if (GUILayout.Button("Открыть граф реакций")) NodeEditorWindow.Open(graph);
            EditorGUILayout.HelpBox("Настройте повторяемость в ноде «Начало». Реакции выполняются последовательно; каждая ждёт завершения предыдущей.", MessageType.Info);
            if (graph.TryGetSequence(out var actions, out var error))
                EditorGUILayout.HelpBox((graph.RunOnce ? "Одноразовый" : "Многоразовый") + ". Реакций: " + actions.Count, MessageType.Info);
            else EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }

    [CustomEditor(typeof(ReactionInteractable))]
    public sealed class ReactionInteractableInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prompt"), new GUIContent("Подсказка"));
            var graph = serializedObject.FindProperty("graph");
            EditorGUILayout.PropertyField(graph, new GUIContent("Граф реакций"));
            serializedObject.ApplyModifiedProperties();
            if (graph.objectReferenceValue is ReactionGraph asset)
            {
                if (GUILayout.Button("Открыть граф реакций")) NodeEditorWindow.Open(asset);
                if (!asset.TryGetSequence(out _, out var error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
            }
            else EditorGUILayout.HelpBox("Назначьте граф реакций. На объекте или его дочернем объекте нужен обычный коллайдер, без Is Trigger.", MessageType.Info);
        }
    }

    [CustomEditor(typeof(SceneTrigger))]
    public sealed class SceneTriggerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var graph = serializedObject.FindProperty("graph");
            EditorGUILayout.PropertyField(graph, new GUIContent("Граф реакций"));
            serializedObject.ApplyModifiedProperties();
            if (graph.objectReferenceValue is ReactionGraph asset)
            {
                if (GUILayout.Button("Открыть граф")) NodeEditorWindow.Open(asset);
                if (!asset.TryGetSequence(out _, out var error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
            }
            else EditorGUILayout.HelpBox("Создайте Memories → Reaction Graph и назначьте его сюда. Реакции и повторяемость задаются в графе.", MessageType.Info);
        }
    }
}
