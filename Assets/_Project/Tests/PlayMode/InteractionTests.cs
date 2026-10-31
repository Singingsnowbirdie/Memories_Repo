using System.Collections;
using Memories.Contracts;
using Memories.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class InteractionTargetStub : Interactable
    {
        public bool Activated { get; private set; }
        public override string Prompt => Activated ? "Деактивировать" : "Активировать";
        public override void Interact() { if (CanInteract) Activated = !Activated; }
    }
    public sealed class InteractionTests
    {
        private GameObject root;
        private Camera camera;
        private InteractionTargetStub target;
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
            target = item.AddComponent<InteractionTargetStub>();
            probe = new InteractionProbe(camera);
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
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
            Assert.IsTrue(target.Activated);
            Assert.AreEqual("[E] Деактивировать", view.Prompt);
            input.InteractPressed = false;
            presenter.Tick();
            Assert.IsTrue(target.Activated);
            input.InteractionEnabled = false;
            input.InteractPressed = true;
            presenter.Tick();
            Assert.IsFalse(view.Visible);
            Assert.IsTrue(target.Activated);
            input.InteractionEnabled = true;
            presenter.Tick();
            Assert.IsFalse(target.Activated);
            Object.DestroyImmediate(target.gameObject);
            presenter.Tick();
            Assert.IsFalse(view.Available);
            Assert.AreEqual("", view.Prompt);
        }
    }
}
