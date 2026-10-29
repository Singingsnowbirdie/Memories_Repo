using Memories.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor;

namespace Memories.Tests.EditMode
{
    public sealed class SharedSkyTests
    {
        [Test]
        public void CamerasInDifferentScenesShareSkyAfterCameraReplacement()
        {
            var original = SceneManager.GetActiveScene();
            var originalSky = RenderSettings.skybox;
            var first = default(Scene);
            var second = default(Scene);
            var temporaryPath = AssetDatabase.GenerateUniqueAssetPath("Assets/SharedSkyTest.unity");
            GameObject owner = null;
            try
            {
                first = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                Assert.IsTrue(EditorSceneManager.SaveScene(first, temporaryPath));
                second = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(first);
                owner = Object.Instantiate(Resources.Load<GameObject>("SharedSky"));
                var controller = owner.GetComponent<GhostCosmosSky>();
                controller.SendMessage("Awake");
                var camera = new GameObject("First camera").AddComponent<Camera>();
                GhostCosmosSky.Bind(camera);
                var material = camera.GetComponent<Skybox>().material;
                Assert.IsNotNull(material);
                Assert.AreSame(material, RenderSettings.skybox);
                SceneManager.SetActiveScene(second);
                Assert.AreSame(material, camera.GetComponent<Skybox>().material);
                var nextCamera = new GameObject("Next camera").AddComponent<Camera>();
                GhostCosmosSky.Bind(nextCamera);
                Object.DestroyImmediate(camera.gameObject);
                Assert.AreSame(material, nextCamera.GetComponent<Skybox>().material);
                Assert.AreEqual(CameraClearFlags.Skybox, nextCamera.clearFlags);
                SceneManager.SetActiveScene(first);
                Assert.AreSame(material, nextCamera.GetComponent<Skybox>().material);
                Assert.IsTrue(material != null);
            }
            finally
            {
                if (owner != null) Object.DestroyImmediate(owner);
                if (second.IsValid()) EditorSceneManager.CloseScene(second, true);
                if (first.IsValid()) EditorSceneManager.CloseScene(first, true);
                AssetDatabase.DeleteAsset(temporaryPath);
                SceneManager.SetActiveScene(original);
                RenderSettings.skybox = originalSky;
            }
        }
    }
}
