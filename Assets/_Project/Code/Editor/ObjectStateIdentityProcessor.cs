using System.Collections.Generic;
using Memories.Gameplay.Reactions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Editor
{
    // Проверяем заданные ключи. Не создаём идентификаторы и не изменяем обрабатываемую сцену.
    public sealed class ObjectStateIdentityProcessor : IProcessSceneWithReport, IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => ValidateProject();

        [MenuItem("Tools/Memories/Validate Persistent IDs")]
        public static void ValidateProject()
        {
            var ids = new Dictionary<string, string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Locations" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    new ObjectStateIdentityProcessor().OnProcessScene(scene, null);
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                    {
                        if (ids.TryGetValue(tracker.PersistentId, out var other))
                            throw new BuildFailedException($"Duplicate persistent ID '{tracker.PersistentId}': {other} and {path}/{tracker.name}");
                        ids.Add(tracker.PersistentId, path + "/" + tracker.name);
                    }
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            Debug.Log($"Validated {ids.Count} explicit persistent object IDs across all locations.");
        }
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var ids = new Dictionary<string, ObjectStateTracker>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component is IObjectStateParticipant participant && ObjectStateStore.RequiresPersistence(participant) &&
                    component.GetComponent<ObjectStateTracker>() == null)
                    throw new BuildFailedException($"Missing ObjectStateTracker and explicit persistent ID: {scene.path}/{component.name}");
            foreach (var root in scene.GetRootGameObjects())
            foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
            {
                ObjectStateStore.ValidateIdentity(tracker.gameObject, tracker.PersistentId);
                if (ids.TryGetValue(tracker.PersistentId, out var other))
                    throw new BuildFailedException($"Duplicate persistent ID '{tracker.PersistentId}' in {scene.path}: {other.name} and {tracker.name}");
                ids.Add(tracker.PersistentId, tracker);
            }
        }
    }

    [CustomEditor(typeof(ObjectStateTracker)), CanEditMultipleObjects]
    public sealed class ObjectStateTrackerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            foreach (ObjectStateTracker tracker in targets)
            {
                if (string.IsNullOrWhiteSpace(tracker.PersistentId) || tracker.PersistentId != tracker.PersistentId.Trim())
                    EditorGUILayout.HelpBox("Set a permanent technical ID, for example school.medicine_cabinet. It must be unique across the game.", MessageType.Error);
            }
        }
    }
}
