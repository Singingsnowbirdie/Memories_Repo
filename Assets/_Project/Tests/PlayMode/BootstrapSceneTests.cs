using System.Collections;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using Memories.Gameplay.Player;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.UI;
using Memories.Gameplay.Memories;

namespace Memories.Tests.PlayMode
{
    public sealed class BootstrapSceneTests
    {
        [UnityTest]
        public IEnumerator MainBootstrapStartsDarknessWithSafeWalkingArea() => UniTask.ToCoroutine(async () =>
        {
            const string path = "Assets/_Project/Scenes/Bootstrap.unity";
            await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(path);
            GameSession session = null;
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    if (root.TryGetComponent<AppLifetimeScope>(out var app))
                        session = app.Container.Resolve<GameSession>();
                Assert.IsNotNull(session);
                float deadline = Time.realtimeSinceStartup + 20;
                while (!session.IsRunning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(session.IsRunning);
                Assert.AreEqual("Assets/_Project/Locations/Darkness/Darkness.unity", session.LocationScope.gameObject.scene.path);
                Assert.IsTrue(SceneManager.GetSceneByPath("Assets/_Project/Locations/House/House.unity").isLoaded,
                    "Bootstrap must load the house alongside the highway.");
                var motor = session.LocationScope.Container.Resolve<PlayerMotor>();
                var camera = motor.GetComponentInChildren<Camera>();
                Assert.AreEqual(CameraClearFlags.SolidColor, camera.clearFlags);
                Assert.AreEqual(Color.black, camera.backgroundColor);
                Assert.IsNull(RenderSettings.skybox);
                Assert.AreEqual(0, RenderSettings.ambientIntensity);
                var controller = motor.GetComponent<CharacterController>();
                foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                    new Vector3(1, 0, 1).normalized, new Vector3(-1, 0, 1).normalized,
                    new Vector3(1, 0, -1).normalized, new Vector3(-1, 0, -1).normalized })
                {
                    controller.enabled = false;
                    motor.transform.position = new Vector3(0, 0.05f, 100.5f);
                    controller.enabled = true;
                    Physics.SyncTransforms();
                    for (int step = 0; step < 4000; step++)
                        motor.Move(direction * 0.08f + Vector3.down * 0.04f);
                    var position = motor.transform.position;
                    float sideLimit = position.z < 300.9f ? 4.5f : (position.x < 0 ? 6 * Mathf.Sqrt(2) : 6 * Mathf.Sqrt(2) + 8);
                    Assert.Less(Mathf.Abs(position.x), sideLimit,
                        "Player must stay inside the highway or house approach boundaries.");
                    Assert.That(position.z, Is.InRange(100f, 308 + 14 * Mathf.Sqrt(2)), "North/south barrier failed.");
                    if (direction == Vector3.forward)
                        Assert.Greater(position.z, 299, "The highway must be walkable to the far end.");
                    Assert.That(position.y, Is.InRange(-0.1f, 0.15f), "Player left the floor.");
                }

                controller.enabled = false;
                motor.transform.position = new Vector3(0, 0.05f, 299);
                controller.enabled = true;
                Physics.SyncTransforms();
                // Walk continuously from the road through every room, upstairs and back out.
                WalkTo(0, 311, 0); WalkTo(-3.6f, 311, 0); WalkTo(0, 311, 0);
                WalkTo(3.6f, 311, 0); WalkTo(0, 311, 0);
                WalkTo(0, 318, 0); WalkTo(-3.6f, 318, 0); WalkTo(0, 318, 0);
                WalkTo(0, 314, 0); WalkTo(3.8f, 314, 0);
                WalkTo(3.8f, 321.2f, 3.2f); WalkTo(0, 321.2f, 3.2f);
                WalkTo(0, 318, 3.2f); WalkTo(-3.6f, 318, 3.2f); WalkTo(0, 318, 3.2f);
                WalkTo(0, 311, 3.2f); WalkTo(-3.6f, 311, 3.2f); WalkTo(0, 311, 3.2f);
                WalkTo(3.6f, 311, 3.2f); WalkTo(0, 311, 3.2f);
                WalkTo(0, 321.2f, 3.2f); WalkTo(3.8f, 321.2f, 3.2f);
                WalkTo(3.8f, 314, 0); WalkTo(0, 314, 0);
                WalkTo(0, 311, 0); WalkTo(3.6f, 311, 0);
                float garageCenterX = 6 * Mathf.Sqrt(2) + 4;
                WalkWorldTo(garageCenterX, 308 + 3 * Mathf.Sqrt(2), 0); // Door from house to garage.
                WalkWorldTo(garageCenterX, 318, 0);
                WalkWorldTo(garageCenterX, 305, 0); // Open vehicle entrance.
                WalkWorldTo(0, 305, 0); // Continuous terrace back to the highway.
                WalkWorldTo(0, 299, 0);

                void WalkTo(float x, float z, float floorY)
                {
                    float scale = Mathf.Sqrt(2);
                    WalkWorldTo(x * scale, z >= 308 ? 308 + (z - 308) * scale : z, floorY);
                }

                void WalkWorldTo(float x, float z, float floorY)
                {
                    for (int step = 0; step < 600; step++)
                    {
                        var delta = new Vector3(x - motor.transform.position.x, 0, z - motor.transform.position.z);
                        if (delta.magnitude < 0.04f) break;
                        motor.Move(Vector3.ClampMagnitude(delta, 0.05f) + Vector3.down * 0.04f);
                    }
                    for (int step = 0; step < 20; step++) motor.Move(Vector3.down * 0.04f);
                    var p = motor.transform.position;
                    Assert.Less(Vector2.Distance(new Vector2(p.x, p.z), new Vector2(x, z)), 0.15f,
                        $"Blocked route to ({x}, {z}); stopped at {p}.");
                    Assert.That(p.y, Is.InRange(floorY - 0.1f, floorY + 0.15f), "Wrong floor or missing support.");
                }
            }
            finally
            {
                if (session != null && session.IsRunning) await session.EndAsync();
                if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
                Assert.IsFalse(SceneManager.GetSceneByPath("Assets/_Project/Locations/House/House.unity").isLoaded,
                    "The session must unload its additional house scene.");
            }
        });

        [UnityTest]
        public IEnumerator BootstrapSceneStartsLocationAutomatically() => UniTask.ToCoroutine(async () =>
        {
            const string path = "Assets/_Project/Scenes/TestBootstrap.unity";
            await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(path);
            AppLifetimeScope app = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<AppLifetimeScope>(out var candidate)) app = candidate;
            try
            {
                Assert.IsNotNull(app);
                var session = app.Container.Resolve<GameSession>();
                float deadline = Time.realtimeSinceStartup + 20;
                while (!session.IsRunning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(session.IsRunning, "Bootstrap did not start the initial location.");
                Assert.AreEqual("Assets/Scenes/FirstPersonTest/FirstPersonTest.unity", session.LocationScope.gameObject.scene.path);
                var motor = session.LocationScope.Container.Resolve<PlayerMotor>();
                Assert.IsNotNull(motor.GetComponent<CharacterController>());
                Assert.AreEqual(1, motor.GetComponentsInChildren<Camera>().Length);
                Assert.AreEqual(1, motor.GetComponentsInChildren<AudioListener>().Length);
                Physics.SyncTransforms();
                var probe = session.LocationScope.Container.Resolve<InteractionProbe>();
                var target = probe.FindTarget();
                Assert.IsInstanceOf<TestLightSwitch>(target, "The test switch must be reachable from spawn.");
                Assert.AreEqual("Включить свет", target.Prompt);
                target.Interact();
                Assert.AreEqual("Выключить свет", target.Prompt);
                var hud = session.LocationScope.Container.Resolve<IInteractionView>() as InteractionHud;
                Assert.IsNotNull(hud);
                hud.Show(true, true, "[E] " + target.Prompt);
                Assert.IsTrue(hud.GetComponent<Canvas>().enabled);
                var application = app.Container.Resolve<ApplicationContext>();
                var sessionContext = session.SessionScope.Container.Resolve<SessionContext>();
                var location = session.LocationScope.Container.Resolve<LocationContext>();
                ReactionInteractable shard = null;
                foreach (var root in motor.gameObject.scene.GetRootGameObjects())
                    foreach (var candidate in root.GetComponentsInChildren<ReactionInteractable>()) shard = candidate;
                Assert.IsNotNull(shard);
                Assert.IsTrue(shard.CanInteract);
                shard.Interact();
                deadline = Time.realtimeSinceStartup + 5;
                while (session.LocationScope.Container.Resolve<GameplayModeState>().Mode != GameplayMode.Reading &&
                    Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.AreEqual(GameplayMode.Reading, session.LocationScope.Container.Resolve<GameplayModeState>().Mode);
                Assert.AreEqual(0, Time.timeScale);
                var memoryPresenter = session.LocationScope.Container.Resolve<MemoryPresenter>();
                float playbackStart = Time.realtimeSinceStartup;
                while (memoryPresenter.IsPlaying && Time.realtimeSinceStartup - playbackStart < 20)
                    await UniTask.NextFrame();
                Assert.IsFalse(memoryPresenter.IsPlaying, "Graph should complete automatically while timeScale is zero.");
                Assert.GreaterOrEqual(Time.realtimeSinceStartup - playbackStart, 13.4f);
                await UniTask.NextFrame();
                Assert.IsTrue(shard.gameObject.activeSelf);
                Assert.IsFalse(shard.IsRunning);
                Assert.IsFalse(shard.CanInteract);
                Assert.IsTrue(session.SessionScope.Container.Resolve<MemoryCollection>().Contains("test-memory-01"));
                var pause = session.LocationScope.Container.Resolve<PausePresenter>();
                var pauseView = session.LocationScope.Container.Resolve<IPauseView>() as PauseMenuView;
                Assert.IsNotNull(pauseView);
                pause.Toggle();
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsTrue(AudioListener.pause);
                Assert.IsTrue(pauseView.transform.GetChild(0).gameObject.activeSelf);
                Assert.IsFalse(app.Container.Resolve<Memories.Contracts.IInteractionInput>().InteractionEnabled);
                await session.EndAsync();
                Assert.AreEqual(1, Time.timeScale);
                Assert.IsFalse(AudioListener.pause);
                Assert.IsTrue(pauseView == null);
                Assert.IsTrue(hud == null, "HUD must be destroyed with its location.");
                Assert.IsTrue(target == null, "Test switch must be destroyed with its location.");
                await SceneManager.UnloadSceneAsync(scene);
                Assert.IsTrue(application.Token.IsCancellationRequested);
                Assert.IsTrue(sessionContext.Token.IsCancellationRequested);
                Assert.IsTrue(location.Token.IsCancellationRequested);
            }
            finally
            {
                if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
            }
        });
    }
}
