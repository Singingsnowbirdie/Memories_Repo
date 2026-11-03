using System.Collections;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Memories.Tests.PlayMode
{
 public sealed class ExplicitInteractableTests
 {
  static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
  [Test] public void OnlyGenericInteractableExistsInGameAssembly()
  {
   Assert.AreEqual(typeof(MonoBehaviour), typeof(ReactionInteractable).BaseType); Assert.IsTrue(typeof(ReactionInteractable).IsSealed); Assert.IsNull(typeof(ReactionInteractable).Assembly.GetType("Memories.Gameplay.Interaction.Interactable"));
  }
  [UnityTest] public IEnumerator GarageRunsItsCustomReaction()=>Check(false);
  [UnityTest] public IEnumerator ClassroomRunsItsCustomReaction()=>Check(true);
  static IEnumerator Check(bool school)=>UniTask.ToCoroutine(async()=>
  {
   string location=school?"School":"Garage";string path="Assets/_Project/Locations/"+location+"/"+location+".unity";
#if UNITY_EDITOR
   await UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Additive));
#else
   await SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
#endif
   try
   {
    var scene=SceneManager.GetSceneByPath(path);
    CustomReaction reaction=school?(CustomReaction)scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OpenClassroomDoorReaction>(true)).Single():scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OpenGarageGateReaction>(true)).Single();
    var interaction=reaction.GetComponent<ReactionInteractable>();Assert.IsNotNull(interaction);
    Assert.AreEqual(!school,interaction.enabled,"Preserve the scene's original enabled state.");interaction.enabled=true;
    interaction.Initialize(null,null);
    Assert.IsTrue(interaction.CanInteract);
    var graph=Field<ReactionGraph>(interaction,"graph");Assert.IsTrue(graph.TryGetSequence(out var actions,out var error),error);Assert.AreEqual(1,actions.Count);Assert.AreEqual(ReactionKind.CustomReaction,actions[0].Kind);
    var moving=Field<Transform>(reaction,school?"hinge":"panel");var initialRotation=moving.localRotation;var initialPosition=moving.localPosition;
    reaction.GetType().GetField("duration",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(reaction,.1f);
    await UniTask.DelayFrame(3);Assert.AreEqual(initialRotation,moving.localRotation);Assert.AreEqual(initialPosition,moving.localPosition);
    interaction.Interact();Assert.IsFalse(interaction.CanInteract,"One-shot graph must prevent duplicate interaction.");
    await UniTask.WaitUntil(()=>school?((OpenClassroomDoorReaction)reaction).IsOpen:((OpenGarageGateReaction)reaction).IsOpen).Timeout(System.TimeSpan.FromSeconds(5));
    var expected=initialRotation*(school?Quaternion.Euler(0,100,0):Quaternion.Euler(90,0,0));Assert.Less(Quaternion.Angle(expected,moving.localRotation),.1f);
    if(!school)Assert.Less(Vector3.Distance(Field<Vector3>(reaction,"raisedPosition"),moving.localPosition),.001f);
    reaction.Execute();interaction.Interact();await UniTask.DelayFrame(3);Assert.Less(Quaternion.Angle(expected,moving.localRotation),.1f);
   }
   finally {await SceneManager.UnloadSceneAsync(path);}
  });
 }
}
