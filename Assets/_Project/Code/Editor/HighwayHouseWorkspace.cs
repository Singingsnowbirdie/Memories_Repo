using Memories.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Editor
{
    public static class HighwayHouseWorkspace
    {
        private const string Menu = "Tools/Memories/Шоссе + дом — настройка стыка";

        [MenuItem(Menu)]
        private static void Open()
        {
            // Открываем оригиналы аддитивно, не закрывая другие сцены и не теряя несохранённые правки.
            var highway = OpenIfNeeded(TestLocationLauncher.HighwayPath);
            OpenIfNeeded(TestLocationLauncher.HousePath);
            SceneManager.SetActiveScene(highway);

            var view = EditorWindow.GetWindow<SceneView>();
            view.Frame(new Bounds(new Vector3(4, 2, 301), new Vector3(30, 12, 24)), false);
            view.Focus();
        }

        [MenuItem(Menu, true)]
        private static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static Scene OpenIfNeeded(string path)
        {
            var scene = SceneManager.GetSceneByPath(path);
            return scene.IsValid() && scene.isLoaded
                ? scene
                : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
    }
}
