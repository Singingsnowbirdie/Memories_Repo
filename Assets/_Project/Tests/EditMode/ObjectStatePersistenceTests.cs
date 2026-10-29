using System;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;

namespace Memories.Tests.EditMode
{
    public sealed class PersistentInteractionStub : Interactable
    {
        public override string Prompt => "Test";
        public override void Interact() { }
    }

    public sealed class ObjectStatePersistenceTests
    {
        [Test]
        public void ReloadRestoresInactiveObjectAndDisabledInteractionByIdentity()
        {
            string key = "Memories.Tests.Objects." + Guid.NewGuid().ToString("N");
            var first = new GameObject("Old name");
            GameObject second = null;
            try
            {
                var interaction = first.AddComponent<PersistentInteractionStub>();
                using (var store = new ObjectStateStore(saveKey: key))
                {
                    store.Register(first, "scene-object-1");
                    interaction.SetInteractionEnabled(false);
                    first.SetActive(false);
                    store.Flush();
                }
                UnityEngine.Object.DestroyImmediate(first);
                second = new GameObject("Renamed object");
                var restored = second.AddComponent<PersistentInteractionStub>();
                using (var store = new ObjectStateStore(saveKey: key))
                {
                    store.Register(second, "scene-object-1");
                    Assert.IsFalse(second.activeSelf);
                    Assert.IsFalse(restored.InteractionEnabled);
                }
            }
            finally
            {
                if (first != null) UnityEngine.Object.DestroyImmediate(first);
                if (second != null) UnityEngine.Object.DestroyImmediate(second);
                PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void NonPersistentSessionDoesNotReadOrOverwriteSavedProgress()
        {
            string key = "Memories.Tests.Objects." + Guid.NewGuid().ToString("N");
            var target = new GameObject("Object");
            try
            {
                PlayerPrefs.SetString(key, "untouched");
                using (var store = new ObjectStateStore(false, key))
                {
                    store.Register(target, "id");
                    target.SetActive(false);
                    store.Flush();
                    Assert.AreEqual("untouched", PlayerPrefs.GetString(key));
                    store.Reset();
                    Assert.AreEqual("untouched", PlayerPrefs.GetString(key));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(target); PlayerPrefs.DeleteKey(key); }
        }

        [Test]
        public void SceneTeardownDoesNotOverwriteGameplayVisibility()
        {
            string key = "Memories.Tests.Objects." + Guid.NewGuid().ToString("N");
            var target = new GameObject("Object");
            try
            {
                using (var store = new ObjectStateStore(saveKey: key))
                {
                    store.Register(target, "id");
                    target.SetActive(false);
                    store.Flush();
                    target.SetActive(true);
                    store.UnloadScene(target.scene);
                    target.SetActive(false);
                }
                using (var store = new ObjectStateStore(saveKey: key))
                {
                    store.Register(target, "id");
                    Assert.IsTrue(target.activeSelf);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(target); PlayerPrefs.DeleteKey(key); }
        }
    }
}
