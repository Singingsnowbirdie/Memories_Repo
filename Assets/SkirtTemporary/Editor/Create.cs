using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEngine;
[InitializeOnLoad]public static class SkirtCreate{
static SkirtCreate(){EditorApplication.delayCall+=Run;}
static void Run(){if(File.Exists("Temp/skirt-result-v2.txt"))return;GameObject root=null;PreviewRenderUtility preview=null;
try{
const string path="Assets/_Project/Prefabs/Characters/casual_Female_K_Gray_Seated.prefab";root=PrefabUtility.LoadPrefabContents(path);
var centers=new[]{new Vector3(0,.655f,-.017f),new Vector3(0,.49f,.055f),new Vector3(0,.49f,.23f),new Vector3(0,.49f,.41f)};
float[] widths={.153f,.225f,.24f,.245f},ys={0,.12f,.135f,.14f},zs={.112f,.075f,.025f,0};
var rings=new List<Vector3>();const int n=12;
for(int r=0;r<4;r++)for(int i=0;i<n;i++){float a=2*Mathf.PI*i/n;rings.Add(centers[r]+new Vector3(widths[r]*Mathf.Cos(a),ys[r]*Mathf.Sin(a),zs[r]*Mathf.Sin(a)));}
var v=new List<Vector3>();var tris=new List<int>();
for(int r=0;r<3;r++)for(int i=0;i<n;i++){int a=r*n+i,b=r*n+(i+1)%n,c=(r+1)*n+i,d=(r+1)*n+(i+1)%n;foreach(int k in new[]{a,b,c,b,d,c}){tris.Add(v.Count);v.Add(rings[k]);}}
var mesh=new Mesh{name="SeatedWoman_Skirt"};mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
var existing=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Prefabs/Characters/SeatedWoman_Skirt.asset");EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);UnityEngine.Object.DestroyImmediate(root.transform.Find("Skirt").gameObject);
var skirt=new GameObject("Skirt");skirt.transform.SetParent(root.transform,false);skirt.AddComponent<MeshFilter>().sharedMesh=mesh;skirt.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("c8080d9684a934c478944eedc8fbd9ae"));
PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);root=null;AssetDatabase.SaveAssets();
var sofa=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="3SeaterSofa"&&t.gameObject.scene.path.EndsWith("House.unity"));
preview=new PreviewRenderUtility();var sp=UnityEngine.Object.Instantiate(sofa.gameObject);sp.transform.position=Vector3.zero;sp.transform.rotation=Quaternion.identity;preview.AddSingleGO(sp);
var wp=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));wp.transform.position=new Vector3(0,0,-.12f);wp.transform.rotation=Quaternion.Euler(0,180,0);preview.AddSingleGO(wp);
preview.camera.transform.position=new Vector3(2,1.5f,-2.8f);preview.camera.transform.LookAt(new Vector3(0,.65f,0));preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=20;preview.camera.fieldOfView=40;preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=Quaternion.Euler(40,140,0);preview.lights[1].intensity=1;preview.ambientColor=new Color(.4f,.4f,.4f);
preview.BeginStaticPreview(new Rect(0,0,1000,800));preview.Render(true);var image=preview.EndStaticPreview();File.WriteAllBytes("Temp/skirt-preview.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);File.WriteAllText("Temp/skirt-result-v2.txt","OK");
}catch(Exception e){File.WriteAllText("Temp/skirt-result-v2.txt",e.ToString());}finally{if(root!=null)PrefabUtility.UnloadPrefabContents(root);if(preview!=null)preview.Cleanup();}}}
