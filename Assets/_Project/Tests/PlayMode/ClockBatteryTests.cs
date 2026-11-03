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
    public sealed class ClockBatteryTests
    {
        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [UnityTest]
        public IEnumerator ClockHandsRunOppositeAndStopAfterBatteryPickup() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Clock session test");
            root.SetActive(false);
            var app = root.AddComponent<AppLifetimeScope>();
            app.StartAutomatically = false;
            root.SetActive(true);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                await session.JumpToCheckpointAsync(TesterCheckpoint.HouseEntrance);
                var scene = session.CurrentLocationScene;
                var clock = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RemoveClockBatteryReaction>(true)).Single();
                var interaction = clock.GetComponent<ReactionInteractable>();
                var minuteHand = Field<Transform>(clock, "minuteHand");
                var hourHand = Field<Transform>(clock, "hourHand");
                var battery = Field<QuestItem>(clock, "battery");
                var inventory = session.SessionScope.Container.Resolve<QuestInventory>();
                Assert.IsTrue(interaction.CanInteract);
                var rotation = minuteHand.localRotation;
                var hourRotation = hourHand.localRotation;
                await UniTask.NextFrame();
                Assert.Greater(Quaternion.Angle(rotation, minuteHand.localRotation), .01f);
                Assert.Greater(Quaternion.Angle(hourRotation, hourHand.localRotation), .01f);
                Assert.Less(Mathf.DeltaAngle(rotation.eulerAngles.z, minuteHand.localEulerAngles.z) *
                    Mathf.DeltaAngle(hourRotation.eulerAngles.z, hourHand.localEulerAngles.z), 0f,
                    "Clock hands must visibly rotate in opposite directions.");

                interaction.Interact();
                Assert.IsTrue(inventory.Contains(battery.Id));
                Assert.IsFalse(clock.IsRunning);
                Assert.IsFalse(interaction.CanInteract);
                Assert.IsTrue(clock.gameObject.activeInHierarchy);
                rotation = minuteHand.localRotation;
                hourRotation = hourHand.localRotation;
                interaction.Interact();
                await UniTask.DelayFrame(3);
                Assert.Less(Quaternion.Angle(rotation, minuteHand.localRotation), .001f);
                Assert.Less(Quaternion.Angle(hourRotation, hourHand.localRotation), .001f);
                Assert.AreEqual(1, inventory.Items.Count);

                foreach (bool consumed in new[] { false, true })
                {
                    var restored = new QuestInventory();
                    if (consumed) restored.RestoreConsumed(battery.Id);
                    else restored.TryAdd(battery);
                    interaction.Initialize(null, null, inventory: restored);
                    clock.Initialize(restored);
                    Assert.IsFalse(clock.IsRunning);
                    Assert.IsFalse(interaction.CanInteract);
                    Assert.IsTrue(clock.gameObject.activeInHierarchy);
                }

                inventory = new QuestInventory();
                interaction.Initialize(null, null, inventory: inventory);
                clock.Initialize(inventory);
                inventory.TryAdd(battery);
                await UniTask.DelayFrame(2);
                Assert.IsTrue(clock.IsRunning, "Inventory changes must not implicitly stop the hands.");
                clock.Execute();
                Assert.IsFalse(clock.IsRunning);
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
