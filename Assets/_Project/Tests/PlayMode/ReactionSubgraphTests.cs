using System.Collections.Generic;
using System.Reflection;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;
using XNode;

namespace Memories.Tests.PlayMode
{
    public sealed class StopSubgraphReaction : CustomReaction
    {
        public System.Action OnExecute;
        public override void Execute() => OnExecute();
    }
    public sealed class ReactionSubgraphTests
    {
        readonly List<Object> owned = new();
        T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }
        static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        static void Connect(Node a, Node b) => a.GetOutputPort("next").Connect(b.GetInputPort("previous"));
        ReactionGraph Graph(out ReactionNode action)
        {
            var graph = Own(ScriptableObject.CreateInstance<ReactionGraph>());
            var start = Own(graph.AddNode<ReactionStartNode>()); Set(start, "runOnce", false);
            action = Own(graph.AddNode<ReactionNode>());
            var end = Own(graph.AddNode<ReactionEndNode>());
            Connect(start, action); Connect(action, end); return graph;
        }
        [TearDown] public void Cleanup() { foreach (var obj in owned) if (obj != null) Object.DestroyImmediate(obj); owned.Clear(); }

        [Test] public void NestedGraphUsesCallerBindingsAndReturnsToCaller()
        {
            var child = Graph(out var hide);
            Set(child, "objectKeys", new List<string> { "item" });
            Set(hide, "kind", ReactionKind.ObjectActivity); Set(hide, "objectKey", "item"); Set(hide, "active", false);
            var parent = Graph(out var call); Set(call, "kind", ReactionKind.RunGraph); Set(call, "reactionGraph", child);
            var end = ReactionGraph.Next(call); call.GetOutputPort("next").ClearConnections();
            var show = Own(parent.AddNode<ReactionNode>()); Set(show, "kind", ReactionKind.ObjectActivity); Set(show, "objectKey", "item"); Set(show, "active", true);
            Set(parent, "objectKeys", new List<string> { "item" }); Connect(call, show); Connect(show, end);
            var obj = Own(new GameObject("Caller"));
            var interaction = obj.AddComponent<ReactionInteractable>(); interaction.Configure(parent, "test");
            Set(interaction, "objectBindings", new[] { new ReactionObjectBinding("item", obj) });
            interaction.Initialize(null, null); interaction.Interact();
            Assert.IsTrue(obj.activeSelf, "Caller must continue even when nested graph hides its owner.");
            Assert.IsFalse(interaction.IsRunning);
            Assert.IsTrue(parent.TryGetSequence(out var actions, out _)); Assert.Contains(hide, actions);
        }

        [Test] public void MutualRecursionIsRejectedAndValidationRecovers()
        {
            var a = Graph(out var callA); var b = Graph(out var callB);
            Set(callA, "kind", ReactionKind.RunGraph); Set(callA, "reactionGraph", b);
            Set(callB, "kind", ReactionKind.RunGraph); Set(callB, "reactionGraph", a);
            Assert.IsFalse(a.TryGetSequence(out _, out _));
            Set(callB, "kind", ReactionKind.ObjectInteractivity);
            Assert.IsTrue(a.TryGetSequence(out _, out var error), error);
        }

        [Test] public void MissingSubgraphIsRejected()
        {
            var graph = Graph(out var call); Set(call, "kind", ReactionKind.RunGraph);
            Assert.IsFalse(graph.TryGetSequence(out _, out _));
        }

        [Test] public void StoppingDuringSubgraphCancelsCallerContinuation()
        {
            var child = Graph(out var stop); Set(stop, "kind", ReactionKind.CustomReaction);
            var parent = Graph(out var call); Set(call, "kind", ReactionKind.RunGraph); Set(call, "reactionGraph", child);
            var end = ReactionGraph.Next(call); call.GetOutputPort("next").ClearConnections();
            var show = Own(parent.AddNode<ReactionNode>()); Set(show, "kind", ReactionKind.ObjectActivity); Set(show, "objectKey", "item");
            Set(parent, "objectKeys", new List<string> { "item" }); Connect(call, show); Connect(show, end);
            var obj = Own(new GameObject("Target")); obj.SetActive(false);
            var owner = Own(new GameObject("Owner")); var reaction = owner.AddComponent<StopSubgraphReaction>();
            using var runner = new ReactionRunner(null, null, owner, objectBindings: new[] { new ReactionObjectBinding("item", obj) }, customReactions: new[] { new CustomReactionBinding("", reaction) });
            reaction.OnExecute = runner.Stop;
            Assert.IsTrue(runner.TryRun(parent));
            Assert.IsFalse(obj.activeSelf); Assert.IsFalse(runner.IsRunning);
        }
    }
}
