using System.Collections;
using Memories.Contracts;
using Memories.Gameplay.Reactions;
using System.Reflection;
using Memories.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class ToggleInteractionTestReaction : CustomReaction
    {
        public bool Activated { get; private set; }
        public ReactionGraph Graph;
        public override void Execute()
        {
            Activated = !Activated;
            GetComponent<ReactionInteractable>().Configure(Graph, Activated ? "Деактивировать" : "Активировать");
        }
    }
    public sealed class InteractionTests
    {
        private GameObject root;
        private Camera camera;
        private ReactionInteractable target;
        private ToggleInteractionTestReaction reaction;
        private ReactionGraph graph;
        private static void Set(object owner, string field, object value) =>
            owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private InteractionProbe probe;
        private sealed class Input : IInteractionInput
        {
            public bool InteractionEnabled { get; set; } = true;
            public bool InteractPressed { get; set; }
        }
        private sealed class View : IInteractionView
        {
            public bool Visible, Available;
            public string Prompt;
            public void Show(bool visible, bool available, string prompt)
            { Visible = visible; Available = available; Prompt = prompt; }
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Interaction test");
            root.transform.position = new Vector3(1000, 1000, 1000);
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(root.transform, false);
            camera = cameraObject.AddComponent<Camera>();
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.transform.SetParent(root.transform, false);
            item.transform.localPosition = Vector3.forward * 2;
            target = item.AddComponent<ReactionInteractable>();
            reaction = item.AddComponent<ToggleInteractionTestReaction>();
            graph = ScriptableObject.CreateInstance<ReactionGraph>();
            var start = graph.AddNode<ReactionStartNode>();
            var action = graph.AddNode<ReactionNode>();
            var end = graph.AddNode<ReactionEndNode>();
            Set(graph, "objectKeys", new System.Collections.Generic.List<string> { "toggle" });
            Set(start, "runOnce", false);
            Set(action, "kind", ReactionKind.CustomReaction);
            Set(action, "objectKey", "toggle");
            start.GetOutputPort("next").Connect(action.GetInputPort("previous"));
            action.GetOutputPort("next").Connect(end.GetInputPort("previous"));
            reaction.Graph = graph;
            Set(target, "customReactions", new[] { new CustomReactionBinding("toggle", reaction) });
            target.Configure(graph, "Активировать");
            target.Initialize(null, null);
            probe = new InteractionProbe(camera);
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
            foreach (var node in graph.nodes) Object.DestroyImmediate(node);
            Object.DestroyImmediate(graph);
        }

        [Test]
        public void OwnControllerAndChildCollidersAreSkippedButWallStillBlocks()
        {
            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            var controller = player.AddComponent<CharacterController>();
            controller.center = Vector3.forward * .5f;
            controller.radius = .2f;
            camera.transform.SetParent(player.transform, false);
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(player.transform, false);
            child.transform.localPosition = Vector3.forward;
            child.transform.localScale = Vector3.one * .1f;
            probe = new InteractionProbe(camera);
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(camera.transform.position, Vector3.forward, out var hit, 3));
            Assert.AreSame(controller, hit.collider);
            Assert.AreSame(target, probe.FindTarget());

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform, false);
            wall.transform.localPosition = Vector3.forward * 1.3f;
            wall.transform.localScale = Vector3.one * .1f;
            Physics.SyncTransforms();
            Assert.IsNull(probe.FindTarget());
        }

        [Test]
        public void NearestWallBlocksInteraction()
        {
            Assert.AreSame(target, probe.FindTarget());
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform, false);
            wall.transform.localPosition = Vector3.forward;
            wall.transform.localScale = new Vector3(1, 1, .1f);
            Physics.SyncTransforms();
            Assert.IsNull(probe.FindTarget());
        }

        [Test]
        public void RangeDisabledTargetAndLookingAwayClearTarget()
        {
            target.transform.localPosition = Vector3.forward * 4;
            Physics.SyncTransforms();
            Assert.IsNull(probe.FindTarget());
            target.transform.localPosition = Vector3.forward * 2;
            Physics.SyncTransforms();
            target.enabled = false;
            Assert.IsNull(probe.FindTarget());
            target.enabled = true;
            camera.transform.rotation = Quaternion.Euler(0, 90, 0);
            Assert.IsNull(probe.FindTarget());
        }

        [Test]
        public void PressInvokesTargetAndRefreshesPromptButReleasedCursorBlocksAction()
        {
            var input = new Input();
            var view = new View();
            var presenter = new InteractionPresenter(input, probe, view);
            presenter.Tick();
            Assert.IsTrue(view.Available);
            Assert.AreEqual("[E] Активировать", view.Prompt);
            input.InteractPressed = true;
            presenter.Tick();
            Assert.IsTrue(reaction.Activated);
            Assert.AreEqual("[E] Деактивировать", view.Prompt);
            input.InteractPressed = false;
            presenter.Tick();
            Assert.IsTrue(reaction.Activated);
            input.InteractionEnabled = false;
            input.InteractPressed = true;
            presenter.Tick();
            Assert.IsFalse(view.Visible);
            Assert.IsTrue(reaction.Activated);
            input.InteractionEnabled = true;
            presenter.Tick();
            Assert.IsFalse(reaction.Activated);
            Object.DestroyImmediate(target.gameObject);
            presenter.Tick();
            Assert.IsFalse(view.Available);
            Assert.AreEqual("", view.Prompt);
        }
    }
}
