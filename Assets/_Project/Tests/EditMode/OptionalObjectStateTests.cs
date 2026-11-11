using System.Reflection;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;

namespace Memories.Tests.EditMode
{
    public sealed class OptionalObjectStateTests
    {
        private sealed class PersistentPart : IObjectStateParticipant
        {
            public string StateKey => "required";
            public string CaptureState() => "state";
            public void RestoreState(string state) { }
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [Test]
        public void PersistenceDependsOnStateRatherThanInteraction()
        {
            var owner = new GameObject("Stateless interaction");
            var other = new GameObject("Other object");
            var graph = ScriptableObject.CreateInstance<ReactionGraph>();
            var nested = ScriptableObject.CreateInstance<ReactionGraph>();
            try
            {
                var interactable = owner.AddComponent<ReactionInteractable>();
                interactable.Configure(graph, "Inspect");
                Assert.IsFalse(ObjectStateStore.RequiresPersistence(interactable));
                var action = graph.AddNode<ReactionNode>();
                Set(action, "kind", ReactionKind.StoryText);
                Assert.IsFalse(ObjectStateStore.RequiresPersistence(interactable));

                Set(action, "kind", ReactionKind.RunGraph);
                Set(action, "reactionGraph", nested);
                var nestedAction = nested.AddNode<ReactionNode>();
                Set(nestedAction, "kind", ReactionKind.RunGraph);
                Set(nestedAction, "reactionGraph", graph);
                Assert.IsFalse(ObjectStateStore.RequiresPersistence(interactable), "Cycles must not recurse forever.");
                Set(nestedAction, "kind", ReactionKind.StartHints);
                Assert.IsTrue(ObjectStateStore.RequiresPersistence(interactable), "Nested reminders must survive loading.");

                Set(action, "kind", ReactionKind.ObjectInteractivity);
                Assert.IsTrue(ObjectStateStore.RequiresPersistence(interactable), "Changing owner interaction is state.");
                Set(action, "kind", ReactionKind.ObjectActivity);
                Set(action, "objectKey", "target");
                Set(interactable, "objectBindings", new[] { new ReactionObjectBinding("target", other) });
                Assert.IsFalse(ObjectStateStore.RequiresPersistence(interactable), "Another object's state is not owned here.");
                Set(interactable, "objectBindings", new[] { new ReactionObjectBinding("target", owner) });
                Assert.IsTrue(ObjectStateStore.RequiresPersistence(interactable));
                Assert.IsTrue(ObjectStateStore.RequiresPersistence(new PersistentPart()), "Required custom state stays protected.");
            }
            finally
            {
                foreach (var node in graph.nodes) Object.DestroyImmediate(node);
                foreach (var node in nested.nodes) Object.DestroyImmediate(node);
                Object.DestroyImmediate(graph);
                Object.DestroyImmediate(nested);
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(other);
            }
        }
    }
}
