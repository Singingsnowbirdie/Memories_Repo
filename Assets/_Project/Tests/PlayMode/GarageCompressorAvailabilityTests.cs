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
                father.Initialize(texts, null, inventory: inventory, variables: variables);
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
