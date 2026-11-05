#if UNITY_EDITOR
using System.Collections;using UnityEngine.TestTools;
using System;using System.Linq;using System.Threading;using Cysharp.Threading.Tasks;using Memories.Gameplay.Interaction;using Memories.Gameplay.Reactions;using Memories.Gameplay.UI;using NUnit.Framework;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace Memories.Tests.PlayMode{
public sealed class GarageFinalTransferTests{
static GameObject Find(Scene s,string n)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==n).gameObject;
sealed class TextView:IStoryTextView{public CancellationToken LifetimeToken=>CancellationToken.None;public UniTask ShowAsync(string t,CancellationToken c)=>UniTask.CompletedTask;public UniTask ChangeTextAsync(string t,CancellationToken c)=>UniTask.CompletedTask;public UniTask HideAsync(CancellationToken c)=>UniTask.CompletedTask;public void HideImmediately(){}}
[UnityTest]public IEnumerator TransferAndGateTiming(){
foreach(var order in new[]{new[]{0},new[]{1},new[]{2},new[]{0,1},new[]{0,2},new[]{1,2},new int[0],new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}}){
var scene=EditorSceneManager.OpenPreviewScene("Assets/_Project/Locations/Garage/Garage.unity");var input=new ComicInput();var view=new ComicView();var collection=new Memories.Gameplay.Memories.MemoryCollection(persistProgress:false);using var texts=new StoryTextPresenter(new TextView());using var memories=new Memories.Gameplay.Memories.MemoryPresenter(collection,new GameplayModeState(input),view,input,new ComicMusic(),new ComicSounds());try{
var items=new[]{"VacuumHose","SiliconeSpray","PaperWipes"}.Select(n=>AssetDatabase.LoadAssetAtPath<QuestItem>("Assets/_Project/Configs/Инвентарные предметы/Гараж/"+n+".asset")).ToArray();var inv=new QuestInventory();foreach(int i in order)inv.TryAdd(items[i]);var father=Find(scene,"Отец у открытого капота").GetComponent<ReactionInteractable>();father.gameObject.SetActive(true);var fifth=Find(scene,"Отец у машины — пятый");Assert.IsNull(fifth.GetComponent<ReactionInteractable>());Assert.IsFalse(fifth.GetComponent<Animator>().enabled);
var gate=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OpenGarageGateReaction>(true)).Single();var so=new SerializedObject(gate);var panel=(Transform)so.FindProperty("panel").objectReferenceValue;var closed=panel.localPosition;so.FindProperty("duration").floatValue=.1f;so.ApplyModifiedPropertiesWithoutUndo();father.Initialize(texts,memories,inventory:inv);father.RestoreState("{\"active\":[\"garage_hose_silicone_wipes\"]}");father.Interact();yield return null;
if(order.Length<3){Assert.IsFalse(memories.IsPlaying);Assert.AreEqual(order.Length,inv.Items.Count);Assert.IsFalse(items.Any(i=>inv.IsConsumed(i.Id)));Assert.IsFalse(fifth.activeSelf);continue;}
Assert.IsTrue(memories.IsPlaying);Assert.IsFalse(father.gameObject.activeSelf);Assert.IsTrue(fifth.activeSelf);Assert.IsEmpty(inv.Items);Assert.IsTrue(items.All(i=>inv.IsConsumed(i.Id)));Assert.IsFalse(father.CaptureState().Contains("garage_hose_silicone_wipes"));Assert.AreEqual(closed,panel.localPosition);Assert.IsFalse(gate.IsOpen);
float deadline=Time.realtimeSinceStartup+5;while(memories.IsPlaying&&Time.realtimeSinceStartup<deadline){input.Advance();yield return null;yield return null;}Assert.IsFalse(memories.IsPlaying);Assert.AreEqual(5,view.Frames);Assert.IsTrue(collection.Contains("garage_final_memory"));yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(gate.IsOpen);Assert.AreNotEqual(closed,panel.localPosition);
var restore=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GarageFinalMemoryReaction>(true)).Single();fifth.SetActive(false);father.gameObject.SetActive(true);restore.Restore(inv,new Memories.Gameplay.Memories.MemoryCollection(persistProgress:false));Assert.IsTrue(fifth.activeSelf);Assert.IsFalse(father.gameObject.activeSelf);Assert.IsTrue(gate.IsOpen);Assert.IsFalse(memories.IsPlaying);
}finally{EditorSceneManager.ClosePreviewScene(scene);}}
}
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



}}
#endif
