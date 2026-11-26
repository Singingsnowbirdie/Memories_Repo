using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TempHangarVehicles
{
    static TempHangarVehicles() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/HangarVehicles.done")) return;
        try
        {
            const string path = "Assets/_Project/Locations/Hangar.unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var h = scene.GetRootGameObjects().Single(x => x.name == "Hangar").transform;
            if (h.Find("Vehicle Posters") != null) throw new Exception("Vehicle Posters already exists");
            string[] names = { "Car_Sport80s", "Car_Muscle90s", "Car_Rally80s", "Car_Tuner90s", "Car_Roadster90s", "Bike_Sport90s", "Bike_Cruiser80s", "Truck_Longhaul80s", "Truck_Cabover90s" };
            float[] zs = { -10f, -8.6f, -5.8f, -4.4f, -1.6f, 0f, 1.6f, 4.4f, 5.8f };
            var group = new GameObject("Vehicle Posters").transform;
            SceneManager.MoveGameObjectToScene(group.gameObject, scene);
            group.SetParent(h, false);
            for (int i = 0; i < names.Length; i++)
            {
                string asset = "CinemaPoster_" + names[i];
                string texturePath = "Assets/_Project/Art/Photos/" + asset + ".png";
                AssetDatabase.ImportAsset(texturePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (!texture) throw new Exception("Missing " + texturePath);
                string matPath = "Assets/_Project/Locations/HangarMaterials/" + asset + ".mat";
                if (File.Exists(matPath)) throw new Exception("Material already exists " + matPath);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.name = asset;
                mat.SetTexture("_BaseMap", texture);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", .12f);
                AssetDatabase.CreateAsset(mat, matPath);
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = asset;
                go.transform.SetParent(group, false);
                go.transform.localPosition = new Vector3(15.94f, 1.8f, zs[i]);
                go.transform.localRotation = Quaternion.Euler(0, 90, 0);
                go.transform.localScale = new Vector3(1.4f * texture.width / texture.height, 1.4f, 1);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial = mat;
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            for (int k = 0; k < 3; k++)
            {
                var cameraGo = new GameObject("Temporary poster preview");
                SceneManager.MoveGameObjectToScene(cameraGo, scene);
                var camera = cameraGo.AddComponent<Camera>();
                camera.transform.position = h.TransformPoint(new Vector3(14.8f, 1.8f, new float[] {-8, -2, 4}[k]));
                camera.transform.rotation = Quaternion.LookRotation(h.right, h.up);
                camera.orthographic = true;
                camera.orthographicSize = 2.2f;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = 3f;
                var rt = new RenderTexture(1800, 1000, 24);
                camera.targetTexture = rt;
                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var img = new Texture2D(1800, 1000, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0, 0, 1800, 1000), 0, 0);
                img.Apply();
                File.WriteAllBytes("Temp/HangarVehicles" + k + ".png", img.EncodeToPNG());
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(img);
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(cameraGo);
            }
            if (opened) EditorSceneManager.CloseScene(scene, true);
            File.WriteAllText("Temp/HangarVehicles.done", "OK: 9 vehicle posters added; existing posters preserved.");
        }
        catch (Exception ex) { File.WriteAllText("Temp/HangarVehicles.done", ex.ToString()); Debug.LogException(ex); }
    }
}
