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
            if (type == typeof(ReactionRandomNode)) return "Случайный выбор";
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
        private void DrawCustomReaction(ReactionGraph graph, SerializedProperty key)
        {
            var names = new System.Collections.Generic.List<string>();
            var prefabNames = new System.Collections.Generic.List<string>();
            void Collect(Component owner)
            {
                using (var settings = new SerializedObject(owner))
                {
                    var graphProperty = settings.FindProperty("graph") ?? settings.FindProperty("interactionGraph");
                    var initialization = settings.FindProperty("initializationGraph");
                    if (graphProperty?.objectReferenceValue != graph && initialization?.objectReferenceValue != graph) return;
                    var bindings = settings.FindProperty("customReactions");
                    if (bindings == null) return;
                    var result = EditorUtility.IsPersistent(owner) ? prefabNames : names;
                    for (int i = 0; i < bindings.arraySize; i++)
                    {
                        var binding = bindings.GetArrayElementAtIndex(i);
                        if (binding.FindPropertyRelative("reaction").objectReferenceValue == null) continue;
                        string name = binding.FindPropertyRelative("key").stringValue;
                        if (!result.Contains(name)) result.Add(name);
                    }
                }
            }
            foreach (var owner in Resources.FindObjectsOfTypeAll<ReactionInteractable>()) Collect(owner);
            foreach (var owner in Resources.FindObjectsOfTypeAll<SceneTrigger>()) Collect(owner);
            if (names.Count == 0) names = prefabNames;

            EditorGUILayout.LabelField("Кастомная реакция");
            string selected = key.stringValue;
            bool found = names.Contains(selected);
            string label = found ? (string.IsNullOrEmpty(selected) ? "Основная реакция" : selected) : "Выберите реакцию";
            using (new EditorGUI.DisabledScope(names.Count == 0))
                if (EditorGUILayout.DropdownButton(new GUIContent(label), FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    foreach (string name in names)
                    {
                        string chosen = name;
                        menu.AddItem(new GUIContent(string.IsNullOrEmpty(name) ? "Основная реакция" : name), name == selected, () =>
                        {
                            if (target == null) return;
                            using (var settings = new SerializedObject(target))
                            {
                                settings.FindProperty("objectKey").stringValue = chosen;
                                settings.ApplyModifiedProperties();
                            }
                            NodeEditorWindow.RepaintAll();
                        });
                    }
                    menu.ShowAsContext();
                }
            if (!found)
                EditorGUILayout.HelpBox(names.Count == 0
                    ? "Назначьте CustomReaction в инспекторе объекта с этим графом."
                    : "Прежняя привязка отсутствует. Выберите назначенную реакцию.", MessageType.Warning);
        }

        public override void OnHeaderGUI() => GUILayout.Label("Реакция", NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            NodeEditorGUILayout.PortField(new GUIContent("Вход"), target.GetInputPort("previous"));
            NodeEditorGUILayout.PortField(new GUIContent("Далее"), target.GetOutputPort("next"));
            var kind = serializedObject.FindProperty("kind");
            EditorGUILayout.LabelField("Действие");
            // Keep existing serialized add/remove actions while presenting one inventory action.
            int displayedKind = kind.intValue == (int)ReactionKind.ConsumeInventoryItem
                ? (int)ReactionKind.AddInventoryItem : kind.intValue;
            EditorGUI.BeginChangeCheck();
            int chosenKind = EditorGUILayout.IntPopup(displayedKind,
                new[] { "Показать текст", "Показать воспоминание", "Перейти в локацию", "Запустить реакцию объекта", "Установить кастомную переменную", "Активность объекта", "Инвентарный предмет", "Запустить подсказки", "Остановить подсказки", "Интерактивность предмета", "Запустить граф реакций" },
                new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11 });
            if (EditorGUI.EndChangeCheck()) kind.intValue = chosenKind;
            EditorGUILayout.Space(8);
            if (kind.intValue == (int)ReactionKind.RunGraph)
            {
                EditorGUILayout.LabelField("Граф реакций");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("reactionGraph"), GUIContent.none);
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("Выполняется до конца, затем продолжается текущая цепочка. Объекты передаются по именам из привязок вызывающего объекта.", MessageType.Info);
            }
            else if (kind.enumValueIndex == 0)
            {
                EditorGUILayout.LabelField("Граф текста");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textGraph"), GUIContent.none);
            }
            else if (kind.enumValueIndex == 1)
            {
                EditorGUILayout.LabelField("Граф воспоминания");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryGraph"), GUIContent.none);
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("ID воспоминания");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryId"), GUIContent.none);
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Текст после первого просмотра");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("firstCompletionText"), GUIContent.none);
                EditorGUILayout.Space(8);
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
                }
                else EditorGUILayout.HelpBox("Создайте банк через Assets → Create → Memories → Custom Variable Bank или назначьте готовый.", MessageType.Info);
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Значение");
                if (bank?.Find(serializedObject.FindProperty("variableId").stringValue)?.Type == CustomVariableType.Integer)
                {
                    var add = serializedObject.FindProperty("addToIntValue");
                    add.boolValue = EditorGUILayout.Popup("Операция", add.boolValue ? 1 : 0,
                        new[] { "Присвоить", "Прибавить" }) == 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("variableIntValue"), GUIContent.none);
                }
                else
                {
                    var value = serializedObject.FindProperty("variableValue");
                    value.boolValue = EditorGUILayout.Popup(value.boolValue ? 1 : 0, new[] { "Нет", "Да" }) == 1;
                }
            }
            else if (kind.enumValueIndex == 3)
            {
                DrawCustomReaction((ReactionGraph)target.graph, serializedObject.FindProperty("objectKey"));
            }
            else if (kind.enumValueIndex == 5 || kind.enumValueIndex == 10)
            {
                bool interactivity = kind.enumValueIndex == 10;
                var graph = (ReactionGraph)target.graph;
                var key = serializedObject.FindProperty("objectKey");
                int offset = interactivity ? 1 : 0;
                var names = new string[graph.ObjectKeys.Count + offset];
                if (interactivity) names[0] = "Объект, запустивший граф";
                int selected = interactivity && string.IsNullOrEmpty(key.stringValue) ? 0 : -1;
                for (int i = 0; i < graph.ObjectKeys.Count; i++)
                { names[i + offset] = graph.ObjectKeys[i]; if (names[i + offset] == key.stringValue) selected = i + offset; }
                EditorGUILayout.LabelField("Объект");
                using (new EditorGUI.DisabledScope(names.Length == 0))
                {
                    EditorGUI.BeginChangeCheck();
                    int chosen = EditorGUILayout.Popup(selected, names);
                    if (EditorGUI.EndChangeCheck() && chosen >= 0)
                        key.stringValue = interactivity && chosen == 0 ? "" : names[chosen];
                }
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField(interactivity ? "Интерактивность" : "Активность");
                var active = serializedObject.FindProperty(interactivity ? "interactionEnabled" : "active");
                active.boolValue = EditorGUILayout.Popup(active.boolValue ? 0 : 1, new[] { "Включить", "Выключить" }) == 0;
                if (!interactivity && active.boolValue)
                {
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("Проявление, сек. (0 — сразу)");
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("revealDuration"), GUIContent.none);
                    if (serializedObject.FindProperty("revealDuration").floatValue > 0)
                    {
                        EditorGUILayout.Space(8);
                        EditorGUILayout.LabelField("Задержка, сек.");
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("revealDelay"), GUIContent.none);
                        EditorGUILayout.Space(8);
                        EditorGUILayout.LabelField("Шейдер проявления");
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("revealShader"), GUIContent.none);
                    }
                }
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
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Операция");
                int operation = kind.intValue == (int)ReactionKind.ConsumeInventoryItem ? 1 : 0;
                EditorGUI.BeginChangeCheck();
                operation = EditorGUILayout.Popup(operation, new[] { "Добавить", "Убрать" });
                if (EditorGUI.EndChangeCheck())
                    kind.intValue = (int)(operation == 0 ? ReactionKind.AddInventoryItem : ReactionKind.ConsumeInventoryItem);
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
                new[] { "Предмет в инвентаре", "Значение кастомной переменной" });
            EditorGUILayout.Space(8);
            if (kind.intValue != (int)ReactionConditionKind.CustomVariable)
            {
                EditorGUILayout.LabelField("Предмет");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("item"), GUIContent.none);
            }
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
                }
                else EditorGUILayout.HelpBox("Создайте банк через Assets → Create → Memories → Custom Variable Bank или назначьте готовый.", MessageType.Info);
            }
            if (kind.intValue == (int)ReactionConditionKind.CustomVariable)
            {
                var bank = serializedObject.FindProperty("variableBank").objectReferenceValue as CustomVariableBank;
                if (bank?.Find(serializedObject.FindProperty("variableId").stringValue)?.Type == CustomVariableType.Integer)
                {
                    var comparison = serializedObject.FindProperty("integerComparison");
                    comparison.enumValueIndex = EditorGUILayout.Popup("Сравнение", comparison.enumValueIndex,
                        new[] { "=", "≠", "<", "≤", ">", "≥" });
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("integerValue"), new GUIContent("Значение"));
                }
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customReactions"), new GUIContent("Кастомные реакции"), true);
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customReactions"), new GUIContent("Кастомные реакции"), true);
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
