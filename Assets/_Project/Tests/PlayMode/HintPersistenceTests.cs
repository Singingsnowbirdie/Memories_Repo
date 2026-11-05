using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using XNode;
using Object = UnityEngine.Object;

namespace Memories.Tests.PlayMode
{
    public sealed class HintPersistenceCounter : CustomReaction
    {
        public int Count;
        public override void Execute() => Count++;
    }

    public sealed class HintPersistenceTests
    {
        readonly List<Object> owned = new List<Object>();
        string key;
        T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }
        static void Set(object obj, string field, object value) => obj.GetType()
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        static void Connect(Node a, Node b) => a.GetOutputPort("next").Connect(b.GetInputPort("previous"));
        ReactionGraph Graph(ReactionKind kind, out ReactionNode action)
        {
            var graph = Own(ScriptableObject.CreateInstance<ReactionGraph>());
            var start = Own(graph.AddNode<ReactionStartNode>());
            Set(start, "runOnce", false);
            action = Own(graph.AddNode<ReactionNode>());
            Set(action, "kind", kind);
            var end = Own(graph.AddNode<ReactionEndNode>());
            Connect(start, action); Connect(action, end);
            return graph;
        }
        ReactionGraph Conversation()
        {
            var hint = Graph(ReactionKind.CustomReaction, out var tick);
            Set(tick, "objectKey", "counter"); Set(hint, "objectKeys", new List<string> { "counter" });
            var graph = Graph(ReactionKind.StartHints, out var start);
            Set(start, "hintId", "test_hint"); Set(start, "hintGraph", hint);
            Set(start, "hintTimeout", 0.05f); Set(start, "repeatHint", true);
            Assert.IsTrue(graph.TryGetSequence(out _, out var error), error); return graph;
        }
        ReactionInteractable Create(ReactionGraph graph, out HintPersistenceCounter counter)
        {
            var go = Own(new GameObject("Hint owner"));
            counter = go.AddComponent<HintPersistenceCounter>();
            var interaction = go.AddComponent<ReactionInteractable>();
            interaction.Configure(graph, "test");
            Set(interaction, "customReactions", new[] { new CustomReactionBinding("counter", counter) });
            interaction.Initialize(null, null);
            return interaction;
        }
        [SetUp] public void Setup() { key = "Memories.Tests.Hints." + Guid.NewGuid().ToString("N"); }
        [TearDown] public void Cleanup()
        {
            foreach (var obj in owned) if (obj is GameObject) Object.DestroyImmediate(obj);
            foreach (var obj in owned) if (obj != null) Object.DestroyImmediate(obj);
            owned.Clear(); PlayerPrefs.DeleteKey(key);
        }
        [UnityTest] public IEnumerator ActiveHintResumesAfterReloadWithoutInteraction()
        {
            var graph = Conversation();
            var first = Create(graph, out _);
            using (var store = new ObjectStateStore(saveKey: key))
            {
                store.Register(first.gameObject, "father");
                first.Interact(); store.Flush();
            }
            Object.DestroyImmediate(first.gameObject);
            var second = Create(graph, out var counter);
            using (var store = new ObjectStateStore(saveKey: key))
            {
                store.Register(second.gameObject, "father");
                yield return new WaitForSeconds(0.2f);
                Assert.Greater(counter.Count, 0, "Saved hint must run without another interaction.");
            }
        }
        [UnityTest] public IEnumerator StoppedHintDoesNotResumeAfterReload()
        {
            var graph = Conversation();
            var first = Create(graph, out _);
            using (var store = new ObjectStateStore(saveKey: key))
            {
                store.Register(first.gameObject, "father"); first.Interact();
                var stop = Graph(ReactionKind.StopHints, out var action);
                Set(action, "hintId", "test_hint");
                first.Configure(stop, "stop"); first.Interact();
                first.Configure(graph, "test"); store.Flush();
            }
            Object.DestroyImmediate(first.gameObject);
            var second = Create(graph, out var counter);
            using (var store = new ObjectStateStore(saveKey: key))
            {
                store.Register(second.gameObject, "father");
                yield return new WaitForSeconds(0.2f);
                Assert.AreEqual(0, counter.Count);
            }
        }
        [UnityTest] public IEnumerator FreshSaveDoesNotStartHint()
        {
            var interaction = Create(Conversation(), out var counter);
            using (var store = new ObjectStateStore(saveKey: key))
            {
                store.Register(interaction.gameObject, "father");
                yield return new WaitForSeconds(0.2f);
                Assert.AreEqual(0, counter.Count);
            }
        }
    }
}
