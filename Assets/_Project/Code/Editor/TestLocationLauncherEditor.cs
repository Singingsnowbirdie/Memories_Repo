using Memories.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Editor
{
    [CustomEditor(typeof(TestLocationLauncher))]
    public sealed class TestLocationLauncherEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("location"), new GUIContent("Локация для теста"));
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.HelpBox("Загрузится только выбранная локация. Игрок появится в начале шоссе или перед входом в дом. Обычный Bootstrap не меняется.", MessageType.Info);
                if (GUILayout.Button("Запустить выбранную локацию"))
                {
                    var launcher = (TestLocationLauncher)target;
                    if (launcher.gameObject.scene.path != TestLocationLauncher.BootstrapPath)
                    {
                        EditorUtility.DisplayDialog("Тест локации", "Используйте инструмент в сцене TestBootstrap.", "OK");
                        return;
                    }
                    var scope = new SerializedObject(launcher.GetComponent<AppLifetimeScope>());
                    scope.FindProperty("startAutomatically").boolValue = true;
                    scope.ApplyModifiedProperties();
                    if (SceneManager.sceneCount > 1)
                    {
                        var selected = launcher.SelectedLocation;
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                        var scene = EditorSceneManager.OpenScene(TestLocationLauncher.BootstrapPath, OpenSceneMode.Single);
                        foreach (var root in scene.GetRootGameObjects())
                            if (root.TryGetComponent<TestLocationLauncher>(out var loaded))
                            {
                                loaded.SelectedLocation = selected;
                                loaded.GetComponent<AppLifetimeScope>().StartAutomatically = true;
                                EditorSceneManager.MarkSceneDirty(scene);
                            }
                    }
                    EditorApplication.isPlaying = true;
                }
            }
        }
    }
}
