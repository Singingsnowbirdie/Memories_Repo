using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Editor-only, scoped to this model. Never modifies a scene or existing prefab.
[InitializeOnLoad]
internal static class GrandTouringCoupeSetup
{
    private const string Root = "Assets/_Project/Models/Vehicles/GrandTouringCoupe";
    static GrandTouringCoupeSetup() { EditorApplication.delayCall += Create; }

    [MenuItem("Tools/Memories/Create Grand Touring Coupe Prefab")]
    private static void Create()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Create; return; }
        if (File.Exists(Root + "/GrandTouringCoupe.prefab")) return;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/GrandTouringCoupe.obj");
        if (!model) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) { Debug.LogError("Coupe: URP Lit shader missing"); return; }
        Directory.CreateDirectory(Root + "/Materials");
        var instance = Object.Instantiate(model);
        instance.name = "GrandTouringCoupe";
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var name = mats[i] ? mats[i].name.Replace(" (Instance)", "") : "Paint";
                var path = Root + "/Materials/" + name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat)
                {
                    mat = new Material(shader) { name = name };
                    Color color = name switch
                    {
                        "Paint" => new Color(.64f,.48f,.30f).gamma,
                        "Chrome" => new Color(.8f,.84f,.87f),
                        "Rubber" => new Color(.035f,.038f,.041f),
                        "Dark" => new Color(.055f,.06f,.065f),
                        "Glass" => new Color(.25f,.43f,.47f,.32f),
                        "Leather" => new Color(.24f,.025f,.045f),
                        "Headlamp" => new Color(.72f,.85f,.88f),
                        "Amber" => new Color(1f,.25f,.015f),
                        "Red" => new Color(.65f,.015f,.025f),
                        _ => new Color(.85f,.85f,.77f)
                    };
                    mat.SetColor("_BaseColor",color);
                    mat.SetFloat("_Metallic",0f);
                    mat.SetFloat("_Smoothness",0f);
                    mat.SetFloat("_EnvironmentReflections",0f);
                    mat.SetFloat("_SpecularHighlights",0f);
                    mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                    mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    mat.SetFloat("_ReceiveShadows",0f);
                    mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                    // Both sides for thin glazing and stamped trim surfaces.
                    mat.SetFloat("_Cull",0);
                    if (name == "Glass")
                    {
                        mat.SetFloat("_Surface",1); mat.SetFloat("_ZWrite",0);
                        mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                        mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.SetOverrideTag("RenderType","Transparent");
                        mat.renderQueue = (int)RenderQueue.Transparent;
                    }
                    if (name != "Glass" && name != "Paint")
                        mat.shader = Shader.Find("Memories/Vehicles/Matte Faceted");
                    AssetDatabase.CreateAsset(mat,path);
                }
                mats[i]=mat;
            }
            renderer.sharedMaterials=mats;
        }
        var collider = instance.AddComponent<BoxCollider>();
        collider.center = new Vector3(0,.75f,0); collider.size = new Vector3(1.85f,1.5f,5.15f);
        PrefabUtility.SaveAsPrefabAsset(instance,Root + "/GrandTouringCoupe.prefab");
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log("GrandTouringCoupe: static prefab and URP materials created.");
    }
}
