using System.Collections;
using Memories.Contracts;
using Memories.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class InteractionTests
    {
        private GameObject root;
        private Camera camera;
        private TestLightSwitch target;
        private Light lamp;
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
            target = item.AddComponent<TestLightSwitch>();
            lamp = root.AddComponent<Light>();
            lamp.enabled = false;
            target.Initialize(lamp);
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
        public void PressTogglesLightAndRefreshesPromptButReleasedCursorBlocksAction()
        {
            var input = new Input();
            var view = new View();
            var presenter = new InteractionPresenter(input, probe, view);
            presenter.Tick();
            Assert.IsTrue(view.Available);
            Assert.AreEqual("[E] Включить свет", view.Prompt);
            input.InteractPressed = true;
            presenter.Tick();
            Assert.IsTrue(lamp.enabled);
            Assert.AreEqual("[E] Выключить свет", view.Prompt);
            input.InteractPressed = false;
            presenter.Tick();
            Assert.IsTrue(lamp.enabled);
            input.InteractionEnabled = false;
            input.InteractPressed = true;
            presenter.Tick();
            Assert.IsFalse(view.Visible);
            Assert.IsTrue(lamp.enabled);
            input.InteractionEnabled = true;
            presenter.Tick();
            Assert.IsFalse(lamp.enabled);
            Object.DestroyImmediate(target.gameObject);
            presenter.Tick();
            Assert.IsFalse(view.Available);
            Assert.AreEqual("", view.Prompt);
        }
    }
}
