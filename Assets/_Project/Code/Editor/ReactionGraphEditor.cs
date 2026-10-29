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
            EditorGUILayout.LabelField("Действие");
            kind.enumValueIndex = EditorGUILayout.Popup(kind.enumValueIndex, new[] { "Показать текст", "Показать воспоминание", "Перейти в локацию", "Запустить реакцию объекта", "Установить кастомную переменную", "Активность объекта", "Добавить предмет в инвентарь", "Запустить подсказки", "Остановить подсказки", "Использовать предмет" });
            EditorGUILayout.Space(8);
            if (kind.enumValueIndex == 0)
            {
                EditorGUILayout.LabelField("Граф текста");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textGraph"), GUIContent.none);
            }
            else if (kind.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryGraph"), new GUIContent("Граф воспоминания"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryId"), new GUIContent("ID воспоминания"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("firstCompletionText"), new GUIContent("Текст после первого просмотра"));
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
            else if (kind.enumValueIndex == 4)
            {
                var bankProperty = serializedObject.FindProperty("variableBank");
                EditorGUILayout.LabelField("Банк переменных");
                EditorGUILayout.PropertyField(bankProperty, GUIContent.none);
                EditorGUILayout.Space(6);
                var bank = bankProperty.objectReferenceValue as CustomVariableBank;
                if (bank != null)
                {
                    var id = serializedObject.FindProperty("variableId");
                    var names = new string[bank.Variables.Count + 1];
                    names[0] = string.IsNullOrEmpty(id.stringValue) ? "Выберите переменную" : "Переменная не найдена";
                    int selected = 0;
                    for (int i = 0; i < bank.Variables.Count; i++)
                    {
                        var variable = bank.Variables[i];
                        names[i + 1] = (i + 1) + ". " + variable.Name;
                        if (variable.Id == id.stringValue) selected = i + 1;
                    }
                    EditorGUILayout.LabelField("Переменная");
                    EditorGUI.BeginChangeCheck();
                    int chosen = EditorGUILayout.Popup(selected, names);
                    if (EditorGUI.EndChangeCheck()) id.stringValue = chosen == 0 ? "" : bank.Variables[chosen - 1].Id;
                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("Редактировать банк")) Selection.activeObject = bank;
                }
                else EditorGUILayout.HelpBox("Создайте банк через Assets → Create → Memories → Custom Variable Bank или назначьте готовый.", MessageType.Info);
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Значение");
                var value = serializedObject.FindProperty("variableValue");
                value.boolValue = EditorGUILayout.Popup(value.boolValue ? 1 : 0, new[] { "Нет", "Да" }) == 1;
            }
            else if (kind.enumValueIndex == 5)
            {
                var graph = (ReactionGraph)target.graph;
                var key = serializedObject.FindProperty("objectKey");
                var names = new string[graph.ObjectKeys.Count + 1];
                names[0] = string.IsNullOrEmpty(key.stringValue) ? "Выберите объект" : "Не найден: " + key.stringValue;
                int selected = 0;
                for (int i = 0; i < graph.ObjectKeys.Count; i++)
                { names[i + 1] = graph.ObjectKeys[i]; if (names[i + 1] == key.stringValue) selected = i + 1; }
                EditorGUILayout.LabelField("Объект");
                EditorGUI.BeginChangeCheck();
                int chosen = EditorGUILayout.Popup(selected, names);
                if (EditorGUI.EndChangeCheck()) key.stringValue = chosen == 0 ? "" : names[chosen];
                if (GUILayout.Button("Настроить имена объектов")) Selection.activeObject = graph;
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Активность");
                var active = serializedObject.FindProperty("active");
                active.boolValue = EditorGUILayout.Popup(active.boolValue ? 0 : 1, new[] { "Включить", "Выключить" }) == 0;
            }
            else if (kind.enumValueIndex == 7 || kind.enumValueIndex == 8)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hintId"), new GUIContent("ID подсказки"));
                if (kind.enumValueIndex == 7)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hintGraph"), new GUIContent("Граф подсказки"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hintTimeout"), new GUIContent("Таймаут, сек."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("repeatHint"), new GUIContent("Повторять"));
                    EditorGUILayout.HelpBox("Первый запуск после таймаута; повтор — после завершения графа и нового таймаута. Проверки предметов задаются в графе подсказки. Пауза останавливает отсчёт. Повторный запуск того же ID не сбрасывает таймер.", MessageType.Info);
                }
                EditorGUILayout.HelpBox("ID действует в пределах объекта, запустившего граф. При отключении объекта или выгрузке сцены таймеры прекращаются; между запусками игры не сохраняются.", MessageType.Info);
            }
            else if (kind.enumValueIndex == 6 || kind.enumValueIndex == 9)
            {
                EditorGUILayout.LabelField("Предмет");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("item"), GUIContent.none);
            }
            else EditorGUILayout.HelpBox("Запускает включённый компонент CustomReaction на том же объекте, который запустил граф. Нужен ровно один такой компонент. Следующая реакция запускается сразу.", MessageType.Info);
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
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Тип проверки");
            kind.enumValueIndex = EditorGUILayout.Popup(kind.enumValueIndex,
                new[] { "Предмет в инвентаре", "Значение кастомной переменной", "Предмет уже подобран", "Предмет использован" });
            EditorGUILayout.Space(8);
            if (kind.intValue != (int)ReactionConditionKind.CustomVariable)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("item"), new GUIContent("Предмет"));
            else if (kind.intValue == (int)ReactionConditionKind.CustomVariable)
            {
                var bankProperty = serializedObject.FindProperty("variableBank");
                EditorGUILayout.LabelField("Банк переменных");
                EditorGUILayout.PropertyField(bankProperty, GUIContent.none);
                EditorGUILayout.Space(6);
                var bank = bankProperty.objectReferenceValue as CustomVariableBank;
                if (bank != null)
                {
                    var id = serializedObject.FindProperty("variableId");
                    var names = new string[bank.Variables.Count + 1];
                    names[0] = string.IsNullOrEmpty(id.stringValue) ? "Выберите переменную" : "Переменная не найдена";
                    int selected = 0;
                    for (int i = 0; i < bank.Variables.Count; i++)
                    {
                        var variable = bank.Variables[i];
                        names[i + 1] = (i + 1) + ". " + variable.Name;
                        if (variable.Id == id.stringValue) selected = i + 1;
                    }
                    EditorGUILayout.LabelField("Переменная");
                    EditorGUI.BeginChangeCheck();
                    int chosen = EditorGUILayout.Popup(selected, names);
                    if (EditorGUI.EndChangeCheck()) id.stringValue = chosen == 0 ? "" : bank.Variables[chosen - 1].Id;
                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("Редактировать банк")) Selection.activeObject = bank;
                }
                else EditorGUILayout.HelpBox("Создайте банк через Assets → Create → Memories → Custom Variable Bank или назначьте готовый.", MessageType.Info);
            }
            EditorGUILayout.Space(10);
            NodeEditorGUILayout.PortField(new GUIContent("Если да"), target.GetOutputPort("ifPresent"));
            NodeEditorGUILayout.PortField(new GUIContent("Если нет"), target.GetOutputPort("ifAbsent"));
            serializedObject.ApplyModifiedProperties();
            if (!((ReactionConditionNode)target).Validate(out var error))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }
            EditorGUILayout.Space(4);
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
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("objectKeys"), new GUIContent("Имена объектов"), true);
            EditorGUILayout.HelpBox("Задайте уникальные имена. Ссылки на объекты сцены назначаются в разделе «Объекты графа» на запускающем компоненте. При переименовании обновите ноды и привязки.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initializationGraph"), new GUIContent("Граф восстановления"));
            var graph = serializedObject.FindProperty("graph");
            EditorGUILayout.PropertyField(graph, new GUIContent("Граф реакций"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("objectBindings"), new GUIContent("Объекты графа"), true);
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("objectBindings"), new GUIContent("Объекты графа"), true);
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
