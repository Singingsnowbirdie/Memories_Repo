using System;
using System.Linq;
using System.Reflection;
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

namespace Memories.Tests.EditMode
{
    public sealed class TVRemoteTransferPersistenceTests
    {
        private const string House = "Assets/_Project/Locations/House/House.unity";
        private static T Field<T>(object owner, string name) => (T)owner.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static GameObject Named(Scene scene, string name) => All<Transform>(scene)
            .Single(value => value.name == name).gameObject;

        [Test]
        public void TransferThenReloadRestoresHandAndMatchWithoutRespawningItems()
        {
            string key = "Memories.Tests.TVTransfer." + Guid.NewGuid().ToString("N");
            var items = new[] { "TVRemote", "BatteryAA", "BatteryAA_02" }.Select(name =>
                AssetDatabase.LoadAssetAtPath<QuestItem>("Assets/_Project/Prefabs/InteractiveItems/" + name + ".asset")).ToArray();
            Assert.IsTrue(items.All(item => item != null));
            Scene scene = default;
            var variables = new CustomVariableStore(persistProgress: false);
            using var texts = new StoryTextPresenter(new NoTextView());
            try
            {
                var inventory = new QuestInventory();
                using (var save = new QuestInventorySave(inventory, items, key))
                {
                    foreach (var item in items) Assert.IsTrue(inventory.TryAdd(item));
                    scene = EditorSceneManager.OpenPreviewScene(House);
                    Initialize(scene, inventory, variables, texts);
                    Assert.IsFalse(Named(scene, "Remote in hand").activeSelf);
                    var woman = Named(scene, "casual_Female_K_Gray_Seated").GetComponent<ReactionInteractable>();
                    Assert.IsTrue(woman.CanInteract);
                    woman.Interact();
                    AssertTransferred(scene, inventory, items);
                }
                EditorSceneManager.ClosePreviewScene(scene);
                scene = default;
                // A fresh inventory and a fresh scene: nothing relies on live object state.
                inventory = new QuestInventory();
                using (var save = new QuestInventorySave(inventory, items, key))
                {
                    Assert.IsEmpty(inventory.Items);
                    scene = EditorSceneManager.OpenPreviewScene(House);
                    Initialize(scene, inventory, variables, texts);
                    AssertTransferred(scene, inventory, items);
                    Named(scene, "casual_Female_K_Gray_Seated").GetComponent<ReactionInteractable>().Interact();
                    AssertTransferred(scene, inventory, items);
                }
            }
            finally
            {
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                QuestInventorySave.Clear(key);
            }
        }

        private static void Initialize(Scene scene, QuestInventory inventory, CustomVariableStore variables, StoryTextPresenter texts)
        {
            // The pickup prefabs are designer-placeable and are not yet instantiated in the saved House scene.
            foreach (string name in new[] { "TVRemote_Pickup", "BatteryAA_Pickup", "BatteryAA_02_Pickup" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Prefabs/InteractiveItems/" + name + ".prefab");
                Assert.IsNotNull(prefab, name);
                PrefabUtility.InstantiatePrefab(prefab, scene);
            }
            // Mirror the startup order: initialize even inactive interactables, then restore the remembered woman.
            foreach (var interactable in All<ReactionInteractable>(scene))
                interactable.Initialize(texts, null, inventory: inventory, variables: variables);
            var photo = All<PhotoMemory>(scene).Single();
            variables.Set(Field<CustomVariableBank>(photo, "variableBank"), Field<string>(photo, "revealedVariableId"), true);
            photo.Initialize(texts, null, inventory, variables);
        }

        private static void AssertTransferred(Scene scene, QuestInventory inventory, QuestItem[] items)
        {
            Assert.IsEmpty(inventory.Items);
            foreach (var item in items)
            {
                Assert.IsTrue(inventory.IsConsumed(item.Id), item.Id);
                var pickups = All<ReactionInteractable>(scene).Where(component =>
                    Field<ReactionGraph>(component, "graph").nodes.OfType<ReactionNode>()
                        .Any(node => node.Kind == ReactionKind.AddInventoryItem && node.Item == item)).ToArray();
                Assert.IsNotEmpty(pickups, "Expected original scene pickup for " + item.Id);
                foreach (var pickup in pickups) Assert.IsFalse(pickup.gameObject.activeSelf, "Pickup respawned: " + item.Id);
            }
            Assert.IsTrue(Named(scene, "Remote in hand").activeInHierarchy);
            Assert.AreEqual("RemoteSocket", Named(scene, "Remote in hand").transform.parent.name);
            Assert.IsFalse(Named(scene, "TV Static Screen").activeSelf);
            var match = Named(scene, "TV Baseball Screen");
            Assert.IsTrue(match.activeInHierarchy);
            Assert.IsNotNull(match.GetComponent<Memories.Gameplay.TVComicPlayback>());
            var material = match.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual("Memories/TV Baseball", material.shader.name);
            Assert.IsNotNull(material.GetTexture("_Atlas"));
            Assert.AreEqual(4f, material.GetFloat("_FrameDuration"));
        }

        private sealed class NoTextView : IStoryTextView
        {
            public CancellationToken LifetimeToken => CancellationToken.None;
            public UniTask ShowAsync(string text, CancellationToken token) => throw new AssertionException("No dialogue should play on transfer or restore.");
            public UniTask ChangeTextAsync(string text, CancellationToken token) => UniTask.CompletedTask;
            public UniTask HideAsync(CancellationToken token) => UniTask.CompletedTask;
            public void HideImmediately() { }
        }
    }
}
