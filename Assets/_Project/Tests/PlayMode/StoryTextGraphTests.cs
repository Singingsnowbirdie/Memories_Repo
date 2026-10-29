using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Story;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XNode;

namespace Memories.Tests.PlayMode
{
    public sealed class StoryTextGraphTests
    {
        private StoryTextGraph graph;
        private StoryTextStartNode start;
        private StoryTextNode first, second;
        private StoryTextEndNode end;

        [SetUp]
        public void Create()
        {
            graph = ScriptableObject.CreateInstance<StoryTextGraph>();
            start = graph.AddNode<StoryTextStartNode>();
            second = graph.AddNode<StoryTextNode>();
            first = graph.AddNode<StoryTextNode>();
            end = graph.AddNode<StoryTextEndNode>();
            JsonUtility.FromJsonOverwrite("{\"text\":\"Первый\",\"duration\":0.1}", first);
            JsonUtility.FromJsonOverwrite("{\"text\":\"Второй\",\"duration\":0.1}", second);
            Connect(start, first); Connect(first, second); Connect(second, end);
        }

        private static void Connect(Node a, Node b) => a.GetOutputPort("next").Connect(b.GetInputPort("previous"));

        [TearDown]
        public void Destroy()
        {
            foreach (var node in graph.nodes.ToArray()) UnityEngine.Object.DestroyImmediate(node);
            UnityEngine.Object.DestroyImmediate(graph);
        }

        [Test]
        public void SequenceFollowsConnectionsAndCopiesContent()
        {
            Assert.IsTrue(graph.TryGetSequence(out var fragments, out var error), error);
            Assert.AreEqual("Первый", fragments[0].Text);
            Assert.AreEqual("Второй", fragments[1].Text);
            JsonUtility.FromJsonOverwrite("{\"text\":\"Изменено\"}", first);
            Assert.AreEqual("Первый", fragments[0].Text);
        }

        [Test]
        public void RejectsBrokenConnectionsCyclesAndDisconnectedNodes()
        {
            first.GetOutputPort("next").ClearConnections();
            Assert.IsFalse(graph.TryGetSequence(out _, out _));
            Connect(first, second);
            Connect(second, first);
            Assert.IsFalse(graph.TryGetSequence(out _, out _));
            Connect(start, first); Connect(second, end);
            var orphan = graph.AddNode<StoryTextNode>();
            Assert.IsFalse(graph.TryGetSequence(out _, out _));
            graph.RemoveNode(orphan); UnityEngine.Object.DestroyImmediate(orphan);
            Assert.IsTrue(graph.TryGetSequence(out _, out var error), error);
        }

        [Test]
        public void RejectsEmptyTextInvalidDurationAndForeignNodes()
        {
            JsonUtility.FromJsonOverwrite("{\"text\":\" \"}", first);
            Assert.IsFalse(graph.TryGetSequence(out _, out _));
            JsonUtility.FromJsonOverwrite("{\"text\":\"Текст\"}", first);
            var duration = typeof(StoryTextNode).GetField("duration", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                duration.SetValue(first, invalid);
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
            }
            duration.SetValue(first, 1f);
            var foreign = ScriptableObject.CreateInstance<StoryTextGraph>();
            try
            {
                var node = foreign.AddNode<StoryTextNode>();
                Connect(first, node);
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
                UnityEngine.Object.DestroyImmediate(node);
            }
            finally { UnityEngine.Object.DestroyImmediate(foreign); }
        }

        [UnityTest]
        public IEnumerator GraphPlaysOnPanelAndInvalidGraphDoesNotInterruptIt() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Text graph test", typeof(RectTransform));
            var view = root.AddComponent<StoryTextView>();
            using var presenter = new StoryTextPresenter(view);
            try
            {
                var text = root.GetComponentInChildren<Text>();
                var playback = presenter.ShowAsync(graph);
                Assert.AreEqual("Первый", text.text);
                JsonUtility.FromJsonOverwrite("{\"text\":\"\"}", first);
                Assert.Throws<ArgumentException>(() => presenter.ShowAsync(graph));
                Assert.IsTrue(presenter.IsPlaying);
                await UniTask.WaitUntil(() => text.text == "Второй");
                Assert.IsTrue(root.GetComponent<Canvas>().enabled);
                await playback;
                Assert.IsFalse(presenter.IsPlaying);
                Assert.IsTrue(root.GetComponent<Canvas>().enabled);
                Assert.IsEmpty(root.GetComponentInChildren<Text>().text);
            }
            finally { presenter.Dispose(); UnityEngine.Object.Destroy(root); }
        });
    }
}
