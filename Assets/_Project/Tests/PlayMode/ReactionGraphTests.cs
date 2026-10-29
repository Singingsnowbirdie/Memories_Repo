using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Story;
using Memories.Gameplay.Triggers;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Interaction;
using Memories.Contracts;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using XNode;

namespace Memories.Tests.PlayMode
{
    public sealed class ReactionGraphTests
    {
        private readonly List<Object> owned = new List<Object>();
        private ReactionGraph graph;
        private StoryTextGraph textGraph;
        private MemoryGraph memoryGraph;
        private ReactionStartNode start;
        private ReactionNode first, memory, last;
        private ReactionEndNode end;

        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Connect(Node from, Node to) => from.GetOutputPort("next").Connect(to.GetInputPort("previous"));

        [SetUp]
        public void CreateGraphs()
        {
            textGraph = Own(ScriptableObject.CreateInstance<StoryTextGraph>());
            var ts = Own(textGraph.AddNode<StoryTextStartNode>());
            var text = Own(textGraph.AddNode<StoryTextNode>());
            Set(text, "text", "Trigger graph test"); Set(text, "duration", .02f);
            var te = Own(textGraph.AddNode<StoryTextEndNode>());
            Connect(ts, text); Connect(text, te);
            memoryGraph = Own(ScriptableObject.CreateInstance<MemoryGraph>());
            var ms = Own(memoryGraph.AddNode<MemoryStartNode>());
            var frame = Own(memoryGraph.AddNode<MemoryFrameNode>());
            Set(frame, "image", Texture2D.whiteTexture);
            Set(frame, "transition", MemoryFrameTransition.Instant);
            Set(frame, "displayDuration", .1f);
            var me = Own(memoryGraph.AddNode<MemoryEndNode>());
            Connect(ms, frame); Connect(frame, me);
            graph = Own(ScriptableObject.CreateInstance<ReactionGraph>());
            start = Own(graph.AddNode<ReactionStartNode>());
            first = Own(graph.AddNode<ReactionNode>());
            memory = Own(graph.AddNode<ReactionNode>());
            last = Own(graph.AddNode<ReactionNode>());
            end = Own(graph.AddNode<ReactionEndNode>());
            Set(first, "textGraph", textGraph); Set(last, "textGraph", textGraph);
            Set(memory, "kind", ReactionKind.Memory);
            Set(memory, "memoryGraph", memoryGraph); Set(memory, "memoryId", "graph-test-memory");
            Connect(start, first); Connect(first, memory); Connect(memory, last); Connect(last, end);
        }

        [Test]
        public void InteractivityCanBeDisabledAndEnabledWithoutHidingOwner()
        {
            var owner = Own(new GameObject("Interaction owner"));
            var interactable = owner.AddComponent<ReactionInteractable>();
            Set(start, "runOnce", false);
            Set(first, "kind", ReactionKind.ObjectInteractivity);
            Set(memory, "kind", ReactionKind.ObjectInteractivity);
            Set(last, "kind", ReactionKind.ObjectInteractivity);
            interactable.Configure(graph, "Interact");
            interactable.Initialize(null, null);

            Assert.IsTrue(interactable.CanInteract);
            interactable.Interact();

            Assert.IsFalse(interactable.CanInteract);
            Assert.IsTrue(owner.activeSelf);
            Assert.IsTrue(interactable.enabled);
            Assert.IsFalse(interactable.IsRunning, "Disabling interaction must let the graph reach its end.");
            Set(last, "interactionEnabled", true);
            using (var runner = new ReactionRunner(null, null, interactable))
                Assert.IsTrue(runner.TryRun(graph));
            Assert.IsTrue(interactable.CanInteract);
        }

        [Test]
        public void InteractivityChangesOnlyTheSelectedObject()
        {
            var owner = Own(new GameObject("Graph owner")).AddComponent<ReactionInteractable>();
            var target = Own(new GameObject("Selected object")).AddComponent<ReactionInteractable>();
            Set(start, "runOnce", false);
            Set(graph, "objectKeys", new List<string> { "Target" });
            foreach (var action in new[] { first, memory, last })
            {
                Set(action, "kind", ReactionKind.ObjectInteractivity);
                Set(action, "objectKey", "Target");
            }
            owner.Configure(graph, "Interact");
            target.Configure(graph, "Interact");
            owner.Initialize(null, null);
            target.Initialize(null, null);
            using (var runner = new ReactionRunner(null, null, owner,
                objectBindings: new[] { new ReactionObjectBinding("Target", target.gameObject) }))
            {
                Assert.IsTrue(runner.TryRun(graph));
                Assert.IsTrue(owner.CanInteract);
                Assert.IsFalse(target.CanInteract);
                Set(last, "interactionEnabled", true);
                Assert.IsTrue(runner.TryRun(graph));
                Assert.IsTrue(target.CanInteract);
                Assert.IsTrue(target.gameObject.activeSelf);
            }
        }

        [Test]
        public void SeparateBranchEndsAreValidButDisconnectedEndsAreRejected()
        {
            var branchGraph = Own(ScriptableObject.CreateInstance<ReactionGraph>());
            var entry = Own(branchGraph.AddNode<ReactionStartNode>());
            var condition = Own(branchGraph.AddNode<ReactionConditionNode>());
            var presentEnd = Own(branchGraph.AddNode<ReactionEndNode>());
            var absentEnd = Own(branchGraph.AddNode<ReactionEndNode>());
            var item = Own(ScriptableObject.CreateInstance<QuestItem>());
            Set(item, "id", "multiple-ends-test");
            Set(condition, "item", item);
            Set(entry, "runOnce", false);
            Connect(entry, condition);
            condition.GetOutputPort("ifPresent").Connect(presentEnd.GetInputPort("previous"));
            condition.GetOutputPort("ifAbsent").Connect(absentEnd.GetInputPort("previous"));

            Assert.IsTrue(branchGraph.TryGetSequence(out _, out var error), error);
            var inventory = new QuestInventory();
            using (var runner = new ReactionRunner(null, null, null, inventory: inventory))
            {
                Assert.IsTrue(runner.TryRun(branchGraph));
                Assert.IsFalse(runner.IsRunning);
                inventory.TryAdd(item);
                Assert.IsTrue(runner.TryRun(branchGraph));
                Assert.IsFalse(runner.IsRunning);
            }

            Own(branchGraph.AddNode<ReactionEndNode>());
            Assert.IsFalse(branchGraph.TryGetSequence(out _, out _));
        }

        [Test]
        public void ValidatesWholeChainAndReferencedGraphs()
        {
            Assert.IsTrue(graph.TryGetSequence(out var steps, out var error), error);
            CollectionAssert.AreEqual(new[] { first, memory, last }, steps);
            Set(first, "textGraph", null);
            Assert.IsFalse(graph.TryGetSequence(out _, out _));
            Set(first, "textGraph", textGraph);
            last.GetOutputPort("next").ClearConnections();
            Connect(last, first);
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "Cycles must be rejected.");
            last.GetOutputPort("next").ClearConnections();
            Connect(start, first); Connect(last, end);
            var disconnected = Own(graph.AddNode<ReactionNode>());
            Set(disconnected, "textGraph", textGraph);
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "Disconnected actions must be rejected.");
        }

        [Test]
        public void InventoryConditionSelectsBranchAndAllowsSharedContinuation()
        {
            var item = Own(ScriptableObject.CreateInstance<QuestItem>());
            Set(item, "id", "remote");
            var condition = Own(graph.AddNode<ReactionConditionNode>());
            Set(condition, "item", item);
            start.GetOutputPort("next").ClearConnections();
            first.GetOutputPort("next").ClearConnections();
            Connect(start, condition);
            condition.GetOutputPort("ifPresent").Connect(first.GetInputPort("previous"));
            condition.GetOutputPort("ifAbsent").Connect(memory.GetInputPort("previous"));
            Connect(first, last);
            Assert.IsTrue(graph.TryGetSequence(out var actions, out var error), error);
            Assert.AreEqual(3, actions.Count, "Shared continuation must only be validated once.");
            var inventory = new QuestInventory();
            Assert.AreEqual("ifAbsent", condition.SelectOutput(inventory));
            inventory.TryAdd(item);
            Assert.AreEqual("ifPresent", condition.SelectOutput(inventory));
            Assert.IsTrue(inventory.Contains(item.Id), "Checking must not consume the item.");
            condition.GetOutputPort("ifAbsent").ClearConnections();
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "Both branches must be connected.");
            condition.GetOutputPort("ifAbsent").Connect(condition.GetInputPort("previous"));
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "Branch cycles must be rejected.");
            condition.GetOutputPort("ifAbsent").ClearConnections();
            condition.GetOutputPort("ifAbsent").Connect(memory.GetInputPort("previous"));
            Set(condition, "item", null);
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "A missing item must be rejected.");
        }

        [Test]
        public void TemporaryVariablesIgnoreAndPreserveSavedProgress()
        {
            string key = "Memories.Tests.Variables." + System.Guid.NewGuid().ToString("N");
            try
            {
                var bank = Own(ScriptableObject.CreateInstance<CustomVariableBank>());
                Set(bank, "bankId", "temporary-test");
                var variable = new CustomVariableBank.Variable("Flag");
                Set(bank, "variables", new List<CustomVariableBank.Variable> { variable });
                new CustomVariableStore(key).Set(bank, variable.Id, true);
                string before = PlayerPrefs.GetString(key);
                var temporary = new CustomVariableStore(key, persistProgress: false);
                Assert.IsFalse(temporary.Get(bank, variable.Id));
                temporary.Set(bank, variable.Id, true);
                Assert.IsTrue(temporary.Get(bank, variable.Id));
                temporary.Set(bank, variable.Id, false);
                Assert.AreEqual(before, PlayerPrefs.GetString(key));
                Assert.IsTrue(new CustomVariableStore(key).Get(bank, variable.Id));
            }
            finally { CustomVariableStore.Clear(key); }
        }

        [Test]
        public void CustomVariableConditionPersistsValuesAndSurvivesRename()
        {
            string key = "Memories.Tests.Variables." + System.Guid.NewGuid().ToString("N");
            try
            {
                var bank = Own(ScriptableObject.CreateInstance<CustomVariableBank>());
                Set(bank, "bankId", "test-bank");
                var variable = new CustomVariableBank.Variable("Door opened");
                var variables = new List<CustomVariableBank.Variable> { variable };
                Set(bank, "variables", variables);
                var condition = Own(graph.AddNode<ReactionConditionNode>());
                Set(condition, "kind", ReactionConditionKind.CustomVariable);
                Set(condition, "variableBank", bank);
                Set(condition, "variableId", variable.Id);
                Assert.IsTrue(condition.Validate(out var error), error);
                var store = new CustomVariableStore(key);
                Assert.AreEqual("ifAbsent", condition.SelectOutput(null, variables: store));
                store.Set(bank, variable.Id, true);
                Set(variable, "name", "Renamed variable");
                var restored = new CustomVariableStore(key);
                Assert.AreEqual("ifPresent", condition.SelectOutput(null, variables: restored));
                restored.Set(bank, variable.Id, false);
                Assert.AreEqual("ifAbsent", condition.SelectOutput(null, variables: restored));
                var otherBank = Own(ScriptableObject.CreateInstance<CustomVariableBank>());
                Set(otherBank, "bankId", "other-bank");
                Set(otherBank, "variables", variables);
                Assert.IsFalse(restored.Get(otherBank, variable.Id), "Banks must have independent values.");
                variables.Clear();
                Assert.IsFalse(condition.Validate(out _), "Deleted variables must invalidate the condition.");
                Assert.Throws<System.ArgumentException>(() => restored.Get(bank, variable.Id));
                variables.Add(variable);
                CustomVariableStore.Clear(key);
                Assert.IsFalse(new CustomVariableStore(key).Get(bank, variable.Id));
            }
            finally { CustomVariableStore.Clear(key); }
        }

        [UnityTest]
        public IEnumerator RunsInOrderRepeatsAndKeepsStatePerTrigger() => UniTask.ToCoroutine(async () =>
        {
            const string path = "Assets/_Project/Scenes/Bootstrap.unity";
            await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(path);
            GameSession session = null;
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    if (root.TryGetComponent<AppLifetimeScope>(out var app)) session = app.Container.Resolve<GameSession>();
                Assert.IsNotNull(session);
                await UniTask.WaitUntil(() => session.IsRunning);
                var texts = session.LocationScope.Container.Resolve<StoryTextPresenter>();
                var memories = session.LocationScope.Container.Resolve<MemoryPresenter>();
                var variableGraph = Own(ScriptableObject.CreateInstance<ReactionGraph>());
                var variableStart = Own(variableGraph.AddNode<ReactionStartNode>());
                Set(variableStart, "runOnce", false);
                var assignment = Own(variableGraph.AddNode<ReactionNode>());
                var variableEnd = Own(variableGraph.AddNode<ReactionEndNode>());
                Connect(variableStart, assignment); Connect(assignment, variableEnd);
                var activity = Own(variableGraph.AddNode<ReactionNode>());
                Set(activity, "kind", ReactionKind.ObjectActivity);
                Set(variableGraph, "objectKeys", new List<string> { "Target" });
                Set(activity, "objectKey", "Target");
                assignment.GetOutputPort("next").ClearConnections();
                Connect(assignment, activity); Connect(activity, variableEnd);
                var activityTarget = Own(new GameObject("Activity target"));
                var customOwner = Own(new GameObject("Custom reaction owner"));
                var custom = customOwner.AddComponent<TestCustomReaction>();
                var child = Own(new GameObject("Child reaction"));
                child.transform.SetParent(customOwner.transform);
                var childReaction = child.AddComponent<TestCustomReaction>();
                Assert.IsTrue(CustomReaction.TryFind(customOwner.transform, out var found));
                Assert.AreSame(custom, found, "Only the invoking object's components are eligible.");
                var customNode = Own(variableGraph.AddNode<ReactionNode>());
                Set(customNode, "kind", ReactionKind.CustomReaction);
                activity.GetOutputPort("next").ClearConnections();
                Connect(activity, customNode); Connect(customNode, variableEnd);
                activityTarget.SetActive(false);
                var bindings = new[] { new ReactionObjectBinding("Target", activityTarget) };
                Assert.IsTrue(ReactionObjectBinding.TryResolve(bindings, "Target", out var resolved));
                Assert.AreSame(activityTarget, resolved);
                Assert.IsFalse(ReactionObjectBinding.TryResolve(new[] { bindings[0], bindings[0] }, "Target", out _));
                var bank = Own(ScriptableObject.CreateInstance<CustomVariableBank>());
                Set(bank, "bankId", "assignment-test");
                var variable = new CustomVariableBank.Variable("Test flag");
                Set(bank, "variables", new List<CustomVariableBank.Variable> { variable });
                Set(assignment, "kind", ReactionKind.SetCustomVariable);
                Set(assignment, "variableBank", bank);
                Set(assignment, "variableId", variable.Id);
                string saveKey = "Memories.Tests.Assignment." + System.Guid.NewGuid().ToString("N");
                try
                {
                    var store = new CustomVariableStore(saveKey);
                    using var runner = new ReactionRunner(texts, memories, customOwner.transform, variables: store, objectBindings: bindings,
                        customReactions: new[] { new CustomReactionBinding("", custom) });
                    Assert.IsTrue(runner.TryRun(variableGraph));
                    Assert.IsTrue(new CustomVariableStore(saveKey).Get(bank, variable.Id));
                    Assert.IsTrue(activityTarget.activeSelf, "Inactive objects must be enabled through their binding.");
                    Assert.AreEqual(1, custom.Calls);
                    Assert.AreEqual(0, childReaction.Calls);
                    Set(activity, "active", false);
                    Set(assignment, "variableValue", false);
                    Assert.IsTrue(runner.TryRun(variableGraph));
                    Assert.IsFalse(new CustomVariableStore(saveKey).Get(bank, variable.Id));
                    Assert.IsFalse(activityTarget.activeSelf);
                    Assert.AreEqual(2, custom.Calls);
                    custom.enabled = false;
                    Assert.IsFalse(CustomReaction.TryFind(customOwner.transform, out _));
                    custom.enabled = true;
                    var duplicate = customOwner.AddComponent<TestCustomReaction>();
                    Assert.IsFalse(CustomReaction.TryFind(customOwner.transform, out _));
                    Assert.IsTrue(runner.TryRun(variableGraph), "An explicitly assigned component must work even with another reaction on the same object.");
                    Assert.AreEqual(3, custom.Calls);
                    Assert.AreEqual(0, duplicate.Calls);
                    Object.DestroyImmediate(duplicate);
                    Set(assignment, "variableId", "missing");
                    Assert.IsFalse(variableGraph.TryGetSequence(out _, out _));
                }
                finally { CustomVariableStore.Clear(saveKey); }
                var trigger = CreateTrigger(texts, memories);
                Assert.IsTrue(trigger.TryActivate());
                Assert.IsFalse(trigger.TryActivate(), "No overlapping activation.");
                Assert.IsTrue(texts.IsPlaying);
                Assert.IsFalse(memories.IsPlaying, "Memory must wait for text completion.");
                await WaitUntil(() => memories.IsPlaying);
                Assert.IsFalse(texts.IsPlaying);
                await WaitUntil(() => !memories.IsPlaying);
                await WaitUntil(() => texts.IsPlaying);
                await WaitUntil(() => !trigger.IsRunning);
                Assert.IsFalse(trigger.TryActivate(), "One-shot state is stored on the scene trigger.");
                var other = CreateTrigger(texts, memories);
                Assert.IsTrue(other.TryActivate(), "Sharing an asset must not share fired state.");
                await WaitUntil(() => !other.IsRunning);
                Set(start, "runOnce", false);
                Assert.IsTrue(trigger.TryActivate(), "Repeat also replays already collected memories.");
                await WaitUntil(() => memories.IsPlaying);
                trigger.gameObject.SetActive(false);
                await WaitUntil(() => !trigger.IsRunning);
                Assert.IsFalse(memories.IsPlaying);
                Assert.AreEqual(1, Time.timeScale);
                Set(start, "runOnce", true);
                var objectRoot = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
                objectRoot.transform.position = Vector3.one * 600;
                var interactable = objectRoot.AddComponent<ReactionInteractable>();
                Set(interactable, "graph", graph);
                Set(interactable, "prompt", "Прочитать");
                interactable.Initialize(texts, memories);
                var cameraRoot = Own(new GameObject("Interaction test camera"));
                var camera = cameraRoot.AddComponent<Camera>();
                camera.transform.position = objectRoot.transform.position - Vector3.forward * 2;
                Physics.SyncTransforms();
                var probe = new InteractionProbe(camera);
                var input = new TestInput();
                var view = new TestView();
                var interaction = new InteractionPresenter(input, probe, view);
                interaction.Tick();
                Assert.AreSame(interactable, probe.FindTarget());
                Assert.AreEqual("[E] Прочитать", view.Prompt);
                input.Pressed = true;
                interaction.Tick();
                input.Pressed = false;
                Assert.IsTrue(interactable.IsRunning);
                Assert.IsFalse(interactable.CanInteract);
                Assert.IsTrue(objectRoot.GetComponent<Renderer>().enabled, "Interactive objects stay visible.");
                await WaitUntil(() => !interactable.IsRunning);
                interaction.Tick();
                Assert.IsFalse(view.Available, "One-shot interaction no longer offers a prompt.");
                Set(start, "runOnce", false);
                interaction.Tick();
                Assert.IsTrue(view.Available);
                input.Pressed = true;
                interaction.Tick();
                Assert.IsTrue(interactable.IsRunning);
                objectRoot.SetActive(false);
                await WaitUntil(() => !interactable.IsRunning);
                Assert.IsFalse(texts.IsPlaying);
            }
            finally
            {
                if (session != null && session.IsRunning) await session.EndAsync();
                if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
            }
        });

        private sealed class TestInput : IInteractionInput
        {
            public bool Pressed;
            public bool InteractionEnabled => true;
            public bool InteractPressed => Pressed;
        }
        private sealed class TestView : IInteractionView
        {
            public string Prompt;
            public bool Available;
            public void Show(bool visible, bool available, string prompt) { Prompt = prompt; Available = available; }
        }

        private SceneTrigger CreateTrigger(StoryTextPresenter texts, MemoryPresenter memories)
        {
            var root = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            root.transform.position = Vector3.one * 500;
            root.GetComponent<Collider>().isTrigger = true;
            var trigger = root.AddComponent<SceneTrigger>();
            Set(trigger, "graph", graph);
            trigger.Initialize(texts, memories);
            return trigger;
        }

        private static async UniTask WaitUntil(System.Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (!condition() && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
            Assert.IsTrue(condition(), "Timed out waiting for trigger playback.");
        }

        [TearDown]
        public void DestroyOwned()
        {
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        private ReactionGraph HintTestGraph(out ReactionNode action)
        {
            var result = Own(ScriptableObject.CreateInstance<ReactionGraph>());
            var entry = Own(result.AddNode<ReactionStartNode>());
            Set(entry, "runOnce", false);
            action = Own(result.AddNode<ReactionNode>());
            var exit = Own(result.AddNode<ReactionEndNode>());
            Connect(entry, action); Connect(action, exit);
            return result;
        }

        [UnityTest]
        public IEnumerator PickupCanShowStoryTextAfterHidingItself() => UniTask.ToCoroutine(async () =>
        {
            var pickupGraph = HintTestGraph(out var add);
            var item = Own(ScriptableObject.CreateInstance<QuestItem>());
            Set(item, "id", "pickup-with-story");
            Set(add, "kind", ReactionKind.AddInventoryItem); Set(add, "item", item);
            Set(pickupGraph, "objectKeys", new List<string> { "Pickup" });
            var hide = Own(pickupGraph.AddNode<ReactionNode>());
            Set(hide, "kind", ReactionKind.ObjectActivity); Set(hide, "objectKey", "Pickup"); Set(hide, "active", false);
            var say = Own(pickupGraph.AddNode<ReactionNode>());
            Set(say, "textGraph", textGraph);
            var exit = pickupGraph.nodes.Find(node => node is ReactionEndNode);
            add.GetOutputPort("next").ClearConnections();
            Connect(add, hide); Connect(hide, say); Connect(say, exit);
            var host = Own(new GameObject("Pickup with story"));
            var interactable = host.AddComponent<ReactionInteractable>();
            interactable.Configure(pickupGraph, "Pick up");
            Set(interactable, "objectBindings", new[] { new ReactionObjectBinding("Pickup", host) });
            var viewHost = Own(new GameObject("Pickup story view", typeof(RectTransform)));
            using var texts = new StoryTextPresenter(viewHost.AddComponent<StoryTextView>());
            var inventory = new QuestInventory();
            interactable.Initialize(texts, null, inventory: inventory);
            interactable.Interact();
            Assert.IsTrue(inventory.Contains(item.Id));
            Assert.IsFalse(host.activeSelf);
            Assert.IsTrue(texts.IsPlaying, "Hiding the pickup must not cancel the following text.");
            await WaitUntil(() => !interactable.IsRunning);
            Assert.IsFalse(texts.IsPlaying);
        });

        [UnityTest]
        public IEnumerator HintTimeoutPausesAndStopCancelsPendingHint() => UniTask.ToCoroutine(async () =>
        {
            var item = Own(ScriptableObject.CreateInstance<QuestItem>());
            Set(item, "id", "hint-test-item");
            var hint = HintTestGraph(out var reward);
            Set(reward, "kind", ReactionKind.AddInventoryItem); Set(reward, "item", item);
            var launch = HintTestGraph(out var timer);
            Set(timer, "kind", ReactionKind.StartHints); Set(timer, "hintId", "test");
            Set(timer, "hintGraph", hint); Set(timer, "hintTimeout", .1f); Set(timer, "repeatHint", false);
            var inventory = new QuestInventory();
            using var runner = new ReactionRunner(null, null, launch, inventory: inventory);
            float originalScale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                Assert.IsTrue(runner.TryRun(launch));
                await UniTask.Delay(200, ignoreTimeScale: true);
                Assert.IsFalse(inventory.Contains(item.Id), "Paused time must not consume the timeout.");
                Time.timeScale = 1;
                await WaitUntil(() => inventory.Contains(item.Id));
                var other = Own(ScriptableObject.CreateInstance<QuestItem>());
                Set(other, "id", "cancelled-hint-item"); Set(reward, "item", other);
                Assert.IsTrue(runner.TryRun(launch));
                runner.Stop();
                await UniTask.Delay(200);
                Assert.IsFalse(inventory.Contains(other.Id), "Disabling the owner cancels pending hints.");
            }
            finally { Time.timeScale = originalScale; }
        });

        [UnityTest]
        public IEnumerator HintChecksInventoryAtTimeoutAndCanStopItself() => UniTask.ToCoroutine(async () =>
        {
            var item = Own(ScriptableObject.CreateInstance<QuestItem>());
            Set(item, "id", "remote-before-timeout");
            var hint = HintTestGraph(out var reward);
            Set(reward, "kind", ReactionKind.CustomReaction);
            var condition = Own(hint.AddNode<ReactionConditionNode>());
            Set(condition, "item", item);
            var stop = Own(hint.AddNode<ReactionNode>());
            Set(stop, "kind", ReactionKind.StopHints); Set(stop, "hintId", "remote");
            var entry = hint.nodes.Find(node => node is ReactionStartNode);
            var exit = hint.nodes.Find(node => node is ReactionEndNode);
            entry.GetOutputPort("next").ClearConnections(); Connect(entry, condition);
            condition.GetOutputPort("ifPresent").Connect(stop.GetInputPort("previous"));
            condition.GetOutputPort("ifAbsent").Connect(reward.GetInputPort("previous"));
            Connect(stop, exit);
            var launch = HintTestGraph(out var timer);
            Set(timer, "kind", ReactionKind.StartHints); Set(timer, "hintId", "remote");
            Set(timer, "hintGraph", hint); Set(timer, "hintTimeout", .05f);
            var host = Own(new GameObject("Hint test"));
            var counter = host.AddComponent<TestCustomReaction>();
            var inventory = new QuestInventory();
            using var runner = new ReactionRunner(null, null, host, inventory: inventory,
                customReactions: new[] { new CustomReactionBinding("", counter) });
            Assert.IsTrue(runner.TryRun(launch));
            inventory.TryAdd(item);
            await UniTask.Delay(250);
            Assert.AreEqual(0, counter.Calls, "An item collected before the timeout must suppress the reminder.");
        });

        [Test]
        public void HintRejectsInvalidTimeoutAndRecursiveSchedules()
        {
            var launch = HintTestGraph(out var timer);
            var hint = HintTestGraph(out var action);
            Set(action, "kind", ReactionKind.StopHints); Set(action, "hintId", "test");
            Set(timer, "kind", ReactionKind.StartHints); Set(timer, "hintId", "test");
            Set(timer, "hintGraph", hint);
            Assert.IsTrue(launch.TryGetSequence(out _, out var error), error);
            Set(timer, "hintTimeout", float.NaN);
            Assert.IsFalse(launch.TryGetSequence(out _, out _));
            Set(timer, "hintTimeout", 180f); Set(action, "kind", ReactionKind.StartHints);
            Set(action, "hintGraph", launch);
            Assert.IsFalse(launch.TryGetSequence(out _, out _), "Nested timer graphs must fail without recursive validation.");
        }
    }
}

namespace Memories.Tests.PlayMode
{
    public sealed class TestCustomReaction : Memories.Gameplay.Reactions.CustomReaction
    {
        public int Calls { get; private set; }
        public override void Execute() => Calls++;
    }
}
