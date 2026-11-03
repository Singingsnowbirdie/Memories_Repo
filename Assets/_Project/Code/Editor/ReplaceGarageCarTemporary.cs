using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class ReplaceGarageCarTemporary
{
 static ReplaceGarageCarTemporary(){EditorApplication.delayCall+=Run;}
 static void Run(){
 if(EditorApplication.isPlayingOrWillChangePlaymode||File.Exists("Temp/garage-car-replaced.txt"))return;
 const string path="Assets/_Project/Locations/Garage/Garage.unity";
 var s=SceneManager.GetSceneByPath(path);bool opened=!s.IsValid()||!s.isLoaded;
 if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 var root=s.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="Гараж — мастерская");
 var old=root.GetComponentsInChildren<Transform>(true).Where(x=>PrefabUtility.IsAnyPrefabInstanceRoot(x.gameObject)&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(x.gameObject).EndsWith("/MuscleCarV4.prefab")).ToArray();
 if(old.Length!=1||root.Find("Caprice Estate")==null)throw new System.Exception("Expected old and new cars");
 Undo.DestroyObjectImmediate(old[0].gameObject);
 EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
 var cam=new GameObject("Temporary preview").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(cam.gameObject,s);cam.scene=s;
 cam.transform.position=root.TransformPoint(new Vector3(3.25f,2.25f,.55f));cam.transform.LookAt(root.TransformPoint(new Vector3(0,.8f,4.5f)));cam.fieldOfView=65;cam.nearClipPlane=.05f;
 var rt=new RenderTexture(1400,900,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
 var tex=new Texture2D(1400,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,900),0,0);tex.Apply();File.WriteAllBytes("Temp/Caprice-in-garage.png",tex.EncodeToPNG());
 RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(cam.gameObject);
 File.WriteAllText("Temp/garage-car-replaced.txt","Old MuscleCarV4 replaced by Caprice Estate. Saved.");
 }catch(System.Exception e){Debug.LogException(e);File.WriteAllText("Temp/garage-car-replace-error.txt",e.ToString());}
 finally{if(opened)EditorSceneManager.CloseScene(s,true);}
 }
}
