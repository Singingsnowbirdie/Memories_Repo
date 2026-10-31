using System;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;

namespace Memories.Tests.PlayMode
{
    public sealed class ReactionRandomNodeTests
    {
        [Test]
        public void RunnerExecutesOnlySelectedBranchAndGraphRejectsBrokenEdges()
        {
            var graph = ScriptableObject.CreateInstance<ReactionGraph>();
            var firstTarget = new GameObject("First");
            var secondTarget = new GameObject("Second");
            void Set(object target, string field, object value) => target.GetType()
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
            try
            {
                Set(graph, "objectKeys", new System.Collections.Generic.List<string> { "first", "second" });
                var start = graph.AddNode<ReactionStartNode>();
                Set(start, "runOnce", false);
                var random = graph.AddNode<ReactionRandomNode>();
                var first = graph.AddNode<ReactionNode>(); var second = graph.AddNode<ReactionNode>();
                var end = graph.AddNode<ReactionEndNode>();
                Set(first, "kind", ReactionKind.ObjectActivity); Set(first, "objectKey", "first");
                Set(second, "kind", ReactionKind.ObjectActivity); Set(second, "objectKey", "second");
                start.GetOutputPort("next").Connect(random.GetInputPort("previous"));
                string zero = random.AddChoice(0), selected = random.AddChoice(1);
                random.GetOutputPort(zero).Connect(first.GetInputPort("previous"));
                random.GetOutputPort(selected).Connect(second.GetInputPort("previous"));
                first.GetOutputPort("next").Connect(end.GetInputPort("previous"));
                second.GetOutputPort("next").Connect(end.GetInputPort("previous"));
                Assert.IsTrue(graph.TryGetSequence(out var actions, out var error), error);
                Assert.AreEqual(2, actions.Count);
                using (var runner = new ReactionRunner(null, null, null, objectBindings: new[] {
                    new ReactionObjectBinding("first", firstTarget), new ReactionObjectBinding("second", secondTarget) }))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        firstTarget.SetActive(false); secondTarget.SetActive(false);
                        Assert.IsTrue(runner.TryRun(graph));
                        Assert.IsFalse(firstTarget.activeSelf);
                        Assert.IsTrue(secondTarget.activeSelf);
                    }
                }
                random.GetOutputPort(zero).ClearConnections();
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
                random.GetOutputPort(zero).Connect(first.GetInputPort("previous"));
                second.GetOutputPort("next").Connect(random.GetInputPort("previous"));
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
            }
            finally
            {
                foreach (var node in graph.nodes) UnityEngine.Object.DestroyImmediate(node);
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(firstTarget); UnityEngine.Object.DestroyImmediate(secondTarget);
            }
        }

        [Test]
        public void WeightsDefineIntervalsAndZeroNeverWins()
        {
            var node = ScriptableObject.CreateInstance<ReactionRandomNode>();
            try
            {
                node.AddChoice(0);
                string first = node.AddChoice(1), second = node.AddChoice(3);
                node.AddChoice(0);
                Assert.AreEqual(first, node.SelectOutput(0));
                Assert.AreEqual(first, node.SelectOutput(.249999));
                Assert.AreEqual(second, node.SelectOutput(.25));
                Assert.AreEqual(second, node.SelectOutput(1));
                int firstCount = 0;
                for (int i = 0; i < 1000; i++) if (node.SelectOutput(i / 1000d) == first) firstCount++;
                Assert.AreEqual(250, firstCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(node); }
        }

        [Test]
        public void InvalidWeightsAreRejected()
        {
            foreach (float weight in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                var node = ScriptableObject.CreateInstance<ReactionRandomNode>();
                try { node.AddChoice(weight); Assert.IsFalse(node.Validate(out _)); }
                finally { UnityEngine.Object.DestroyImmediate(node); }
            }
        }

        [Test]
        public void LargeWeightsDoNotOverflow()
        {
            var node = ScriptableObject.CreateInstance<ReactionRandomNode>();
            try
            {
                string first = node.AddChoice(float.MaxValue), second = node.AddChoice(float.MaxValue);
                Assert.AreEqual(first, node.SelectOutput(.49));
                Assert.AreEqual(second, node.SelectOutput(.5));
            }
            finally { UnityEngine.Object.DestroyImmediate(node); }
        }

        [Test]
        public void RemovingAnOutputPreservesOtherConnections()
        {
            var graph = ScriptableObject.CreateInstance<ReactionGraph>();
            try
            {
                var random = graph.AddNode<ReactionRandomNode>();
                var end = graph.AddNode<ReactionEndNode>();
                random.AddChoice();
                string retained = random.AddChoice();
                random.GetOutputPort(retained).Connect(end.GetInputPort("previous"));
                random.RemoveChoice(0);
                Assert.AreSame(end, ReactionGraph.Next(random, retained));
                Assert.AreNotEqual(retained, random.AddChoice());
            }
            finally { foreach (var node in graph.nodes) UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(graph); }
        }
    }
}
