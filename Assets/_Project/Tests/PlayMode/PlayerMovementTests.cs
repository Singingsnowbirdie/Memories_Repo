using System.Collections;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Contracts;
using Memories.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class PlayerMovementTests
    {
        private Scene scene;
        private PlayerMotor motor;
        private PlayerPresenter presenter;
        private InputStub input;

        private sealed class InputStub : IPlayerInput
        {
            public Vector2 Move { get; set; }
            public Vector2 LookDelta => Vector2.zero;
            public void Enable() { }
            public void Disable() { }
            public void Poll() { }
        }

        [UnitySetUp]
        public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
        {
            await SceneManager.LoadSceneAsync(AppLifetimeScope.DefaultLocationPath, LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath(AppLifetimeScope.DefaultLocationPath);
            var player = new GameObject("MovementTestPlayer");
            SceneManager.MoveGameObjectToScene(player, scene);
            Camera camera = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<Camera>())
                    if (candidate.CompareTag("MainCamera")) camera = candidate;
            Assert.IsNotNull(camera);
            motor = player.AddComponent<PlayerMotor>();
            var settings = new PlayerMovementSettings();
            motor.Initialize(settings, camera.transform);
            input = new InputStub();
            presenter = new PlayerPresenter(input, new PlayerMovementModel(settings), motor, settings);
            presenter.Start();
            Physics.SyncTransforms();
        });

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            presenter?.Dispose();
            if (scene.IsValid() && scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
        });

        private void Place(Vector3 position)
        {
            var controller = motor.GetComponent<CharacterController>();
            controller.enabled = false;
            motor.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private void Simulate(float seconds)
        {
            for (int i = 0; i < Mathf.RoundToInt(seconds * 60); i++) presenter.Tick(1f / 60f);
        }

        [UnityTest]
        public IEnumerator GravityLandsOnPolygonFloor()
        {
            Place(new Vector3(0, 4, -17));
            Simulate(3);
            Assert.That(motor.transform.position.y, Is.InRange(-0.05f, 0.1f));
            Assert.IsTrue(motor.IsGrounded);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharacterCannotWalkThroughBoundaryWall()
        {
            Place(new Vector3(-19, 0.05f, -17));
            input.Move = Vector2.left;
            Simulate(2);
            Assert.That(motor.transform.position.x, Is.InRange(-19.8f, -19.1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharacterClimbsPolygonStairs()
        {
            Place(new Vector3(5, 0.05f, 2));
            input.Move = Vector2.up;
            Simulate(2);
            Assert.That(motor.transform.position.z, Is.GreaterThan(8.5f));
            Assert.That(motor.transform.position.y, Is.InRange(1.9f, 2.15f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharacterClimbsPolygonRamp()
        {
            Place(new Vector3(11, 0.05f, -1));
            input.Move = Vector2.up;
            Simulate(2.5f);
            Assert.That(motor.transform.position.z, Is.GreaterThan(8.2f));
            Assert.That(motor.transform.position.y, Is.InRange(1.9f, 2.15f));
            yield return null;
        }
    }
}
