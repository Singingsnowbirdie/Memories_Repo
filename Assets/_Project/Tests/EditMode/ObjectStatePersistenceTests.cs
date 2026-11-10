using System;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Tests.EditMode
{
    public sealed class ObjectStatePersistenceTests
    {
        private static ObjectStateTracker Mark(GameObject target, string id)
        {
            var tracker = target.AddComponent<ObjectStateTracker>();
            var serialized = new SerializedObject(tracker);
            serialized.FindProperty("persistentId").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tracker;
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase(" school.object")]
        public void InvalidExplicitIdFailsBeforeRegistration(string id)
        {
            var target = new GameObject("Invalid key");
            try
            {
                using (var store = new ObjectStateStore(false))
                    Assert.Throws<InvalidOperationException>(() => store.Register(target, id));
                Assert.IsNull(target.GetComponent<ObjectStateTracker>());
            }
            finally { UnityEngine.Object.DestroyImmediate(target); }
        }

        [Test]
        public void DuplicateSceneKeysAreRejectedBeforeRestoringAnyObject()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            string key = "Memories.Tests.Objects." + Guid.NewGuid().ToString("N");
            try
            {
                var first = new GameObject("First");
                var second = new GameObject("Second");
                SceneManager.MoveGameObjectToScene(first, scene);
                SceneManager.MoveGameObjectToScene(second, scene);
                Mark(first, "test.duplicate");
                Mark(second, "test.duplicate");
                PlayerPrefs.SetString(key, "{\"entries\":[{\"id\":\"test.duplicate\",\"state\":\"{\\\"active\\\":false,\\\"parts\\\":[]}\"}]}");
                using (var store = new ObjectStateStore(saveKey: key))
                    Assert.Throws<InvalidOperationException>(() => store.RestoreScene(scene));
                Assert.IsTrue(first.activeSelf);
                Assert.IsTrue(second.activeSelf);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); PlayerPrefs.DeleteKey(key); }
        }

        [Test]
        public void SceneRestoreUsesExplicitIdentitiesAndIgnoresUnmarkedObjects()
        {
            string key = "Memories.Tests.Objects." + Guid.NewGuid().ToString("N");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var authored = new GameObject("Authored object");
                var runtime = new GameObject("Runtime object");

                SceneManager.MoveGameObjectToScene(authored, scene);
                SceneManager.MoveGameObjectToScene(runtime, scene);

                var serialized = new SerializedObject(authored.AddComponent<ObjectStateTracker>());
                serialized.FindProperty("persistentId").stringValue = "test.authored";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                using (var store = new ObjectStateStore(saveKey: key))
                {
                    store.RestoreScene(scene);
                    Assert.IsNotNull(authored.GetComponent<ObjectStateTracker>());
                    Assert.IsNull(runtime.GetComponent<ObjectStateTracker>());

                    authored.SetActive(false);
                    store.Flush();
                }

                authored.SetActive(true);
                using (var store = new ObjectStateStore(saveKey: key))
                {
                    store.RestoreScene(scene);
                    Assert.IsFalse(authored.activeSelf);
                    Assert.IsTrue(runtime.activeSelf);
                    Assert.DoesNotThrow(() => store.RestoreScene(scene));
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void DuplicateExplicitIdentityStillFails()
        {
            var first = new GameObject("First");
            var second = new GameObject("Second");
            try
            {
                using (var store = new ObjectStateStore(false))
                {
                    store.Register(first, "same-id");
                    Assert.Throws<InvalidOperationException>(() => store.Register(second, "same-id"));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void ReloadRestoresInactiveObjectAndDisabledInteractionByIdentity()
        {
            string key = "Memories.Tests.Objects." + Guid.NewGuid().ToString("N");
            var first = new GameObject("Old name");
            GameObject second = null;
            try
            {
                var interaction = first.AddComponent<ReactionInteractable>();
                using (var store = new ObjectStateStore(saveKey: key))
                {
                    store.Register(first, "scene-object-1");
                    interaction.SetInteractionEnabled(false);
                    first.SetActive(false);
                    store.Flush();
                }
                UnityEngine.Object.DestroyImmediate(first);
                second = new GameObject("Renamed object");
                var restored = second.AddComponent<ReactionInteractable>();
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
