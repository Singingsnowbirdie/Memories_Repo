using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class LilyMannequinTemporary {
 static LilyMannequinTemporary(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(File.Exists("Temp/LilyMannequin.done"))return;
  GameObject root=null;
  try {
   root=PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/Characters/Lily Gray.prefab");
   root.name="Lily Gray T-Pose"; root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   foreach(var a in root.GetComponentsInChildren<Animator>(true)){a.runtimeAnimatorController=null;a.enabled=false;}
   var poses=new Dictionary<Transform,Matrix4x4>();
   foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) for(int i=0;i<r.bones.Length;i++)if(r.bones[i]!=null)poses[r.bones[i]]=r.transform.localToWorldMatrix*r.sharedMesh.bindposes[i].inverse;
   foreach(var p in poses.OrderBy(p=>Depth(p.Key))) {var m=p.Key.parent.worldToLocalMatrix*p.Value;p.Key.localPosition=m.GetColumn(3);p.Key.localRotation=m.rotation;p.Key.localScale=m.lossyScale;}
   foreach(var side in new[]{"L","R"}){
    var bones=root.GetComponentsInChildren<Transform>();
    var upper=bones.Single(t=>t.name=="bip "+side+" UpperArm");
    var fore=bones.Single(t=>t.name=="bip "+side+" Forearm");
    var hand=bones.Single(t=>t.name=="bip "+side+" Hand");
    var dir=upper.position.x>0?Vector3.right:Vector3.left;
    upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,dir)*upper.rotation;
    fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,dir)*fore.rotation;
   }
   PrefabUtility.SaveAsPrefabAsset(root,"Assets/_Project/Prefabs/Characters/Lily Gray T-Pose.prefab");
   var display=UnityEngine.Object.Instantiate(root); PrefabUtility.UnloadPrefabContents(root); root=null; var preview=new PreviewRenderUtility();
   try {preview.AddSingleGO(display);preview.camera.transform.position=new Vector3(0,1,-4);preview.camera.transform.LookAt(new Vector3(0,1,0));preview.camera.fieldOfView=35;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=30;preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(30,-20,0);preview.lights[1].intensity=.7f;preview.ambientColor=Color.gray;preview.BeginStaticPreview(new Rect(0,0,900,900));preview.camera.Render();var tex=preview.EndStaticPreview();File.WriteAllBytes("Temp/LilyMannequin.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}finally{preview.Cleanup();root=null;}
   File.WriteAllText("Temp/LilyMannequin.done","Created T-pose mannequin prefab. Animation disabled.");
  }catch(Exception e){File.WriteAllText("Temp/LilyMannequin.error",e.ToString());Debug.LogException(e);}finally{if(root)PrefabUtility.UnloadPrefabContents(root);}
 }
 static int Depth(Transform t){int d=0;while(t.parent){t=t.parent;d++;}return d;}
}
