using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Player;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class BackyardGarageAttachmentTests
    {
        private static T Field<T>(object owner, string name) => (T)owner.GetType()
            .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(owner);
        [UnityTest]
        public IEnumerator CompletedMemoryAttachesGarageWithoutReplacingYardOrPlayer() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Garage attachment test");
            root.SetActive(false);
            var app = root.AddComponent<AppLifetimeScope>();
            app.StartAutomatically = false;
#if UNITY_EDITOR
            app.UseOpenLocationForEditorPlay = true;
#endif
            root.SetActive(true);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                await session.StartAsync(TestLocationLauncher.BackyardPath);
                var yard = session.CurrentLocationScene;
                var scope = session.LocationScope;
                var motor = scope.Container.Resolve<PlayerMotor>();
                scope.Container.Resolve<PlayerPresenter>().ControlsEnabled = false;
                var camera = motor.GetComponentInChildren<Camera>();
                var attachment = yard.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BackyardGarageAttachment>()).Single();
                var fence = yard.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Single(t => t.name == "Правый забор");
                var memories = session.SessionScope.Container.Resolve<MemoryCollection>();
                var seatedReaction = yard.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShowSeatedBenReaction>()).Single();
                var seated = Field<GameObject>(seatedReaction, "seatedBen");
                var pitcher = Field<GameObject>(seatedReaction, "pitcher");
                Assert.IsFalse(seated.activeSelf);
                pitcher.SetActive(true);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.GaragePath).isLoaded);
                memories.Collect("unrelated-memory");
                await UniTask.NextFrame();
                Assert.IsFalse(attachment.IsOpen);
                Assert.IsTrue(fence.Find("Секция 7").gameObject.activeSelf);
                var position = motor.transform.position;
                var area = yard.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RefreshBaseballAreaReaction>(true)).Single();
                var interactable = area.GetComponent<ReactionInteractable>();
                Assert.IsNotNull(interactable);
                Assert.IsFalse(interactable.CanInteract);
                var inventory = session.SessionScope.Container.Resolve<QuestInventory>();
                var bat = Field<QuestItem>(area, "bat");
                var intact = Field<GameObject>(area, "intactWindow");
                var broken = Field<GameObject>(area, "brokenWindow");
                inventory.TryAdd(bat);
                var variables = session.SessionScope.Container.Resolve<CustomVariableStore>();
                var bank = Field<CustomVariableBank>(area, "variableBank");
                variables.Set(bank, "ben_received_baseball", true);
                variables.Set(bank, "ben_received_cap", true);
                await UniTask.DelayFrame(3);
                Assert.IsFalse(area.IsReady, "Equipment changes must not be polled or subscribed to.");
                area.GetComponent<RefreshBaseballAreaReaction>().Execute();
                Assert.IsTrue(area.IsReady);
                Assert.IsTrue(interactable.CanInteract);
                memories.Collect(BackyardGarageAttachment.MemoryId);
                await UniTask.DelayFrame(3);
                Assert.IsFalse(attachment.IsOpen, "Collecting a memory must not bypass the custom reaction node.");
                Assert.IsTrue(inventory.Contains(bat.Id), "No implicit consumption in Update.");
                Assert.IsTrue(intact.activeSelf, "No implicit window replacement in Update.");
                Assert.IsFalse(broken.activeSelf);
                Assert.IsFalse(seated.activeSelf, "Collecting the memory must not implicitly switch Ben.");
                Assert.IsTrue(pitcher.activeSelf);
                var graph = Field<ReactionGraph>(interactable, "graph");
                Assert.IsTrue(graph.TryGetSequence(out var sequence, out var error), error);
                CollectionAssert.AreEqual(new[] { ReactionKind.Memory, ReactionKind.CustomReaction, ReactionKind.ConsumeInventoryItem, ReactionKind.CustomReaction,
                    ReactionKind.ObjectActivity, ReactionKind.ObjectActivity, ReactionKind.CustomReaction }, sequence.Select(n => n.Kind));
                interactable.Interact();
                var presenter = scope.Container.Resolve<MemoryPresenter>();
                await UniTask.WaitUntil(() => presenter.IsPlaying).Timeout(System.TimeSpan.FromSeconds(5));
                Assert.IsTrue(presenter.IsPlaying);
                // Finish the replay of the already-collected memory and let the real graph run its tail.
                presenter.Close();
                float deadline = Time.realtimeSinceStartup + 30;
                while (!attachment.IsOpen && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(attachment.IsOpen);
                Assert.IsFalse(pitcher.activeSelf);
                Assert.IsTrue(seated.activeSelf);
                Assert.IsNotNull(seated.GetComponent<Animator>().runtimeAnimatorController);
                var bones = seated.GetComponentsInChildren<Transform>();
                var pose = bones.Select(t => t.localRotation).ToArray();
                await UniTask.DelayFrame(3);
                for (int i = 0; i < bones.Length; i++) Assert.Less(Quaternion.Angle(pose[i], bones[i].localRotation), .001f);
                Assert.IsFalse(inventory.Contains(bat.Id));
                Assert.IsFalse(intact.activeSelf);
                Assert.IsTrue(broken.activeSelf);
                Assert.IsFalse(area.IsReady, "The explicit graph reaction must clear readiness after consuming the bat.");
                Assert.IsFalse(interactable.CanInteract);
                var garage = SceneManager.GetSceneByPath(TestLocationLauncher.GaragePath);
                Assert.IsTrue(garage.isLoaded);
                Assert.IsTrue(yard.isLoaded);
                Assert.AreEqual(yard, session.CurrentLocationScene);
                Assert.AreEqual(yard, SceneManager.GetActiveScene());
                Assert.AreSame(scope, session.LocationScope);
                Assert.AreSame(motor, scope.Container.Resolve<PlayerMotor>());
                Assert.AreSame(camera, motor.GetComponentInChildren<Camera>());
                Assert.Less(Vector3.Distance(position, motor.transform.position), .15f);
                Assert.AreEqual(root.scene, motor.gameObject.scene);
                Assert.IsTrue(garage.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).All(c => !c.enabled));
                for (int i = 7; i <= 10; i++) Assert.IsFalse(fence.Find("Секция " + i).gameObject.activeSelf);
                Assert.IsTrue(fence.Find("Секция 6").gameObject.activeSelf);
                var cc = motor.GetComponent<CharacterController>();
                cc.enabled = false;
                motor.transform.position = attachment.transform.position - attachment.transform.forward * 3f + Vector3.up * .05f;
                cc.enabled = true;
                Physics.SyncTransforms();
                deadline = Time.realtimeSinceStartup + 8;
                while (Vector3.Dot(motor.transform.position - attachment.transform.position, attachment.transform.forward) < 1.2f && Time.realtimeSinceStartup < deadline)
                {
                    cc.Move(attachment.transform.forward * (2f * Time.deltaTime) + Vector3.down * .01f);
                    await UniTask.NextFrame();
                }
                Assert.Greater(Vector3.Dot(motor.transform.position - attachment.transform.position, attachment.transform.forward), .8f,
                    "Side doorway must be walkable: " + motor.transform.position + "\n" + string.Join("\n", Physics.OverlapSphere(motor.transform.position + Vector3.up, 1.1f).Select(c => c.name + " " + c.bounds)));
                Assert.AreEqual(yard, session.CurrentLocationScene);
                Assert.IsTrue(yard.isLoaded);
                memories.Collect(BackyardGarageAttachment.MemoryId);
                attachment.Execute();
                await UniTask.NextFrame();
                Assert.AreEqual(1, Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Count(s => s.path == garage.path));
            }
            finally
            {
                if (session.IsRunning) await session.EndAsync();
                Object.Destroy(root);
                await UniTask.NextFrame();
            }
        });

        [UnityTest]
        public IEnumerator RestoredMemoryLoadsAttachmentOnlyOnce() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Restored attachment");
            try
            {
                var attachment = root.AddComponent<BackyardGarageAttachment>();
                var memories = new MemoryCollection();
                memories.Collect(BackyardGarageAttachment.MemoryId);
                int calls = 0;
                attachment.Initialize(memories, _ => { calls++; return UniTask.CompletedTask; });
                await UniTask.DelayFrame(3);
                Assert.IsTrue(attachment.IsOpen);
                attachment.Initialize(memories, _ => { calls++; return UniTask.CompletedTask; });
                await UniTask.DelayFrame(3);
                Assert.AreEqual(1, calls);
            }
            finally { Object.Destroy(root); }
        });
    }
}
