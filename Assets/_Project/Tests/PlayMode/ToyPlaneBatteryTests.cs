using System.Collections;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class ToyPlaneBatteryTests
    {
        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [UnityTest]
        public IEnumerator PlaneGivesBatteryOnceAndRestoresStoppedPropeller() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Toy plane session test");
            root.SetActive(false);
            var app = root.AddComponent<AppLifetimeScope>();
            app.StartAutomatically = false;
            root.SetActive(true);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                await session.JumpToCheckpointAsync(TesterCheckpoint.HouseEntrance);
                var scene = session.CurrentLocationScene;
                var plane = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RemoveToyPlaneBatteryReaction>(true)).Single();
                var interaction = plane.GetComponent<ReactionInteractable>();
                var propeller = Field<Transform>(plane, "propeller");
                var battery = Field<QuestItem>(plane, "battery");
                var inventory = session.SessionScope.Container.Resolve<QuestInventory>();
                Assert.IsTrue(interaction.CanInteract);
                var rotation = propeller.localRotation;
                await UniTask.NextFrame();
                Assert.Greater(Quaternion.Angle(rotation, propeller.localRotation), .01f);

                interaction.Interact();
                Assert.IsTrue(inventory.Contains(battery.Id));
                Assert.IsFalse(plane.IsSpinning);
                Assert.IsFalse(interaction.CanInteract);
                Assert.IsTrue(plane.gameObject.activeInHierarchy);
                rotation = propeller.localRotation;
                interaction.Interact();
                await UniTask.DelayFrame(3);
                Assert.Less(Quaternion.Angle(rotation, propeller.localRotation), .001f);
                Assert.AreEqual(1, inventory.Items.Count);

                foreach (bool consumed in new[] { false, true })
                {
                    var restored = new QuestInventory();
                    if (consumed) restored.RestoreConsumed(battery.Id);
                    else restored.TryAdd(battery);
                    interaction.Initialize(null, null, inventory: restored);
                    plane.Initialize(restored);
                    Assert.IsFalse(plane.IsSpinning);
                    Assert.IsFalse(interaction.CanInteract);
                    Assert.IsTrue(plane.gameObject.activeInHierarchy);
                }

                inventory = new QuestInventory();
                interaction.Initialize(null, null, inventory: inventory);
                plane.Initialize(inventory);
                inventory.TryAdd(battery);
                await UniTask.DelayFrame(2);
                Assert.IsTrue(plane.IsSpinning, "Inventory changes must not implicitly stop the propeller.");
                plane.Execute();
                Assert.IsFalse(plane.IsSpinning);
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
