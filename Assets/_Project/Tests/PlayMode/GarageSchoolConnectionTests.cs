using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Player;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class GarageSchoolConnectionTests
    {
        private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();

        [UnityTest]
        public IEnumerator FinalMemoryPreloadsSchoolAndGateLeadsIntoCorridor() => CheckPassage(false);

        [UnityTest]
        public IEnumerator RestoredFinalMemoryPreparesPassageWithoutReplayingComic() => CheckPassage(true);

        [UnityTest]
        public IEnumerator GateWaitsForSlowSchoolLoad()
        {
            foreach (bool restore in new[] { false, true })
            {
                var owner = new GameObject("Gate readiness test");
                var panel = new GameObject("Panel").transform;
                panel.SetParent(owner.transform);
                var gate = owner.AddComponent<OpenGarageGateReaction>();
                typeof(OpenGarageGateReaction).GetField("panel", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).SetValue(gate, panel);
                typeof(OpenGarageGateReaction).GetField("duration", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).SetValue(gate, .1f);
                var ready = new UniTaskCompletionSource();
                gate.Initialize(() => ready.Task);
                try
                {
                    if (restore) gate.RestoreOpen(); else gate.Execute();
                    yield return new WaitForSeconds(.2f);
                    Assert.IsFalse(gate.IsOpen);
                    Assert.AreEqual(Vector3.zero, panel.localPosition);
                    ready.TrySetResult();
                    yield return new WaitForSeconds(.2f);
                    Assert.IsTrue(gate.IsOpen);
                }
                finally { Object.Destroy(owner); }
            }
        }

        [UnityTest]
        public IEnumerator DisposingDuringPreloadUnloadsBothScenes() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("School cancellation test");
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
                await session.StartAsync(TestLocationLauncher.GaragePath);
                Find<GarageSchoolConnection>(session.CurrentLocationScene).Execute();
                float deadline = Time.realtimeSinceStartup + 30;
                while (!session.IsTransitioning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(session.IsTransitioning);
                session.Dispose();
                while (session.IsRunning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                while ((SceneManager.GetSceneByPath(TestLocationLauncher.GaragePath).isLoaded ||
                    SceneManager.GetSceneByPath(TestLocationLauncher.SchoolPath).isLoaded) &&
                    Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsFalse(session.IsRunning);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.GaragePath).isLoaded);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.SchoolPath).isLoaded);
            }
            finally { session.Dispose(); Object.Destroy(root); await UniTask.NextFrame(); }
        });

        private IEnumerator CheckPassage(bool restore) => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("School connection test");
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
                await session.StartAsync(TestLocationLauncher.GaragePath);
                var garage = session.CurrentLocationScene;
                var scope = session.LocationScope;
                var motor = scope.Container.Resolve<PlayerMotor>();
                scope.Container.Resolve<PlayerPresenter>().ControlsEnabled = false;
                var connection = Find<GarageSchoolConnection>(garage);
                var gate = Find<OpenGarageGateReaction>(garage);
                var memories = scope.Container.Resolve<MemoryPresenter>();
                var inventory = scope.Container.Resolve<QuestInventory>();
                var final = Find<GarageFinalMemoryReaction>(garage);
                var items = (QuestItem[])typeof(GarageFinalMemoryReaction).GetField("deliveredItems",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(final);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.SchoolPath).isLoaded);
                var father = garage.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == "Отец у открытого капота").GetComponent<ReactionInteractable>();
                if (restore)
                {
                    var collection = scope.Container.Resolve<MemoryCollection>();
                    collection.Collect("garage_final_memory");
                    await UniTask.NextFrame();
                    Assert.IsFalse(connection.IsReady, "Progress changes must not trigger loading implicitly.");
                    final.Restore(inventory, collection);
                }
                else
                {
                    foreach (var item in items) inventory.TryAdd(item);
                    father.gameObject.SetActive(true);
                    father.Interact();
                }
                float deadline = Time.realtimeSinceStartup + 30;
                while (!connection.IsReady && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(connection.IsReady);
                Assert.AreEqual(!restore, memories.IsPlaying);
                if (!restore) Assert.IsFalse(gate.IsOpen, "Gate must stay closed during the memory.");
                Assert.AreEqual(garage, session.CurrentLocationScene);
                var school = SceneManager.GetSceneByPath(TestLocationLauncher.SchoolPath);
                var arrival = school.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == "Вход из гаража");
                Assert.Less(Vector3.Distance(arrival.position, connection.transform.position), .001f);
                Assert.Less(Quaternion.Angle(arrival.rotation, connection.transform.rotation), .001f);
                Assert.IsTrue(school.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).All(c => !c.enabled));
                if (!restore) memories.Close();
                deadline = Time.realtimeSinceStartup + 10;
                while (!gate.IsOpen && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(gate.IsOpen);
                var start = connection.transform.position - connection.transform.forward * .8f;
                var direction = connection.transform.forward;
                Assert.IsFalse(Physics.SphereCast(start + Vector3.up, .25f, direction, out var hit, 2f,
                    ~0, QueryTriggerInteraction.Ignore), "Blocked doorway: " + hit.collider +
                    (hit.collider == null ? "" : " scene=" + hit.collider.gameObject.scene.path +
                    " parent=" + hit.collider.transform.parent.name + " bounds=" + hit.collider.bounds) +
                    " start=" + start + " direction=" + direction);
                motor.Teleport(start + Vector3.up * .1f, connection.transform.eulerAngles.y);
                var controller = motor.GetComponent<CharacterController>();
                deadline = Time.realtimeSinceStartup + 8;
                while (session.CurrentLocationScene == garage && Time.realtimeSinceStartup < deadline)
                {
                    controller.Move(direction * (2f * Time.deltaTime) + Vector3.down * .01f);
                    await UniTask.NextFrame();
                }
                while (session.IsTransitioning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.AreEqual(school, session.CurrentLocationScene);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.GaragePath).isLoaded);
                Assert.AreSame(motor, session.LocationScope.Container.Resolve<PlayerMotor>());
                Assert.AreEqual(school, SceneManager.GetActiveScene());
            }
            finally
            {
                if (session.IsRunning) await session.EndAsync();
                Object.Destroy(root);
                await UniTask.NextFrame();
            }
        });
    }
}
