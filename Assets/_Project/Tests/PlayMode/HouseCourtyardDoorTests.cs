using System.Collections;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Player;
using Memories.Gameplay.Reactions;
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
        [UnityTest] public IEnumerator BackwardsExitTurnsTowardFenceBeforeReplacingHouse() => Check(false, false, true);

        private static IEnumerator Check(bool main, bool previouslyFound = false, bool backwards = false) => UniTask.ToCoroutine(async () =>
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
                var door = Object.FindFirstObjectByType<OpenCourtyardDoorReaction>();
                Assert.IsNotNull(door);
                Assert.IsNull(door.GetComponent<ReactionInteractable>(), "The house exit must not be an interactive object.");
                var memories = session.SessionScope.Container.Resolve<MemoryCollection>();
                if (!previouslyFound)
                {
                    Assert.IsNull(new InteractionProbe(motor.GetComponentInChildren<Camera>()).FindTarget());
                    Assert.IsFalse(door.IsOpen, "The exit must stay closed until the custom reaction.");
                    memories.Collect("unrelated-memory");
                    await UniTask.NextFrame();
                    Assert.IsFalse(door.IsOpen, "Other memories must not unlock this door.");
                    memories.Collect("house-tv-baseball");
                    await UniTask.Delay(1000);
                    Assert.IsFalse(door.IsOpen, "Collecting the TV memory must not open the door before the graph reaction.");
                    Assert.IsEmpty(door.GetComponentsInChildren<Camera>(), "Opening must not start from the memory event.");
                    var reaction = Object.FindFirstObjectByType<OpenCourtyardDoorReaction>(FindObjectsInactive.Include);
                    Assert.IsNotNull(reaction);
                    var closeEntrance = Object.FindFirstObjectByType<CloseEntranceDoorsReaction>(FindObjectsInactive.Include);
                    Assert.IsNotNull(closeEntrance);
                    closeEntrance.Execute();
                    closeEntrance.Execute();
                    reaction.Execute();
                    reaction.Execute(); // Repeated playback must not start a second opening.
                }
                deadline = Time.realtimeSinceStartup + 25;
                while (!door.IsOpen && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(door.IsOpen);
                var entrance = Object.FindFirstObjectByType<CloseEntranceDoorsReaction>(FindObjectsInactive.Include);
                Assert.IsNotNull(entrance);
                Assert.IsTrue(Physics.Raycast(entrance.transform.TransformPoint(new Vector3(0.5f, 1, 1)),
                    -entrance.transform.forward, out var entranceHit, 2f), "Closed entrance must block the player.");
                Assert.IsTrue(entranceHit.collider.transform.IsChildOf(entrance.transform));
                Assert.IsNull(entrance.GetComponent<ReactionInteractable>(), "The player must not be able to reopen the entrance.");
                Assert.IsNull(door.GetComponent<ReactionInteractable>());
                Assert.IsTrue(SceneManager.GetSceneByPath(TestLocationLauncher.BackyardPath).isLoaded);
                Assert.IsTrue(SceneManager.GetSceneByPath(TestLocationLauncher.HousePath).isLoaded, "House must remain in place while looking through the open door.");
                var playerScene = motor.gameObject.scene;
                Assert.AreEqual(app.gameObject.scene, playerScene);
                var camera = motor.GetComponentInChildren<Camera>();
                var movement = session.LocationScope.Container.Resolve<PlayerMovementModel>();
                var gameplayScope = session.LocationScope;
                Assert.IsEmpty(door.GetComponentsInChildren<Camera>(), "No portal camera is allowed.");
                Assert.AreEqual(1, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled));
                var yard = SceneManager.GetSceneByPath(TestLocationLauncher.BackyardPath);
                var entry = yard.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BackyardEntranceReaction>()).Single();
                var entryData = new UnityEditor.SerializedObject(entry);
                var lightsData = entryData.FindProperty("lights");
                var entryLights = Enumerable.Range(0, lightsData.arraySize)
                    .Select(i => (Light)lightsData.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                Assert.AreEqual(8, entryLights.Length);
                Assert.IsTrue(entryLights.All(l => !l.enabled));
                var facade = yard.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .First(t => t.name.StartsWith("Фанерный дом"));
                Assert.IsFalse(facade.GetComponent<Renderer>().enabled, "The facade must not obstruct the passage.");
                Assert.IsFalse(facade.GetComponent<Collider>().enabled);
                var arrival = yard.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .First(t => t.name == door.ArrivalMarker);
                Assert.Less(Vector3.Distance(arrival.position, door.transform.position), .001f);
                Assert.Less(Quaternion.Angle(arrival.rotation, door.transform.rotation), .001f);
                var islands = yard.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject)
                    .Where(r => r.name.StartsWith("Остров_")).ToArray();
                Assert.AreEqual(6, islands.Length);
                var islandPositions = islands.Select(r => r.transform.position).ToArray();
                await UniTask.Delay(300);
                for (int i = 0; i < islands.Length; i++)
                {
                    Assert.Less(Vector3.Distance(islandPositions[i], islands[i].transform.position), 1f,
                        islands[i].name + " must not return to its coordinates before scene alignment.");
                    Assert.Less(Vector3.Distance(arrival.position, islands[i].transform.position), 100f,
                        islands[i].name + " must remain beside the connected backyard.");
                }
                var forward = door.transform.forward;
                var origin = door.transform.position;
                motor.Teleport(origin - forward * 1.2f + Vector3.up * .05f, door.transform.eulerAngles.y);
                movement.ResetMotion(door.transform.eulerAngles.y + (backwards ? 180 : 0));
                movement.SetInitialPitch(17);
                motor.SetLook(movement.Yaw, movement.Pitch);
                if (!main) Capture(camera, "Temp/streaming-door-open.png");
                var presenter = session.LocationScope.Container.Resolve<PlayerPresenter>();
                presenter.ControlsEnabled = false;
                int steps = 0;
                Vector3? lockedCameraPosition = null;
                var mode = session.LocationScope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>();
                while (session.CurrentLocationScene != yard && steps++ < 400)
                {
                    var before = motor.transform.position;
                    if (!entry.IsRunning) motor.Move(forward * .04f + Vector3.down * .01f);
                    else
                    {
                        Assert.IsTrue(mode.ControlsBlocked);
                        if (!lockedCameraPosition.HasValue) lockedCameraPosition = camera.transform.position;
                        Assert.Less(Vector3.Distance(lockedCameraPosition.Value, camera.transform.position), .02f);
                        Assert.IsFalse(facade.GetComponent<Renderer>().enabled);
                    }
                    await UniTask.NextFrame();
                    Assert.Less(Vector3.Distance(before, motor.transform.position), .12f, "Transition moved the player.");
                    Assert.AreSame(motor, session.LocationScope.Container.Resolve<PlayerMotor>());
                    Assert.AreEqual(playerScene, motor.gameObject.scene);
                    Assert.AreSame(camera, motor.GetComponentInChildren<Camera>());
                }
                if (session.CurrentLocationScene != yard)
                {
                    var nearby = Physics.OverlapSphere(motor.transform.position + Vector3.up, 2f);
                    File.WriteAllText("Temp/streaming-blockers.txt", "Player=" + motor.transform.position +
                        " origin=" + origin + " forward=" + forward + " clearance=" + door.ExitClearance + "\n" +
                        string.Join("\n", nearby.Select(c => c.name + " " + c.GetType().Name + " " + c.bounds)));
                    Capture(camera, "Temp/streaming-blocked.png");
                }
                Assert.AreEqual(yard, session.CurrentLocationScene,
                    "Could not walk through the joined doorway. Player=" + motor.transform.position + " clearance=" + door.ExitClearance);
                var inscription = yard.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == "Ben Alex Friends Forever");
                Assert.Less(Vector3.Angle(camera.transform.forward, inscription.position - camera.transform.position), 1f,
                    "House replacement must only start after the player faces the inscription, including a backwards exit.");
                int previousLit = 0;
                var observedPairs = new System.Collections.Generic.HashSet<int>();
                while (session.IsTransitioning)
                {
                    Assert.IsTrue(mode.ControlsBlocked);
                    Assert.Less(Vector3.Distance(lockedCameraPosition.Value, camera.transform.position), .02f);
                    int lit = entryLights.Count(l => l.enabled);
                    Assert.AreEqual(0, lit % 2, "Lanterns must switch on in pairs.");
                    Assert.GreaterOrEqual(lit, previousLit);
                    for (int i = 0; i < entryLights.Length; i++)
                        Assert.AreEqual(i < lit, entryLights[i].enabled, "Pairs must light from the entrance toward the fence.");
                    previousLit = lit;
                    observedPairs.Add(lit);
                    await UniTask.NextFrame();
                }
                CollectionAssert.IsSubsetOf(new[] { 2, 4, 6, 8 }, observedPairs);
                Assert.AreSame(gameplayScope, session.LocationScope);
                Assert.AreSame(movement, session.LocationScope.Container.Resolve<PlayerMovementModel>());
                Assert.Less(Vector3.Angle(camera.transform.forward, inscription.position - camera.transform.position), 1f);
                Assert.IsFalse(entry.IsRunning);
                Assert.IsFalse(mode.ControlsBlocked);
                Assert.IsTrue(entryLights.All(l => l.enabled));
                Assert.IsTrue(facade.GetComponent<Renderer>().enabled);
                Assert.IsTrue(facade.GetComponent<Collider>().enabled);
                if (!main) Capture(camera, "Temp/streaming-yard-entered.png");
                deadline = Time.realtimeSinceStartup + 15;
                while ((SceneManager.GetSceneByPath(TestLocationLauncher.HousePath).isLoaded ||
                    SceneManager.GetSceneByPath(TestLocationLauncher.HighwayPath).isLoaded) && Time.realtimeSinceStartup < deadline)
                    await UniTask.NextFrame();
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.HousePath).isLoaded);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.HighwayPath).isLoaded);
                await UniTask.WaitUntil(() => !session.IsTransitioning);
                foreach (var island in islands)
                    foreach (var renderer in island.GetComponentsInChildren<Renderer>())
                        Assert.IsTrue(renderer.enabled, island.name + " must be visible after leaving the house.");
                Assert.AreEqual(1, Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(playerScene, camera.gameObject.scene);
                for (int i = 0; i < 450; i++) motor.Move(forward * .05f + Vector3.down * .04f);
                float distance = Vector3.Dot(motor.transform.position - origin, forward);
                Assert.That(distance, Is.InRange(20, 22), "The far fence must be reachable and solid.");
                for (int i = 0; i < 500; i++) motor.Move(-forward * .05f + Vector3.down * .04f);
                distance = Vector3.Dot(motor.transform.position - origin, forward);
                Assert.That(distance, Is.InRange(.1f, 1f), "The restored facade must block returning to the unloaded house.");
            }
            finally
            {
                if (hadSave) PlayerPrefs.SetString(MemoryCollection.DefaultKey, savedMemories);
                else PlayerPrefs.DeleteKey(MemoryCollection.DefaultKey);
                PlayerPrefs.Save();
                if (session != null && session.IsRunning)
                {
                    await UniTask.WaitUntil(() => !session.IsTransitioning);
                    await session.EndAsync();
                }
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
