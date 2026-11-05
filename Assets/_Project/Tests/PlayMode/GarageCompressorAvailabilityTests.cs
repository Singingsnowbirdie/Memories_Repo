#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class GarageCompressorAvailabilityTests
    {

private sealed class ComicInput : Memories.Contracts.IPauseInput, Memories.Contracts.IAdvanceInput
{
    public event Action PauseRequested;
    public event Action AdvanceRequested;
    public void SetPaused(bool paused) { }
    public void Advance() => AdvanceRequested?.Invoke();
}
private sealed class ComicView : Memories.Gameplay.Memories.IMemoryView
{
    public int Frames;
    public void Show() { }
    public void Hide() { }
    public UniTask ShowFrameAsync(Memories.Gameplay.Memories.MemoryFrameNode frame, CancellationToken token)
    { Frames++; return UniTask.CompletedTask; }
}
private sealed class ComicMusic : Memories.Gameplay.Memories.IMemoryMusic
{
    public void Execute(Memories.Gameplay.Memories.MemoryMusicNode node) { }
    public void StopImmediately() { }
}
private sealed class ComicSounds : Memories.Gameplay.Memories.IMemorySound
{
    public UniTask PlayAsync(Memories.Gameplay.Memories.MemorySoundNode node, CancellationToken token) => UniTask.CompletedTask;
    public void StopAll() { }
}

        private sealed class TextView : IStoryTextView
        {
            public CancellationToken LifetimeToken => CancellationToken.None;
            public UniTask ShowAsync(string text, CancellationToken token) => UniTask.CompletedTask;
            public UniTask ChangeTextAsync(string text, CancellationToken token) => UniTask.CompletedTask;
            public UniTask HideAsync(CancellationToken token) => UniTask.CompletedTask;
            public void HideImmediately() { }
        }

        private static GameObject Find(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(t => t.name == name).gameObject;

        [UnityTest]
        public IEnumerator RequestUnlocksPickupAndBothStatesSurviveReload()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Locations/Garage/Garage.unity");
            string key = "Memories.Tests.CompressorAvailability." + Guid.NewGuid().ToString("N");
            using var texts = new StoryTextPresenter(new TextView());
            var comicInput = new ComicInput();
            var comicView = new ComicView();
            var collection = new Memories.Gameplay.Memories.MemoryCollection(persistProgress: false);
            using var memories = new Memories.Gameplay.Memories.MemoryPresenter(collection, new GameplayModeState(comicInput), comicView, comicInput, new ComicMusic(), new ComicSounds());
            var inventory = new QuestInventory();
            var variables = new CustomVariableStore(persistProgress: false);
            try
            {
                var pickup = Find(scene, "Компрессор").GetComponent<ReactionInteractable>();
                pickup.Initialize(texts, null, inventory: inventory, variables: variables);
                Assert.IsTrue(pickup.gameObject.activeSelf);
                Assert.IsFalse(pickup.CanInteract);
                pickup.Interact();
                Assert.IsEmpty(inventory.Items);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Prefabs/InteractiveItems/Compressor_Pickup.prefab");
                using var liveSave = new ObjectStateStore(saveKey: key);
                liveSave.Register(pickup.gameObject, "compressor");
                void VerifyReload(bool expected)
                {
                    liveSave.Flush();
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    try
                    {
                        var loaded = instance.GetComponent<ReactionInteractable>();
                        loaded.Initialize(texts, null, inventory: inventory, variables: variables);
                        using var save = new ObjectStateStore(saveKey: key);
                        save.Register(instance, "compressor");
                        Assert.AreEqual(expected, loaded.CanInteract);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(instance); }
                }

                VerifyReload(false);
                // The same live session now explicitly unlocks the pickup in the request graph.
                var father = Find(scene, "Отец у заднего левого колеса").GetComponent<ReactionInteractable>();
                father.gameObject.SetActive(true);
                var hoodFather = Find(scene, "Отец у капота");
                var switchReaction = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true)).Single();
                switchReaction.Initialize(memories);
                liveSave.Register(father.gameObject, "father-wheel");
                liveSave.Register(hoodFather, "father-hood");
                liveSave.Register(Find(scene, "Компрессор у заднего левого колеса"), "delivered");
                father.Initialize(texts, memories, inventory: inventory, variables: variables);
                father.Interact();
                Assert.IsFalse(pickup.CanInteract, "Keep locked until the request finishes.");
                float deadline = Time.realtimeSinceStartup + 20f;
                while (father.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(father.IsRunning, "Request did not finish.");
                Assert.IsTrue(pickup.CanInteract);
                VerifyReload(true);
                pickup.Interact();
                Assert.AreEqual(1, inventory.Items.Count);
                Assert.IsFalse(pickup.gameObject.activeSelf);
                father.Interact();
                Assert.IsEmpty(inventory.Items);
                Assert.IsTrue(Find(scene, "Компрессор у заднего левого колеса").activeSelf);
                deadline = Time.realtimeSinceStartup + 2f;
                while (!memories.IsPlaying && father.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(memories.IsPlaying);
                Assert.AreEqual(1, comicView.Frames);
                Assert.IsFalse(father.gameObject.activeSelf, "Switch while comic is still playing.");
                Assert.IsTrue(hoodFather.activeSelf);
                Assert.IsTrue(memories.IsPlaying, "Hiding father must not cancel the comic.");
                for (int frame = 0; frame < 4; frame++)
                {
                    comicInput.Advance();
                    yield return null;
                    yield return null;
                }
                Assert.AreEqual(4, comicView.Frames);
                Assert.IsFalse(memories.IsPlaying);
                Assert.IsTrue(collection.Contains("garage_second_memory"));
                liveSave.Flush();
                var reload = EditorSceneManager.OpenPreviewScene("Assets/_Project/Locations/Garage/Garage.unity");
                try
                {
                    using var restored = new ObjectStateStore(saveKey: key);
                    var wheel = Find(reload, "Отец у заднего левого колеса");
                    var hood = Find(reload, "Отец у капота");
                    var compressor = Find(reload, "Компрессор у заднего левого колеса");
                    restored.Register(wheel, "father-wheel");
                    restored.Register(hood, "father-hood");
                    restored.Register(compressor, "delivered");
                    Assert.IsFalse(wheel.activeSelf);
                    Assert.IsTrue(hood.activeSelf);
                    Assert.IsTrue(compressor.activeSelf);
                    wheel.SetActive(true);
                    hood.SetActive(false);
                    reload.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true)).Single().Restore(collection);
                    Assert.IsFalse(wheel.activeSelf);
                    Assert.IsTrue(hood.activeSelf);
                    Assert.IsTrue(compressor.activeSelf);
                }
                finally { EditorSceneManager.ClosePreviewScene(reload); }
                yield return null;
                father.Interact();
                Assert.IsFalse(memories.IsPlaying, "Do not replay after the compressor was consumed.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}

#endif
