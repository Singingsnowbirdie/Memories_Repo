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
                    motor.transform.position = new Vector3(0, 0.05f, 0);
                    controller.enabled = true;
                    Physics.SyncTransforms();
                    for (int step = 0; step < 500; step++)
                        motor.Move(direction * 0.08f + Vector3.down * 0.04f);
                    var position = motor.transform.position;
                    Assert.Greater(new Vector2(position.x, position.z).magnitude, 10, "Player must be able to walk.");
                    Assert.Less(Mathf.Abs(position.x), 14, "East/west barrier failed.");
                    Assert.Less(Mathf.Abs(position.z), 14, "North/south barrier failed.");
                    Assert.That(position.y, Is.InRange(-0.1f, 0.15f), "Player left the floor.");
                }
            }
            finally
            {
                if (session != null && session.IsRunning) await session.EndAsync();
                if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
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
