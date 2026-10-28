using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Memories.Contracts;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Memories.Tests.PlayMode
{
    public sealed class InventoryHudTests
    {
        private sealed class PauseInput : IPauseInput
        {
            public event Action PauseRequested { add { } remove { } }
            public void SetPaused(bool paused) { }
        }

        private static QuestItem Item(string id, string name)
        {
            var item = ScriptableObject.CreateInstance<QuestItem>();
            item.name = name;
            typeof(QuestItem).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, id);
            return item;
        }

        [Test]
        public void DuplicateAndInvalidItemsDoNotChangeInventory()
        {
            var item = Item("key", "Ключ");
            var duplicate = Item("key", "Другой ключ");
            var invalid = Item("", "Без ID");
            try
            {
                var inventory = new QuestInventory();
                int changes = 0;
                inventory.Changed += () => changes++;
                Assert.IsFalse(inventory.TryAdd(null));
                Assert.IsFalse(inventory.TryAdd(invalid));
                Assert.IsTrue(inventory.TryAdd(item));
                Assert.IsFalse(inventory.TryAdd(duplicate));
                Assert.AreEqual(1, changes);
                Assert.AreEqual(1, inventory.Items.Count);
                Assert.IsTrue(inventory.Contains("key"));
            }
            finally { Object.DestroyImmediate(item); Object.DestroyImmediate(duplicate); Object.DestroyImmediate(invalid); }
        }

        [UnityTest]
        public IEnumerator PickupUpdatesHudAndNewHudKeepsSessionItems()
        {
            var inventory = new QuestInventory();
            var mode = new GameplayModeState(new PauseInput());
            var item = Item("key", "Ключ от дома");
            var pickupRoot = new GameObject("Test quest pickup");
            var hudRoot = new GameObject("Test inventory HUD", typeof(RectTransform));
            GameObject nextHudRoot = null;
            float previousTime = Time.timeScale;
            bool previousAudioPause = AudioListener.pause;
            try
            {
                var pickup = pickupRoot.AddComponent<QuestItemPickup>();
                typeof(QuestItemPickup).GetField("item", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pickup, item);
                pickup.Initialize(inventory);
                hudRoot.AddComponent<InventoryHud>().Initialize(inventory, mode);
                Assert.IsTrue(pickup.CanInteract);
                pickup.Interact();
                Assert.IsFalse(pickupRoot.activeSelf);
                yield return null;
                Assert.IsTrue(hudRoot.GetComponentsInChildren<Text>().Any(t => t.text == "Ключ от дома"));
                Assert.IsTrue(hudRoot.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget));
                var panel = (RectTransform)hudRoot.transform.GetChild(0);
                Assert.AreEqual(0, panel.anchorMin.x);
                Assert.AreEqual(0, panel.anchorMax.x);
                mode.TryEnter(GameplayMode.Paused);
                yield return null;
                Assert.IsFalse(hudRoot.GetComponent<Canvas>().enabled);
                mode.Exit(GameplayMode.Paused);
                Object.Destroy(hudRoot);
                yield return null;
                nextHudRoot = new GameObject("Next location HUD", typeof(RectTransform));
                nextHudRoot.AddComponent<InventoryHud>().Initialize(inventory, mode);
                yield return null;
                Assert.IsTrue(nextHudRoot.GetComponentsInChildren<Text>().Any(t => t.text == "Ключ от дома"));
                Assert.IsTrue(nextHudRoot.GetComponent<Canvas>().enabled);
                pickupRoot.SetActive(true);
                pickup.Initialize(inventory);
                Assert.IsFalse(pickupRoot.activeSelf, "Collected pickup must stay hidden when a location is revisited.");
            }
            finally
            {
                mode.Exit(GameplayMode.Paused);
                Time.timeScale = previousTime;
                AudioListener.pause = previousAudioPause;
                Object.Destroy(hudRoot);
                Object.Destroy(nextHudRoot);
                Object.Destroy(pickupRoot);
                Object.Destroy(item);
            }
        }
    }
}
