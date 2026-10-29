using System;
using Memories.Gameplay.Story;
using Memories.Gameplay.Reactions;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Memories.Editor
{
    [CustomNodeGraphEditor(typeof(StoryTextGraph))]
    public sealed class StoryTextGraphEditor : NodeGraphEditor
    {
        private static string NavigationKey(NodeEditorWindow editorWindow) =>
            "Memories.TextGraphBack." + editorWindow.GetInstanceID();

        [UnityEditor.Callbacks.OnOpenAsset(-1)]
        private static bool OpenTextAsset(int instanceId, int line)
        {
            if (!(EditorUtility.InstanceIDToObject(instanceId) is StoryTextGraph graph)) return false;
            OpenGraph(graph);
            return true;
        }

        internal static void OpenGraph(StoryTextGraph graph)
        {
            var editorWindow = EditorWindow.GetWindow<NodeEditorWindow>();
            string key = NavigationKey(editorWindow);
            if (editorWindow.graph is ReactionGraph reaction)
            {
                SessionState.SetInt(key + ".parent", reaction.GetInstanceID());
                SessionState.SetInt(key + ".text", graph.GetInstanceID());
                SessionState.SetVector3(key + ".pan", editorWindow.panOffset);
                SessionState.SetFloat(key + ".zoom", editorWindow.zoom);
            }
            else if (editorWindow.graph != graph)
            {
                SessionState.EraseInt(key + ".parent");
                SessionState.EraseInt(key + ".text");
            }
            NodeEditorWindow.Open(graph);
        }

        public override void OnGUI()
        {
            string key = NavigationKey(window);
            if (SessionState.GetInt(key + ".text", 0) != target.GetInstanceID()) return;
            var parent = EditorUtility.InstanceIDToObject(SessionState.GetInt(key + ".parent", 0)) as ReactionGraph;
            if (parent == null) return;
            if (GUI.Button(new Rect(10, 10, 90, 24), new GUIContent("← Назад", parent.name)))
            {
                Vector2 pan = SessionState.GetVector3(key + ".pan", Vector3.zero);
                float zoom = SessionState.GetFloat(key + ".zoom", 1f);
                EditorApplication.delayCall += () =>
                {
                    if (parent == null) return;
                    var editorWindow = NodeEditorWindow.Open(parent);
                    editorWindow.panOffset = pan;
                    editorWindow.zoom = zoom;
                    Selection.activeObject = parent;
                };
            }
        }

        public override string GetNodeMenuName(Type type)
        {
            if (type == typeof(StoryTextStartNode)) return "Начало";
            if (type == typeof(StoryTextNode)) return "Текст";
            if (type == typeof(StoryTextEndNode)) return "Конец";
            return null;
        }

        [MenuItem("Assets/Create/Memories/Text Graph")]
        private static void CreateGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject("Создать граф текста", "TextGraph", "asset", "Выберите место для графа текста.");
            if (string.IsNullOrEmpty(path)) return;
            var graph = ScriptableObject.CreateInstance<StoryTextGraph>();
            var start = graph.AddNode<StoryTextStartNode>();
            var text = graph.AddNode<StoryTextNode>();
            var end = graph.AddNode<StoryTextEndNode>();
            start.position = Vector2.zero;
            text.position = new Vector2(220, 0);
            end.position = new Vector2(640, 0);
            start.GetOutputPort("next").Connect(text.GetInputPort("previous"));
            text.GetOutputPort("next").Connect(end.GetInputPort("previous"));
            AssetDatabase.CreateAsset(graph, path);
            foreach (var node in graph.nodes) AssetDatabase.AddObjectToAsset(node, graph);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            Selection.activeObject = graph;
            NodeEditorWindow.Open(graph);
        }
    }

    [CustomNodeEditor(typeof(StoryTextStartNode))]
    public sealed class StoryTextStartNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Начало", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            using (var settings = new SerializedObject(target.graph))
                StoryTextGraphInspector.DrawPlaybackSettings(settings);
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
        }
    }

    [CustomNodeEditor(typeof(StoryTextEndNode))]
    public sealed class StoryTextEndNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Конец", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI() => NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
    }

    [CustomNodeEditor(typeof(StoryTextNode))]
    public sealed class StoryTextNodeEditor : NodeEditor
    {
        public override void OnHeaderGUI() => GUILayout.Label("Текст", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("text"), new GUIContent("Текст"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"), new GUIContent("Показ, сек.", "Время чтения после появления текста. Следующий фрагмент сменит его через фейд."));
            serializedObject.ApplyModifiedProperties();
            var node = (StoryTextNode)target;
            if (string.IsNullOrWhiteSpace(node.Text)) EditorGUILayout.HelpBox("Введите текст фрагмента.", MessageType.Warning);
            if (float.IsNaN(node.Duration) || float.IsInfinity(node.Duration) || node.Duration <= 0)
                EditorGUILayout.HelpBox("Укажите положительное время показа.", MessageType.Warning);
        }
    }

    [CustomEditor(typeof(StoryTextGraph))]
    public sealed class StoryTextGraphInspector : UnityEditor.Editor
    {
        internal static void DrawPlaybackSettings(SerializedObject settings)
        {
            settings.Update();
            EditorGUILayout.LabelField("Пауза между фразами, сек.");
            EditorGUILayout.PropertyField(settings.FindProperty("pauseBetweenFragments"), GUIContent.none);
            EditorGUILayout.PropertyField(settings.FindProperty("blockInteractionsForWholeSequence"),
                new GUIContent("Непрерываемая история", "Включено: взаимодействия запрещены до конца цепочки. Выключено: взаимодействия разрешены всегда; после минимум двух секунд чтения текущая фраза может смениться текстом интеракции на той же плашке. Таймер паузы исходной цепочки приостанавливается."));
            settings.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            var graph = (StoryTextGraph)target;
            DrawPlaybackSettings(serializedObject);
            EditorGUILayout.HelpBox("Соедините ноды: Начало → Текст → … → Конец. Тексты автоматически сменяются через фейд в нижней плашке.", MessageType.Info);
            if (GUILayout.Button("Открыть граф текста")) StoryTextGraphEditor.OpenGraph(graph);
            if (graph.TryGetSequence(out var fragments, out var error))
                EditorGUILayout.HelpBox("Граф готов. Фрагментов: " + fragments.Count, MessageType.Info);
            else EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }
}
