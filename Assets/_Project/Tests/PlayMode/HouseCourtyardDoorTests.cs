using System.Collections;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class HouseCourtyardDoorTests
    {
        [UnityTest] public IEnumerator StandaloneHouseOpensOntoVisibleCourtyard() => Check(false);
        [UnityTest] public IEnumerator MainBootstrapHouseOpensOntoVisibleCourtyard() => Check(true);
        [UnityTest] public IEnumerator PreviouslyFoundTVMemoryOpensHouseDoorOnLoad() => Check(false, true);

        private static IEnumerator Check(bool main, bool previouslyFound = false) => UniTask.ToCoroutine(async () =>
        {
            bool hadSave = PlayerPrefs.HasKey(MemoryCollection.DefaultKey);
            string savedMemories = PlayerPrefs.GetString(MemoryCollection.DefaultKey);
            PlayerPrefs.DeleteKey(MemoryCollection.DefaultKey);
            if (previouslyFound)
                new MemoryCollection(persistProgress: true).Collect("house-tv-baseball");
            GameObject root = null;
            Scene bootstrap = default;
            GameSession session = null;
            try
            {
                AppLifetimeScope app;
                if (main)
                {
                    await SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity", LoadSceneMode.Additive);
                    bootstrap = SceneManager.GetSceneByPath("Assets/_Project/Scenes/Bootstrap.unity");
                    app = bootstrap.GetRootGameObjects().Select(g => g.GetComponent<AppLifetimeScope>()).First(a => a != null);
                }
                else
                {
                    root = new GameObject("House courtyard test"); root.SetActive(false);
                    root.AddComponent<TestLocationLauncher>().SelectedLocation = TestLocationLauncher.Location.House;
                    app = root.AddComponent<AppLifetimeScope>(); root.SetActive(true);
                }
                session = app.Container.Resolve<GameSession>();
                float deadline = Time.realtimeSinceStartup + 25;
                while (!session.IsRunning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(session.IsRunning);
                var motor = session.LocationScope.Container.Resolve<PlayerMotor>();
                var cc = motor.GetComponent<CharacterController>();
                cc.enabled = false; motor.transform.position = new Vector3(0, .05f, 325.5f); cc.enabled = true;
                motor.SetLook(0, 0); Physics.SyncTransforms();
                var door = Object.FindFirstObjectByType<CourtyardDoor>();
                Assert.IsNotNull(door);
                Assert.IsFalse(door.CanInteract, "The house exit must never require interaction.");
                Assert.IsEmpty(door.Prompt);
                var memories = session.SessionScope.Container.Resolve<MemoryCollection>();
                if (!previouslyFound)
                {
                    Assert.IsNull(new InteractionProbe(motor.GetComponentInChildren<Camera>()).FindTarget());
                    door.Interact();
                    await UniTask.NextFrame();
                    Assert.IsFalse(door.IsOpen, "Pressing E must not open the exit.");
                    memories.Collect("unrelated-memory");
                    await UniTask.NextFrame();
                    Assert.IsFalse(door.IsOpen, "Other memories must not unlock this door.");
                    memories.Collect("house-tv-baseball");
                }
                deadline = Time.realtimeSinceStartup + 25;
                while (!door.IsOpen && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(door.IsOpen);
                Assert.IsFalse(door.CanInteract);
                Assert.IsTrue(SceneManager.GetSceneByPath(TestLocationLauncher.CourtyardPath).isLoaded);
                Assert.IsTrue(SceneManager.GetSceneByPath(TestLocationLauncher.HousePath).isLoaded, "House must remain in place while looking through the open door.");
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.HighwayPath).isLoaded,
                    "Highway must unload when opening the door, before entering the courtyard.");
                Assert.AreSame(motor, session.LocationScope.Container.Resolve<PlayerMotor>(),
                    "Opening the door must preserve the live player and camera.");
                Assert.AreEqual(TestLocationLauncher.HousePath, session.LocationScope.gameObject.scene.path);
                Assert.IsNull(GameObject.Find("Переход через дверь"), "No fade overlay is allowed.");
                var portal = door.GetComponentsInChildren<Camera>().Single();
                Assert.IsNotNull(portal.targetTexture);
                Assert.IsTrue(portal.targetTexture.IsCreated());
                Assert.AreEqual(1, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled));
                if (!main) Capture(motor.GetComponentInChildren<Camera>(), "Temp/house-courtyard-open.png");
                for (int i = 0; i < 100 && session.LocationScope.gameObject.scene.path != TestLocationLauncher.CourtyardPath; i++)
                { motor.Move(new Vector3(0, -.04f, .04f)); await UniTask.NextFrame(); }
                deadline = Time.realtimeSinceStartup + 15;
                while (SceneManager.GetSceneByPath(TestLocationLauncher.HousePath).isLoaded && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.AreEqual(TestLocationLauncher.CourtyardPath, session.LocationScope.gameObject.scene.path);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.HousePath).isLoaded);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.HighwayPath).isLoaded);
                motor = session.LocationScope.Container.Resolve<PlayerMotor>();
                Assert.That(motor.transform.position.z, Is.InRange(-14.6f, -14f));
                Assert.AreEqual(1, Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Length);
                for (int i = 0; i < 450; i++) motor.Move(new Vector3(0, -.04f, .05f));
                Assert.Greater(motor.transform.position.z, 7, "The eighteen-metre arch must be traversable.");
                Assert.That(motor.transform.position.y, Is.InRange(-.1f, .15f));
            }
            finally
            {
                if (hadSave) PlayerPrefs.SetString(MemoryCollection.DefaultKey, savedMemories);
                else PlayerPrefs.DeleteKey(MemoryCollection.DefaultKey);
                PlayerPrefs.Save();
                if (session != null && session.IsRunning) await session.EndAsync();
                if (root != null) Object.Destroy(root);
                if (bootstrap.IsValid() && bootstrap.isLoaded) await SceneManager.UnloadSceneAsync(bootstrap);
                await UniTask.NextFrame();
            }
        });

        private static void Capture(Camera camera, string path)
        {
            var rt = new RenderTexture(1280, 800, 24);
            camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = null; camera.targetTexture = null;
            Object.Destroy(image); rt.Release(); Object.Destroy(rt);
        }
    }
}
