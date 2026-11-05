using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
namespace Memories.Tests.PlayMode
{
 public sealed class GarageFatherMemoryTests
 {
  static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(o);
  [UnityTest] public IEnumerator InteractionSwitchesFatherWhileComicRemainsOpenAndRestoresCompletedPose()=>UniTask.ToCoroutine(async()=>{
   var root=new GameObject("Garage father test");root.SetActive(false);var app=root.AddComponent<AppLifetimeScope>();app.StartAutomatically=false;
#if UNITY_EDITOR
   app.UseOpenLocationForEditorPlay=true;
#endif
   root.SetActive(true);var session=app.Container.Resolve<GameSession>();
   try{
    await session.StartAsync(TestLocationLauncher.GaragePath);
    var reaction=session.CurrentLocationScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GarageFatherMemoryReaction>(true)).Single();
    var standing=Field<GameObject>(reaction,"standingFather");var kneeling=Field<GameObject>(reaction,"kneelingFather");var presenter=session.LocationScope.Container.Resolve<MemoryPresenter>();var collection=session.SessionScope.Container.Resolve<MemoryCollection>();
    Assert.IsTrue(standing.activeSelf);Assert.IsFalse(kneeling.activeSelf);
    standing.GetComponent<ReactionInteractable>().Interact();await UniTask.NextFrame();
    Assert.IsTrue(presenter.IsPlaying,"Hiding the interactable must not cancel the comic.");Assert.IsFalse(standing.activeSelf);Assert.IsTrue(kneeling.activeSelf);Assert.IsFalse(kneeling.GetComponent<Animator>().enabled);
    var bones=kneeling.GetComponentsInChildren<Transform>();var pose=bones.Select(b=>b.localRotation).ToArray();await UniTask.DelayFrame(3);for(int i=0;i<bones.Length;i++)Assert.Less(Quaternion.Angle(pose[i],bones[i].localRotation),.001f);
    foreach(var r in kneeling.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();r.BakeMesh(mesh);Assert.IsTrue(mesh.vertices.All(v=>r.bounds.Contains(r.transform.TransformPoint(v))),"Culling bounds must contain the pose.");Object.Destroy(mesh);}
    presenter.Close();reaction.Initialize(presenter,collection);Assert.IsTrue(standing.activeSelf);Assert.IsFalse(kneeling.activeSelf);
    collection.Collect(GarageFatherMemoryReaction.MemoryId);await UniTask.DelayFrame(2);Assert.IsTrue(standing.activeSelf,"Collection changes must not implicitly advance gameplay.");
    reaction.Initialize(presenter,collection);Assert.IsFalse(standing.activeSelf);Assert.IsTrue(kneeling.activeSelf);Assert.IsFalse(presenter.IsPlaying,"Restore must not replay the comic.");
   }finally{if(session.IsRunning)await session.EndAsync();Object.Destroy(root);await UniTask.NextFrame();}
  });
 }
}
