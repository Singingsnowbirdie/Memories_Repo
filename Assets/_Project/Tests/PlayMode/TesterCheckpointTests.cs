using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Player;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
namespace Memories.Tests.PlayMode
{
 public sealed class TesterCheckpointTests
 {
  static T Component<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).First();
  static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(value);
  static bool Active(Scene s,string path)=>TesterCheckpointCatalog.Find(s,path).gameObject.activeInHierarchy;
  [UnityTest] public IEnumerator CheckpointsRebuildWorldAndCanRewind()=>UniTask.ToCoroutine(async()=>
  {
   var root=new GameObject("Checkpoint test");root.SetActive(false);var app=root.AddComponent<AppLifetimeScope>();app.StartAutomatically=false;
   #if UNITY_EDITOR
   app.UseOpenLocationForEditorPlay=true;
#endif
   root.SetActive(true);var session=app.Container.Resolve<GameSession>();
   var before=PlayerPrefs.GetString(CustomVariableStore.DefaultKey,"missing");
   try
   {
    foreach(var checkpoint in new[]{TesterCheckpoint.HouseEntrance,TesterCheckpoint.HouseMemoryComplete,TesterCheckpoint.BackyardMemoryComplete,TesterCheckpoint.HouseEntrance})
    {
     await session.JumpToCheckpointAsync(checkpoint);await UniTask.DelayFrame(3);
     Assert.IsFalse(session.IsTransitioning);Assert.AreEqual(1,Time.timeScale);
     Assert.IsTrue(StoryTextView.ReticleUnlocked, "Checkpoint must unlock the reticle without waiting for story text.");
     var hud = Object.FindFirstObjectByType<InteractionHud>();
     Assert.IsNotNull(hud);
     Assert.IsTrue(hud.GetComponent<Canvas>().enabled, "Checkpoint must restore the interaction HUD.");
     Assert.IsTrue(hud.transform.Find("Reticle").GetComponent<UnityEngine.UI.Text>().enabled);
     var scene=session.CurrentLocationScene;var memories=session.SessionScope.Container.Resolve<MemoryCollection>();
     if(checkpoint==TesterCheckpoint.HouseEntrance)
     {
      Assert.AreEqual(TestLocationLauncher.HousePath,scene.path);
      Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.BackyardPath).isLoaded);
      Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.GaragePath).isLoaded);
      Assert.IsTrue(Active(scene,"Гостиная/controller_tv"));
      var clock = Component<RemoveClockBatteryReaction>(scene);
      Assert.IsTrue(clock.gameObject.activeInHierarchy);
      Assert.IsTrue(clock.IsRunning);
      Assert.IsTrue(clock.GetComponent<Memories.Gameplay.Interaction.ReactionInteractable>().CanInteract);
      var plane = Component<RemoveToyPlaneBatteryReaction>(scene);
      Assert.IsTrue(plane.gameObject.activeInHierarchy);
      Assert.IsTrue(plane.IsSpinning);
      Assert.IsTrue(plane.GetComponent<Memories.Gameplay.Interaction.ReactionInteractable>().CanInteract);
      Assert.IsTrue(Active(scene,"Гостиная/Prop_OldTV/TV Static Screen"));
      Assert.IsFalse(Active(scene,"Гостиная/Prop_OldTV/TV Baseball Screen"));
      Assert.IsFalse(Active(scene,"Гостиная/FEMALE Gray Seated"));
      Assert.IsFalse(Component<OpenCourtyardDoorReaction>(scene).IsOpen);
      Assert.IsFalse(Field<bool>(Component<RevealPhotoReaction>(scene),"revealed"));
      Assert.Greater(Quaternion.Angle(Quaternion.identity,Field<Transform>(Component<CloseStairDoorReaction>(scene),"hinge").localRotation),30);
      Assert.Greater(Quaternion.Angle(Quaternion.identity,Field<Transform>(Component<CloseEntranceDoorsReaction>(scene),"leftLeaf").localRotation),30);
      Assert.IsFalse(memories.Contains("house-tv-baseball"));
      Assert.Less(Vector3.Distance(new Vector3(0,.05f,305),session.LocationScope.Container.Resolve<PlayerMotor>().transform.position),.25f);
      var ui=root.GetComponent<TesterCheckpointUI>();ui.Toggle();Assert.IsTrue(ui.IsVisible);
      Assert.AreEqual(GameplayMode.Tester,session.LocationScope.Container.Resolve<GameplayModeState>().Mode);ui.Toggle();Assert.AreEqual(1,Time.timeScale);
     }
     else if(checkpoint==TesterCheckpoint.HouseMemoryComplete)
     {
      var plane = Component<RemoveToyPlaneBatteryReaction>(scene);
      Assert.IsTrue(plane.gameObject.activeInHierarchy);
      Assert.IsFalse(plane.IsSpinning);
      Assert.IsFalse(plane.GetComponent<Memories.Gameplay.Interaction.ReactionInteractable>().CanInteract);
      Assert.IsTrue(memories.Contains("house-tv-baseball"));Assert.IsTrue(Component<OpenCourtyardDoorReaction>(scene).IsOpen);
      Assert.IsTrue(Field<bool>(Component<RevealPhotoReaction>(scene),"revealed"));
      Assert.Less(Quaternion.Angle(Quaternion.identity,Field<Transform>(Component<CloseStairDoorReaction>(scene),"hinge").localRotation),.1f);
      Assert.IsTrue(SceneManager.GetSceneByPath(TestLocationLauncher.BackyardPath).isLoaded);
      var waitingEntry = Component<BackyardEntranceReaction>(SceneManager.GetSceneByPath(TestLocationLauncher.BackyardPath));
      Assert.IsTrue(Field<Light[]>(waitingEntry,"lights").All(l => !l.enabled));
      Assert.IsFalse(Active(scene,"Гостиная/controller_tv"));
      var clock = Component<RemoveClockBatteryReaction>(scene);
      Assert.IsTrue(clock.gameObject.activeInHierarchy);
      Assert.IsFalse(clock.IsRunning);
      Assert.IsFalse(clock.GetComponent<Memories.Gameplay.Interaction.ReactionInteractable>().CanInteract);
      Assert.IsFalse(Active(scene,"Гостиная/Prop_OldTV/TV Static Screen"));Assert.IsTrue(Active(scene,"Гостиная/Prop_OldTV/TV Baseball Screen"));
      Assert.IsTrue(Active(scene,"Гостиная/FEMALE Gray Seated"));Assert.IsTrue(Active(scene,"Гостиная/FEMALE Gray Seated/Remote in hand"));
     }
     else
     {
      Assert.IsFalse(Active(scene,"Бен в позе питчера"));
      Assert.IsTrue(Active(scene,"Бен под деревом"));
      var seated = TesterCheckpointCatalog.Find(scene,"Бен под деревом");
      Assert.IsNotNull(seated.GetComponent<Animator>().runtimeAnimatorController);
      foreach (string name in new[]{"Кепка на голове Бена","Бита рядом с Беном","Мяч рядом с Беном"})
       Assert.IsTrue(seated.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name).gameObject.activeInHierarchy);
      Assert.IsFalse(Active(scene,"Бен в айдл позе"));
      Assert.AreEqual(TestLocationLauncher.BackyardPath,scene.path);Assert.IsTrue(Component<BackyardGarageAttachment>(scene).IsOpen);
      var finishedEntry = Component<BackyardEntranceReaction>(scene);
      Assert.IsFalse(finishedEntry.IsRunning);
      Assert.IsTrue(Field<Light[]>(finishedEntry,"lights").All(l => l.enabled));
      Assert.IsTrue(SceneManager.GetSceneByPath(TestLocationLauncher.GaragePath).isLoaded);
      Assert.IsTrue(memories.Contains(BackyardGarageAttachment.MemoryId));Assert.IsTrue(memories.Contains("house-tv-baseball"));
      var area=Component<RefreshBaseballAreaReaction>(scene); Assert.IsFalse(Field<GameObject>(area,"intactWindow").activeSelf);
      Assert.IsFalse(area.GetComponent<Memories.Gameplay.Interaction.ReactionInteractable>().CanInteract);
      Assert.IsTrue(Active(scene,"Задний двор/Дом соседей — два этажа/Разбитое окно с занавесками"));
     }
     Assert.AreEqual(before,PlayerPrefs.GetString(CustomVariableStore.DefaultKey,"missing"));
    }
   }
   finally {if(session.IsRunning)await session.EndAsync();Object.Destroy(root);await UniTask.NextFrame();}
  });
 }
}


