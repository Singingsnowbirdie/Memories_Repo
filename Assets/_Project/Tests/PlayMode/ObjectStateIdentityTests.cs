using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class ObjectStateIdentityTests
    {
        [UnityTest]
        public IEnumerator AllLocationsCanStartGameSessions() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Explicit identity session test");
            root.SetActive(false);
            var launcher = root.AddComponent<TestLocationLauncher>();
            var app = root.AddComponent<AppLifetimeScope>();
            app.StartAutomatically = false;
#if UNITY_EDITOR
            app.UseOpenLocationForEditorPlay = true;
#endif
            root.SetActive(true);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                foreach (string location in new[] { "Highway", "House", "Backyard", "Garage", "School" })
                {
                    launcher.SelectedLocation = (TestLocationLauncher.Location)Enum.Parse(typeof(TestLocationLauncher.Location), location);
                    await session.StartAsync($"Assets/_Project/Locations/{location}/{location}.unity");
                    Assert.IsTrue(session.IsRunning, location);
                    Assert.IsFalse(session.IsTransitioning, location);
                    Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Any(camera => camera.isActiveAndEnabled), location);
                    await session.EndAsync();
                }
            }
            finally
            {
                if (session.IsRunning) await session.EndAsync();
                UnityEngine.Object.Destroy(root);
                await UniTask.NextFrame();
            }
        });

        [UnityTest]
        public IEnumerator AllLocationsRestoreByExplicitKeysAfterReload()
        {
            var allIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string location in new[] { "Highway", "House", "Backyard", "Garage", "School" })
            {
                string path = $"Assets/_Project/Locations/{location}/{location}.unity";
                string key = "Memories.Tests.ExplicitIds." + Guid.NewGuid().ToString("N");
                Scene scene = default;
                ObjectStateStore store = null;
                try
                {
                    yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                    scene = SceneManager.GetSceneByPath(path);
                    var trackers = Trackers(scene);
                    foreach (var tracker in trackers)
                    {
                        Assert.IsFalse(string.IsNullOrWhiteSpace(tracker.PersistentId), tracker.name);
                        Assert.IsTrue(allIds.Add(tracker.PersistentId), tracker.PersistentId);
                    }
                    foreach (var participant in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                        .Where(c => c is IObjectStateParticipant participant && ObjectStateStore.RequiresPersistence(participant)))
                        Assert.IsNotNull(participant.GetComponent<ObjectStateTracker>(), participant.name);
                    store = new ObjectStateStore(saveKey: key);
                    store.RestoreScene(scene);
                    string changedId = trackers.FirstOrDefault()?.PersistentId;
                    if (trackers.Length > 0) trackers[0].gameObject.SetActive(false);
                    var before = trackers.Select(t => t.PersistentId).OrderBy(id => id).ToArray();
                    store.UnloadScene(scene);
                    store.Dispose(); store = null;
                    yield return SceneManager.UnloadSceneAsync(scene);

                    yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                    scene = SceneManager.GetSceneByPath(path);
                    trackers = Trackers(scene);
                    CollectionAssert.AreEqual(before, trackers.Select(t => t.PersistentId).OrderBy(id => id).ToArray());
                    store = new ObjectStateStore(saveKey: key);
                    store.RestoreScene(scene);
                    if (changedId != null) Assert.IsFalse(trackers.Single(t => t.PersistentId == changedId).gameObject.activeSelf);
                    store.UnloadScene(scene);
                    store.Dispose(); store = null;
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
                finally
                {
                    store?.Dispose();
                    PlayerPrefs.DeleteKey(key);
                    if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
                }
            }
            Assert.Greater(allIds.Count, 0);
        }

        private static ObjectStateTracker[] Trackers(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<ObjectStateTracker>(true)).ToArray();
    }
}
