using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Story;
using XNode;

[InitializeOnLoad]
internal static class SchoolEmergencyWork
{
    static SchoolEmergencyWork() { EditorApplication.delayCall += Inspect; EditorApplication.delayCall += Detail; EditorApplication.delayCall += Repair; EditorApplication.delayCall += Build; }
    private static void Repair()
    {
        if(!File.Exists("Temp/school-emergency-build-error.txt") || File.Exists("Temp/school-emergency-built.txt"))return;
        try
        {
            foreach(var path in new[]{M+"CallAmbulanceMemory.asset",M+"AmbulanceDepartureMemory.asset","Assets/_Project/Configs/StoryTexts/School/PayphoneMissingCoin.asset","Assets/_Project/Configs/StoryTexts/School/CallAmbulanceReminder.asset"})
            {
                var g=Asset<NodeGraph>(path);
                foreach(var n in g.nodes.Where(n=>n!=null && (n is MemoryStartNode || n is StoryTextStartNode) && n.GetOutputPort("next").ConnectionCount==0).ToArray())
                {g.RemoveNode(n);UnityEngine.Object.DestroyImmediate(n,true);}
                EditorUtility.SetDirty(g);
            }
            foreach(var path in new[]{R+"InspectCoach.asset",R+"CallAmbulance.asset",R+"EnterAmbulance.asset",R+"Hints/CallAmbulanceReminder.asset"})
                if(!Asset<ReactionGraph>(path).TryGetSequence(out _,out var error))throw new Exception(path+": "+error);
            AssetDatabase.SaveAssets();
            var scene=SceneManager.GetSceneByPath("Assets/_Project/Locations/School/School.unity");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/school-emergency-built.txt","Repaired automatic duplicate start nodes. Validated graphs and saved scene.");
        }
        catch(Exception e){File.WriteAllText("Temp/school-emergency-repair-error.txt",e.ToString());}
    }
    private const string R = "Assets/_Project/Configs/Reactions/School/";
    private const string M = "Assets/_Project/Configs/Memories/School/";
    private static void Set(UnityEngine.Object obj, string field, object value)
    {
        var so=new SerializedObject(obj);var p=so.FindProperty(field);
        if(p==null) throw new Exception(obj.name+" missing "+field);
        if(value is string s)p.stringValue=s;
        else if(value is bool b)p.boolValue=b;
        else if(value is int i)p.intValue=i;
        else if(value is float f)p.floatValue=f;
        else if(value is Vector3 v)p.vector3Value=v;
        else p.objectReferenceValue=(UnityEngine.Object)value;
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(obj);
    }
    private static T Asset<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new Exception("Missing "+path);
    private static T New<T>(string path) where T:ScriptableObject
    {
        if(File.Exists(path))throw new Exception("Already exists "+path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var obj=ScriptableObject.CreateInstance<T>();obj.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(obj,path);return obj;
    }
    private static T Node<T>(NodeGraph graph) where T:Node
    {
        var n=graph.AddNode<T>();n.position=new Vector2((graph.nodes.Count-1)*350,0);AssetDatabase.AddObjectToAsset(n,graph);EditorUtility.SetDirty(graph);return n;
    }
    private static void Link(Node a,Node b,string port="next") {a.GetOutputPort(port).Connect(b.GetInputPort("previous"));EditorUtility.SetDirty(a);EditorUtility.SetDirty(b);}
    private static ReactionNode Action(ReactionGraph g,ReactionKind kind) {var a=Node<ReactionNode>(g);Set(a,"kind",(int)kind);return a;}
    private static ReactionGraph Graph(string path, bool once=false)
    {var g=New<ReactionGraph>(path);var start=Node<ReactionStartNode>(g);Set(start,"runOnce",once);return g;}
    private static void Keys(ReactionGraph g,params string[] keys)
    {var so=new SerializedObject(g);var a=so.FindProperty("objectKeys");a.arraySize=keys.Length;for(int i=0;i<keys.Length;i++)a.GetArrayElementAtIndex(i).stringValue=keys[i];so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Custom(ReactionInteractable owner,string key,CustomReaction reaction)
    {var so=new SerializedObject(owner);var a=so.FindProperty("customReactions");int n=a.arraySize;a.InsertArrayElementAtIndex(n);var p=a.GetArrayElementAtIndex(n);p.FindPropertyRelative("key").stringValue=key;p.FindPropertyRelative("reaction").objectReferenceValue=reaction;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Track(GameObject go,string id)
    {var t=go.GetComponent<ObjectStateTracker>() ?? go.AddComponent<ObjectStateTracker>();Set(t,"persistentId",id);}
    private static StoryTextGraph Text(string name,string text)
    {
        var g=New<StoryTextGraph>("Assets/_Project/Configs/StoryTexts/School/"+name+".asset");
        var s=Node<StoryTextStartNode>(g);var t=Node<StoryTextNode>(g);Set(t,"text",text);Set(t,"duration",4f);var e=Node<StoryTextEndNode>(g);Link(s,t);Link(t,e);return g;
    }
    private static MemoryGraph Memory(string name,string folder,int count)
    {
        var g=New<MemoryGraph>(M+name+".asset");Set(g,"displayTitle",name=="CallAmbulanceMemory"?"Вызов скорой":"Отъезд от школы");
        Node prev=Node<MemoryStartNode>(g);
        for(int i=1;i<=count;i++) {var n=Node<MemoryFrameNode>(g);Set(n,"image",Asset<Texture2D>($"Assets/_Project/Art/Memories/School/{folder}/{folder}-{i:00}.png"));Set(n,"text","");Set(n,"displayDuration",i==count?6f:4f);Link(prev,n);prev=n;}
        Link(prev,Node<MemoryEndNode>(g));return g;
    }
    private static void Build()
    {
        if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/school-emergency-build.request"))return;
        File.Delete("Temp/school-emergency-build.request");
        try
        {
            var scene=SceneManager.GetSceneByPath("Assets/_Project/Locations/School/School.unity");
            if(!scene.isLoaded || scene.isDirty)throw new Exception("School must be loaded and saved before building.");
            var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
            GameObject Find(string name)=>all.Single(t=>t.name==name).gameObject;
            var phone=Find("Настенный таксофон");var coin=Find("Монета в лотке сдачи");var car=Find("Ambulance-Truck");var doctor=Find("Doctor Gray School");
            var coach=Find("UrbanManV1 Gray Coach").GetComponent<ReactionInteractable>();
            var coinItem=Asset<QuestItem>("Assets/_Project/Configs/Инвентарные предметы/SchoolVendingCoin.asset");
            var call=Memory("CallAmbulanceMemory","CallAmbulanceMemory",3);
            var depart=Memory("AmbulanceDepartureMemory","AmbulanceDepartureMemory",1);
            var missing=Text("PayphoneMissingCoin","Где бы взять монетку?");
            var reminder=Text("CallAmbulanceReminder","Нужно вызвать “скорую.”");
            var hint=Graph(R+"Hints/CallAmbulanceReminder.asset");var hintText=Action(hint,ReactionKind.StoryText);Set(hintText,"textGraph",reminder);Link(hint.nodes[0],hintText);Link(hintText,Node<ReactionEndNode>(hint));
            var pg=Graph(R+"CallAmbulance.asset");Keys(pg,"arrive");
            var condition=Node<ReactionConditionNode>(pg);Set(condition,"item",coinItem);Link(pg.nodes[0],condition);
            var absent=Action(pg,ReactionKind.StoryText);Set(absent,"textGraph",missing);Link(condition,absent,"ifAbsent");
            var consume=Action(pg,ReactionKind.ConsumeInventoryItem);Set(consume,"item",coinItem);Link(condition,consume,"ifPresent");
            var memory=Action(pg,ReactionKind.Memory);Set(memory,"memoryGraph",call);Set(memory,"memoryId","school_call_ambulance");Set(memory,"memoryStartedReactionKey","arrive");Link(consume,memory);
            var pe=Node<ReactionEndNode>(pg);Link(absent,pe);Link(memory,pe);
            var dg=Graph(R+"EnterAmbulance.asset",true);Keys(dg,"depart");var disable=Action(dg,ReactionKind.ObjectInteractivity);Set(disable,"interactionEnabled",false);Link(dg.nodes[0],disable);
            var dm=Action(dg,ReactionKind.Memory);Set(dm,"memoryGraph",depart);Set(dm,"memoryId","school_ambulance_departure");Set(dm,"memoryStartedReactionKey","depart");Link(disable,dm);Link(dm,Node<ReactionEndNode>(dg));
            var pi=phone.AddComponent<ReactionInteractable>();pi.Configure(pg,"Позвонить");Set(pi,"initiallyInteractable",false);Set(pi,"initiallyVisible",false);Track(phone,"school.payphone");
            var pc=phone.AddComponent<BoxCollider>();pc.center=new Vector3(0,0,.15f);pc.size=new Vector3(.5f,.68f,.4f);
            var ci=coin.GetComponent<ReactionInteractable>();Set(ci,"initiallyInteractable",false);Set(ci,"initiallyVisible",false);Set(ci,"prompt","Взять монетку");
            var ai=car.AddComponent<ReactionInteractable>();ai.Configure(dg,"В машину");Set(ai,"initiallyInteractable",false);Set(ai,"initiallyVisible",false);Track(car,"school.ambulance");Track(doctor,"school.ambulance_doctor");
            var reveal=coach.gameObject.AddComponent<RevealSchoolPhoneReaction>();Set(reveal,"phone",pi);Set(reveal,"coin",ci);Set(reveal,"coinItem",coinItem);Custom(coach,"reveal_phone",reveal);
            var arrival=phone.AddComponent<SchoolAmbulanceArrivalReaction>();Set(arrival,"ambulance",ai);Set(arrival,"doctor",doctor);Set(arrival,"coach",coach);Custom(pi,"arrive",arrival);
            var hinges=new[]{Find("Открытая дверь во двор левая").transform.Find("Петля"),Find("Открытая дверь во двор правая").transform.Find("Петля")};
            var so=new SerializedObject(arrival);so.FindProperty("hinges").arraySize=2;so.FindProperty("openAngles").arraySize=2;
            for(int i=0;i<2;i++){so.FindProperty("hinges").GetArrayElementAtIndex(i).objectReferenceValue=hinges[i];so.FindProperty("openAngles").GetArrayElementAtIndex(i).vector3Value=hinges[i].localEulerAngles;hinges[i].localRotation=Quaternion.identity;}
            so.ApplyModifiedPropertiesWithoutUndo();
            var departure=car.AddComponent<SchoolAmbulanceDepartureReaction>();Set(departure,"doctor",doctor);Custom(ai,"depart",departure);
            var beacon=car.AddComponent<SchoolAmbulanceBeacon>();var bs=new SerializedObject(beacon);var lamps=bs.FindProperty("lamps");lamps.arraySize=2;
            var bounds=car.GetComponent<Renderer>().bounds;
            for(int i=0;i<2;i++){var l=new GameObject(i==0?"Blue emergency beacon":"Red emergency beacon").AddComponent<Light>();l.transform.SetParent(car.transform);l.transform.position=new Vector3(bounds.center.x+(i==0?-.4f:.4f),bounds.max.y+.08f,bounds.center.z);l.type=LightType.Point;l.color=i==0?new Color(.08f,.25f,1):Color.red;l.range=7;l.intensity=5;l.shadows=LightShadows.None;lamps.GetArrayElementAtIndex(i).objectReferenceValue=l;}
            bs.ApplyModifiedPropertiesWithoutUndo();
            var cg=Asset<ReactionGraph>(R+"InspectCoach.asset");
            var ret=cg.nodes.OfType<ReactionNode>().Single(n=>n.Kind==ReactionKind.CustomReaction && n.ObjectKey=="return_supplies");
            var end=ReactionGraph.Next(ret);ret.GetOutputPort("next").Disconnect(end.GetInputPort("previous"));
            Node previous=ret;
            foreach(var item in cg.nodes.OfType<ReactionConditionNode>().Where(n=>n.Kind==ReactionConditionKind.InventoryItem).Select(n=>n.Item).ToArray())
            {var a=Action(cg,ReactionKind.ConsumeInventoryItem);Set(a,"item",item);Link(previous,a);previous=a;}
            var stopCoach=Action(cg,ReactionKind.ObjectInteractivity);Set(stopCoach,"interactionEnabled",false);Link(previous,stopCoach);
            var fourth=Action(cg,ReactionKind.Memory);Set(fourth,"memoryGraph",Asset<MemoryGraph>(M+"CoachFourthMemory.asset"));Set(fourth,"memoryId","school_coach_fourth_memory");Set(fourth,"memoryStartedReactionKey","reveal_phone");Link(stopCoach,fourth);
            var timer=Action(cg,ReactionKind.StartHints);Set(timer,"hintId","school.call-ambulance");Set(timer,"hintGraph",hint);Set(timer,"hintTimeout",60f);Set(timer,"repeatHint",true);Link(fourth,timer);Link(timer,end);
            Keys(cg,"return_supplies","open_corridor","reveal_phone");
            phone.SetActive(false);coin.SetActive(false);car.SetActive(false);doctor.SetActive(false);
            foreach(var g in new[]{cg,pg,dg,hint})if(!g.TryGetSequence(out _,out var error))throw new Exception(g.name+": "+error);
            foreach(var g in new[]{call,depart,Asset<MemoryGraph>(M+"CoachFourthMemory.asset")})if(!g.TryGetSequence(out _,out var error))throw new Exception(g.name+": "+error);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/school-emergency-built.txt","Built and validated graphs, scene saved.");
        }
        catch(Exception e){File.WriteAllText("Temp/school-emergency-build-error.txt",e.ToString());Debug.LogException(e);}
    }
    private static void Detail()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/school-emergency-details.txt")) return;
        var scene = SceneManager.GetSceneByPath("Assets/_Project/Locations/School/School.unity");
        if (!scene.isLoaded) return;
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines("Temp/school-emergency-details.txt", all.Where(t=>PathOf(t).Contains("таксофон") || PathOf(t).Contains("дверь во двор")).Select(t=>$"{PathOf(t)} | local {t.localPosition} scale {t.localScale} rot {t.localEulerAngles}"));
        Render(all.Single(t=>t.name=="Настенный таксофон"), "Temp/school-phone.png", new Vector3(-2,0,0));
        Render(all.Single(t=>t.name=="Ambulance-Truck"), "Temp/school-ambulance.png", new Vector3(5,3,-7));
    }
    private static void Render(Transform target, string path, Vector3 offset)
    {
        var go = new GameObject("Temporary reference camera");
        var camera=go.AddComponent<Camera>();
        var bounds = new Bounds(target.position,Vector3.zero);
        foreach(var r in target.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        camera.transform.position=bounds.center+offset;
        camera.transform.LookAt(bounds.center);
        camera.nearClipPlane=.03f; camera.farClipPlane=150; camera.fieldOfView=45;
        var rt=new RenderTexture(1280,720,24); camera.targetTexture=rt; camera.Render();
        var old=RenderTexture.active; RenderTexture.active=rt;
        var tex=new Texture2D(1280,720,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
        File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=old;
        UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(rt);
    }
    private static void Inspect()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/school-emergency-inspection.txt")) return;
        var scene = SceneManager.GetSceneByPath("Assets/_Project/Locations/School/School.unity");
        if (!scene.isLoaded) return;
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines("Temp/school-emergency-inspection.txt", new[] { "Dirty=" + scene.isDirty }.Concat(all.Where(t => t.name.Contains("такс") || t.name.Contains("Тел") || t.name.Contains("двор") || t.name.Contains("Ambulance") || t.name.Contains("Doctor") || t.name.Contains("Монет") || t.name.Contains("Coach")).Select(t => $"{PathOf(t)} | {t.position} | local {t.localPosition} | rot {t.localEulerAngles} | active {t.gameObject.activeSelf} | {string.Join(",",t.GetComponents<Component>().Select(c=>c ? c.GetType().Name : "MISSING"))}")));
    }
    private static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
}
