using System;
using System.Reflection;
using Memories.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Memories.Tests.EditMode
{
    public sealed class QuestInventorySaveTests
    {
        private string key;
        private QuestItem item;
        private QuestItem restoredItem;

        [SetUp]
        public void SetUp()
        {
            key = "Memories.Tests.Inventory." + Guid.NewGuid().ToString("N");
            item = CreateItem("key");
            restoredItem = CreateItem("key");
        }

        private static QuestItem CreateItem(string id)
        {
            var result = ScriptableObject.CreateInstance<QuestItem>();
            typeof(QuestItem).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(result, id);
            return result;
        }

        [TearDown]
        public void TearDown()
        {
            QuestInventorySave.Clear(key);
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(restoredItem);
        }

        [Test]
        public void ConsumedItemsStayRemovedAndCannotBePickedUpAgainAfterReload()
        {
            var inventory = new QuestInventory();
            using (var save = new QuestInventorySave(inventory, new[] { item }, key))
            {
                inventory.TryAdd(item);
                Assert.IsTrue(inventory.TryConsume(item));
                Assert.IsFalse(inventory.Contains(item.Id));
                Assert.IsTrue(inventory.WasCollected(item.Id));
                Assert.IsFalse(inventory.TryConsume(item));
            }
            var restored = new QuestInventory();
            using (var save = new QuestInventorySave(restored, new[] { restoredItem }, key))
            {
                Assert.IsEmpty(restored.Items);
                Assert.IsTrue(restored.IsConsumed(item.Id));
                Assert.IsFalse(restored.TryAdd(restoredItem));
            }
            QuestInventorySave.Clear(key);
            var restarted = new QuestInventory();
            using (var save = new QuestInventorySave(restarted, new[] { item }, key))
                Assert.IsTrue(restarted.TryAdd(item));
        }

        [Test]
        public void TemporarySessionIgnoresAndPreservesExistingSave()
        {
            var savedInventory = new QuestInventory();
            using (var save = new QuestInventorySave(savedInventory, new[] { item }, key))
                savedInventory.TryAdd(item);
            string before = PlayerPrefs.GetString(key);
            var temporary = new QuestInventory();
            using (var save = new QuestInventorySave(temporary, new[] { item }, key, persistProgress: false))
            {
                Assert.IsEmpty(temporary.Items);
                Assert.IsTrue(temporary.TryAdd(item));
                Assert.IsTrue(temporary.Contains(item.Id));
            }
            Assert.AreEqual(before, PlayerPrefs.GetString(key));
        }

        [Test]
        public void PickupIsSavedImmediatelyAndRestoredFromCurrentCatalog()
        {
            var inventory = new QuestInventory();
            using (var save = new QuestInventorySave(inventory, new[] { item }, key))
            {
                Assert.IsFalse(PlayerPrefs.HasKey(key));
                inventory.TryAdd(item);
                Assert.IsTrue(PlayerPrefs.HasKey(key));
                var restored = new QuestInventory();
                using (var next = new QuestInventorySave(restored, new[] { restoredItem }, key))
                {
                    Assert.AreEqual(1, restored.Items.Count);
                    Assert.AreSame(restoredItem, restored.Items[0]);
                    Assert.IsFalse(restored.TryAdd(restoredItem));

                }
            }
        }

        [Test]
        public void NewGameClearCannotBeUndoneByDisposedSession()
        {
            var inventory = new QuestInventory();
            var save = new QuestInventorySave(inventory, new[] { item }, key);
            inventory.TryAdd(item);
            save.Dispose();
            QuestInventorySave.Clear(key);
            var extra = CreateItem("other");
            try
            {
                inventory.TryAdd(extra);
                Assert.IsFalse(PlayerPrefs.HasKey(key));
                var restored = new QuestInventory();
                using (var next = new QuestInventorySave(restored, new[] { restoredItem }, key))
                    Assert.IsEmpty(restored.Items);
            }
            finally { Object.DestroyImmediate(extra); }
        }

        [Test]
        public void TemporarilyMissingDefinitionIsNotLostOnNextPickup()
        {
            PlayerPrefs.SetString(key, "{\"version\":1,\"ids\":[\"missing\"]}");
            using (var save = new QuestInventorySave(new QuestInventory(), new[] { item }, key)) { }
            var inventory = new QuestInventory();
            using (var save = new QuestInventorySave(inventory, new[] { item }, key)) inventory.TryAdd(item);
            var missing = CreateItem("missing");
            try
            {
                var restored = new QuestInventory();
                using (var save = new QuestInventorySave(restored, new[] { restoredItem, missing }, key))
                {
                    Assert.IsTrue(restored.Contains("missing"));
                    Assert.IsTrue(restored.Contains("key"));
                }
            }
            finally { Object.DestroyImmediate(missing); }
        }
    }
}
