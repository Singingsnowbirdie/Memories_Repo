using Memories.Bootstrap;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Editor
{
    public static class OpenLocationPlay
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartOpenLocation()
        {
            if (Object.FindFirstObjectByType<AppLifetimeScope>(FindObjectsInactive.Include) != null) return;
            var location = SceneManager.GetActiveScene();
            string path = location.path;
            if (!path.StartsWith("Assets/_Project/Locations/") && path != AppLifetimeScope.DefaultLocationPath) return;

            // Runtime-only bootstrap. Keep the open scene and its unsaved edits intact.
            var runtimeScene = SceneManager.CreateScene("Editor location bootstrap");
            var root = new GameObject("Запуск открытой локации");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, runtimeScene);
            if (path == TestLocationLauncher.HighwayPath || path == TestLocationLauncher.HousePath ||
                path == TestLocationLauncher.CourtyardPath || path == AppLifetimeScope.DefaultLocationPath)
            {
                var launcher = root.AddComponent<TestLocationLauncher>();
                launcher.SelectedLocation = path == TestLocationLauncher.HousePath ? TestLocationLauncher.Location.House :
                    path == TestLocationLauncher.HighwayPath ? TestLocationLauncher.Location.Highway :
                    path == TestLocationLauncher.CourtyardPath ? TestLocationLauncher.Location.Courtyard :
                    TestLocationLauncher.Location.Highway;
            }
            var app = root.AddComponent<AppLifetimeScope>();
            var settings = new SerializedObject(app);
            settings.FindProperty("initialLocationPath").stringValue = path;
            settings.ApplyModifiedPropertiesWithoutUndo();
            app.UseOpenLocationForEditorPlay = true;
            root.SetActive(true);
        }
    }
}
