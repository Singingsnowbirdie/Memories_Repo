using System;using System.Linq;using System.Threading;using Cysharp.Threading.Tasks;using Memories.Gameplay.Interaction;using Memories.Gameplay.Reactions;using Memories.Gameplay.UI;using NUnit.Framework;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace Memories.Tests.EditMode{
public sealed class GarageCompressorTransferTests{
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

static Memories.Gameplay.Memories.MemoryPresenter CreateMemories() { var input = new ComicInput(); return new Memories.Gameplay.Memories.MemoryPresenter(new Memories.Gameplay.Memories.MemoryCollection(persistProgress:false), new GameplayModeState(input), new ComicView(), input, new ComicMusic(), new ComicSounds()); }

const string Path="Assets/_Project/Locations/Garage/Garage.unity";
static GameObject Find(Scene s,string name)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==name).gameObject;
sealed class NoText:IStoryTextView{public CancellationToken LifetimeToken=>CancellationToken.None;public UniTask ShowAsync(string text,CancellationToken t)=>throw new AssertionException("Transfer must not replay the request.");public UniTask ChangeTextAsync(string text,CancellationToken t)=>UniTask.CompletedTask;public UniTask HideAsync(CancellationToken t)=>UniTask.CompletedTask;public void HideImmediately(){}}
[TestCase(false)][TestCase(true)]public void TransferConsumesItemStopsHintAndSurvivesReload(bool requested){var scene=EditorSceneManager.OpenPreviewScene(Path);string key="Memories.Tests.Compressor."+Guid.NewGuid().ToString("N");var item=AssetDatabase.LoadAssetAtPath<QuestItem>("Assets/_Project/Configs/Инвентарные предметы/Гараж/Compressor.asset");var bank=AssetDatabase.LoadAssetAtPath<CustomVariableBank>("Assets/_Project/Configs/CustomVariables/GarageQuests.asset");var variables=new CustomVariableStore(persistProgress:false);variables.Set(bank,"father_compressor_requested",requested);using var texts=new StoryTextPresenter(new NoText());using var memories=CreateMemories();try{var inv=new QuestInventory();inv.TryAdd(item);var father=Find(scene,"Отец у заднего левого колеса").GetComponent<ReactionInteractable>();father.gameObject.SetActive(true);scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true)).Single().Initialize(memories);father.Initialize(texts,memories,inventory:inv,variables:variables);var delivered=Find(scene,"Компрессор у заднего левого колеса");Assert.IsFalse(delivered.activeSelf);Assert.IsNull(delivered.GetComponent<ReactionInteractable>());father.RestoreState("{\"active\":[\"garage_compressor\"]}");using(var save=new ObjectStateStore(saveKey:key)){save.Register(delivered,"delivered");save.Register(father.gameObject,"father");father.Interact();Assert.IsFalse(inv.Contains(item.Id));Assert.IsTrue(inv.IsConsumed(item.Id));Assert.IsTrue(delivered.activeSelf);Assert.IsFalse(father.CaptureState().Contains("garage_compressor"));Assert.IsTrue(variables.Get(bank,"father_compressor_requested"));father.Interact();Assert.IsTrue(delivered.activeSelf);save.Flush();}memories.Close();EditorSceneManager.ClosePreviewScene(scene);scene=EditorSceneManager.OpenPreviewScene(Path);var loadedInv=new QuestInventory();loadedInv.RestoreConsumed(item.Id);father=Find(scene,"Отец у заднего левого колеса").GetComponent<ReactionInteractable>();scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true)).Single().Initialize(memories);father.Initialize(texts,memories,inventory:loadedInv,variables:variables);delivered=Find(scene,"Компрессор у заднего левого колеса");using(var save=new ObjectStateStore(saveKey:key)){save.Register(delivered,"delivered");save.Register(father.gameObject,"father");Assert.IsTrue(delivered.activeSelf);Assert.IsFalse(father.CaptureState().Contains("garage_compressor"));var pickup=Find(scene,"Компрессор");pickup.GetComponent<ReactionInteractable>().Initialize(texts,null,inventory:loadedInv,variables:variables);Assert.IsFalse(pickup.activeSelf);}}finally{if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);PlayerPrefs.DeleteKey(key);}}
[Test]public void WithoutCompressorNoTransfer(){var scene=EditorSceneManager.OpenPreviewScene(Path);using var texts=new StoryTextPresenter(new NoText());using var memories=CreateMemories();try{var vars=new CustomVariableStore(persistProgress:false);var bank=AssetDatabase.LoadAssetAtPath<CustomVariableBank>("Assets/_Project/Configs/CustomVariables/GarageQuests.asset");vars.Set(bank,"father_compressor_requested",true);var father=Find(scene,"Отец у заднего левого колеса").GetComponent<ReactionInteractable>();father.gameObject.SetActive(true);scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true)).Single().Initialize(memories);father.Initialize(texts,memories,inventory:new QuestInventory(),variables:vars);father.Interact();Assert.IsFalse(Find(scene,"Компрессор у заднего левого колеса").activeSelf);}finally{EditorSceneManager.ClosePreviewScene(scene);}}
}}
