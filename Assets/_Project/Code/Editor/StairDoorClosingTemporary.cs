using System;
using System.IO;
using System.Linq;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Memories;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class StairDoorClosingTemporary
{
    static StairDoorClosingTemporary() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists("Temp/stair-close-bind.request")) return;
        File.Delete("Temp/stair-close-bind.request");
        try { Apply(); }
        catch(Exception e) { File.WriteAllText("Temp/stair-close-bind.txt",e.ToString()); }
    }
    private static void Apply()
    {
        var scene=SceneManager.GetSceneByPath("Assets/_Project/Locations/House/House.unity");
        if(!scene.isLoaded) throw new Exception("House must be open.");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var door=all.Single(t=>t.name=="Дверь_Лестница (1)");
        var hinge=door.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Петли двери");
        var reaction=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CloseEntranceDoorsReaction>(true)).Single();
        var so=new SerializedObject(reaction);
        var extras=so.FindProperty("additionalLeaves");
        if(extras==null) throw new Exception("New code has not compiled yet.");
        EditorSceneManager.SaveScene(scene,"Temp/House-before-bind-stair-closing.unity",true);
        Undo.RecordObject(reaction,"Закрывать дверь у лестницы вместе со входной");
        if(!Enumerable.Range(0,extras.arraySize).Any(i=>extras.GetArrayElementAtIndex(i).objectReferenceValue==hinge))
        {int index=extras.arraySize++; extras.GetArrayElementAtIndex(index).objectReferenceValue=hinge;}
        so.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(reaction);
        var memoryId=so.FindProperty("completedMemoryId").stringValue;
        var testRoot=new GameObject("Door closing verification");
        try
        {
            testRoot.SetActive(false);
            var test=testRoot.AddComponent<CloseEntranceDoorsReaction>();
            var fields=new SerializedObject(test);
            var leaves=new Transform[3];
            var source=new[]{(Transform)so.FindProperty("leftLeaf").objectReferenceValue,(Transform)so.FindProperty("rightLeaf").objectReferenceValue,hinge};
            for(int i=0;i<3;i++) leaves[i]=UnityEngine.Object.Instantiate(source[i],testRoot.transform);
            fields.FindProperty("leftLeaf").objectReferenceValue=leaves[0];
            fields.FindProperty("rightLeaf").objectReferenceValue=leaves[1];
            fields.FindProperty("additionalLeaves").arraySize=1;
            fields.FindProperty("additionalLeaves").GetArrayElementAtIndex(0).objectReferenceValue=leaves[2];
            fields.FindProperty("completedMemoryId").stringValue=memoryId;
            fields.ApplyModifiedPropertiesWithoutUndo();
            void Open(){foreach(var t in leaves){t.localRotation=Quaternion.Euler(0,75,0);foreach(var c in t.GetComponentsInChildren<Collider>(true)){c.enabled=false;c.isTrigger=true;}}}
            void Check(){foreach(var t in leaves){if(Quaternion.Angle(t.localRotation,Quaternion.identity)>.001f)throw new Exception("Leaf did not close.");foreach(var c in t.GetComponentsInChildren<Collider>(true))if(!c.enabled||c.isTrigger)throw new Exception("Collider not blocking.");}}
            Open(); test.Restore(new MemoryCollection());
            if(Quaternion.Angle(leaves[2].localRotation,Quaternion.Euler(0,75,0))>.001f)throw new Exception("Closed before memory.");
            test.Execute(); Check(); test.Execute(); Check();
            Open(); var memories=new MemoryCollection(); memories.Collect(memoryId);test.Restore(memories);Check();
        }
        finally{UnityEngine.Object.DestroyImmediate(testRoot);}
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Temp/stair-close-bind.txt","OK. Bound "+door.name+" / "+hinge.name+". Memory="+memoryId+". Execute, repeated Execute, Restore without/with memory and blocking colliders verified on copies. Original pose preserved: "+hinge.localEulerAngles);
    }
}
