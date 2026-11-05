using System.Collections;using UnityEngine.TestTools;
﻿using System;using System.Linq;using System.Threading;using Cysharp.Threading.Tasks;using Memories.Gameplay.Interaction;using Memories.Gameplay.Reactions;using Memories.Gameplay.UI;using NUnit.Framework;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace Memories.Tests.EditMode{
public sealed class GarageHoodTransferTests{
private sealed class ComicInput : Memories.Contracts.IPauseInput, Memories.Contracts.IAdvanceInput
{
    public event Action PauseRequested;
    public event Action AdvanceRequested;
    public void SetPaused(bool paused) { }
    public void Advance() => AdvanceRequested?.Invoke();
}
private sealed class ComicView : Memories.Gameplay.Memories.IMemoryView
{
    public int Frames;
    public void Show() { }
    public void Hide() { }
    public UniTask ShowFrameAsync(Memories.Gameplay.Memories.MemoryFrameNode frame, CancellationToken token)
    { Frames++; return UniTask.CompletedTask; }
}
private sealed class ComicMusic : Memories.Gameplay.Memories.IMemoryMusic
{
    public void Execute(Memories.Gameplay.Memories.MemoryMusicNode node) { }
    public void StopImmediately() { }
}
private sealed class ComicSounds : Memories.Gameplay.Memories.IMemorySound
{
    public UniTask PlayAsync(Memories.Gameplay.Memories.MemorySoundNode node, CancellationToken token) => UniTask.CompletedTask;
    public void StopAll() { }
}


const string ScenePath="Assets/_Project/Locations/Garage/Garage.unity";
static GameObject Find(Scene s,string name)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==name).gameObject;
sealed class NoText:IStoryTextView { public CancellationToken LifetimeToken=>CancellationToken.None;public UniTask ShowAsync(string text,CancellationToken token)=>throw new AssertionException("Request should not repeat during transfer");public UniTask ChangeTextAsync(string text,CancellationToken token)=>UniTask.CompletedTask;public UniTask HideAsync(CancellationToken token)=>UniTask.CompletedTask;public void HideImmediately(){} }
[UnityTest] public IEnumerator NeitherItem()=>Transfer(false,false,false,true);
[UnityTest] public IEnumerator OnlyToolbox()=>Transfer(true,false,false,true);
[UnityTest] public IEnumerator OnlyHeadlamp()=>Transfer(false,true,false,true);
[UnityTest] public IEnumerator ToolboxThenHeadlamp()=>Transfer(true,true,false,true);
[UnityTest] public IEnumerator HeadlampThenToolbox()=>Transfer(true,true,true,true);
[UnityTest] public IEnumerator BothItemsFromCheckpoint()=>Transfer(true,true,false,false);
private IEnumerator Transfer(bool hasBox,bool hasLamp,bool reverseOrder,bool requested){
 var scene=EditorSceneManager.OpenPreviewScene(ScenePath);var key="Memories.Tests.HoodTransfer."+Guid.NewGuid().ToString("N");
 var input=new ComicInput();var view=new ComicView();var collection=new Memories.Gameplay.Memories.MemoryCollection(persistProgress:false);
 using var texts=new StoryTextPresenter(new NoText());
 using var memories=new Memories.Gameplay.Memories.MemoryPresenter(collection,new GameplayModeState(input),view,input,new ComicMusic(),new ComicSounds());
 try{
 var box=AssetDatabase.LoadAssetAtPath<QuestItem>("Assets/_Project/Configs/Инвентарные предметы/Гараж/Toolbox.asset");var lamp=AssetDatabase.LoadAssetAtPath<QuestItem>("Assets/_Project/Configs/Инвентарные предметы/Гараж/Headlamp.asset");var bank=AssetDatabase.LoadAssetAtPath<CustomVariableBank>("Assets/_Project/Configs/CustomVariables/GarageQuests.asset");var vars=new CustomVariableStore(key);vars.Set(bank,"father_hood_items_requested",requested);var inv=new QuestInventory();if(reverseOrder&&hasLamp)inv.TryAdd(lamp);if(hasBox)inv.TryAdd(box);if(!reverseOrder&&hasLamp)inv.TryAdd(lamp);
 var father=Find(scene,"Отец у капота").GetComponent<ReactionInteractable>();father.gameObject.SetActive(true);father.Initialize(texts,memories,inventory:inv,variables:vars);father.RestoreState("{\"active\":[\"garage_hood_items\"]}");father.Interact();yield return null;yield return null;bool complete=hasBox&&hasLamp;
 var working=Find(scene,"Отец у открытого капота");
 var hood=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SetCarHoodReaction>(true)).Single();
 Assert.AreEqual(complete,working.activeSelf);Assert.AreEqual(!complete,father.gameObject.activeSelf);Assert.AreEqual(complete,hood.IsOpen);
 Assert.IsFalse(working.GetComponent<Animator>().enabled);Assert.IsTrue(working.GetComponentInChildren<Light>(true).enabled);
 Assert.AreEqual(complete,memories.IsPlaying);Assert.AreEqual(complete?1:0,view.Frames);Assert.AreEqual(hasBox&&!complete,inv.Contains(box.Id));Assert.AreEqual(hasLamp&&!complete,inv.Contains(lamp.Id));Assert.AreEqual(complete,vars.Get(bank,"father_hood_items_delivered"));Assert.AreEqual(!complete,father.CaptureState().Contains("garage_hood_items"));
 if(complete){Assert.IsTrue(inv.IsConsumed(box.Id)&&inv.IsConsumed(lamp.Id));memories.Close();father.Initialize(texts,memories,inventory:inv,variables:new CustomVariableStore(key));father.Interact();yield return null;yield return null;Assert.IsFalse(memories.IsPlaying);Assert.AreEqual(1,view.Frames);foreach(var name in new[]{"toolbox","Налобный фонарь"}){var pickup=Find(scene,name).GetComponent<ReactionInteractable>();pickup.Initialize(texts,memories,inventory:inv,variables:vars);Assert.IsFalse(pickup.gameObject.activeSelf);}}
 }finally{EditorSceneManager.ClosePreviewScene(scene);PlayerPrefs.DeleteKey(key);}
}
[Test]public void ThirdMemoryHasSixConnectedReadableFrames(){var graph=AssetDatabase.LoadAssetAtPath<Memories.Gameplay.Memories.MemoryGraph>("Assets/_Project/Configs/Memories/Garage/GarageThirdMemory.asset");Assert.IsTrue(graph.TryGetSequence(out var nodes,out var error),error);var frames=nodes.OfType<Memories.Gameplay.Memories.MemoryFrameNode>().ToArray();Assert.AreEqual(6,frames.Length);Assert.IsTrue(frames[2].Text.Contains("За свою семью"));Assert.AreEqual("Ты: Я запомню.",frames[5].Text);}
private sealed class RecordingText:IStoryTextView {
 public string Last;public CancellationToken LifetimeToken=>CancellationToken.None;
 public UniTask ShowAsync(string text,CancellationToken token){Last=text;return UniTask.CompletedTask;}
 public UniTask ChangeTextAsync(string text,CancellationToken token){Last=text;return UniTask.CompletedTask;}
 public UniTask HideAsync(CancellationToken token)=>UniTask.CompletedTask;public void HideImmediately(){}
}
[UnityTest]public IEnumerator HiddenFatherKeepsComicAndCompletionTextAlive(){
 var scene=EditorSceneManager.OpenPreviewScene(ScenePath);var key="Memories.Tests.FourthFather."+Guid.NewGuid().ToString("N");
 var input=new ComicInput();var view=new ComicView();var collection=new Memories.Gameplay.Memories.MemoryCollection(persistProgress:false);var textView=new RecordingText();
 using var texts=new StoryTextPresenter(textView);
 using var memories=new Memories.Gameplay.Memories.MemoryPresenter(collection,new GameplayModeState(input),view,input,new ComicMusic(),new ComicSounds());
 try{
  var bank=AssetDatabase.LoadAssetAtPath<CustomVariableBank>("Assets/_Project/Configs/CustomVariables/GarageQuests.asset");var vars=new CustomVariableStore(key);var inv=new QuestInventory();
  foreach(var name in new[]{"Toolbox","Headlamp"})inv.TryAdd(AssetDatabase.LoadAssetAtPath<QuestItem>("Assets/_Project/Configs/Инвентарные предметы/Гараж/"+name+".asset"));
  var father=Find(scene,"Отец у капота").GetComponent<ReactionInteractable>();father.gameObject.SetActive(true);father.Initialize(texts,memories,inventory:inv,variables:vars);father.Interact();
  yield return null;Assert.IsFalse(father.gameObject.activeSelf);Assert.IsTrue(memories.IsPlaying);Assert.IsNull(textView.Last);
  double deadline=EditorApplication.timeSinceStartup+5;
  while(memories.IsPlaying&&EditorApplication.timeSinceStartup<deadline){input.Advance();EditorApplication.QueuePlayerLoopUpdate();yield return null;yield return null;}
  Assert.IsFalse(memories.IsPlaying,"Comic still playing after "+view.Frames+" frames");Assert.AreEqual(6,view.Frames);Assert.IsTrue(collection.Contains("garage_third_memory"));
  yield return null;yield return null;Assert.AreEqual("Точно! Меня зовут Алекс.",textView.Last);
  var working=Find(scene,"Отец у открытого капота");var hood=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SetCarHoodReaction>(true)).Single();
  working.SetActive(false);father.gameObject.SetActive(true);hood.SetOpen(false);
  var restore=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GarageOpenHoodFatherReaction>(true)).Single();restore.Restore(vars,collection);
  Assert.IsTrue(working.activeSelf);Assert.IsFalse(father.gameObject.activeSelf);Assert.IsTrue(hood.IsOpen);Assert.IsFalse(memories.IsPlaying);
 }finally{EditorSceneManager.ClosePreviewScene(scene);PlayerPrefs.DeleteKey(key);}
}
}}
