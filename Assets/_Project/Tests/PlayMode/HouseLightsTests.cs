using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class HouseLightsTests
    {
        private static Light[] AuthoredLights(HouseLights house)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var lights = (Light[])typeof(HouseLights).GetField("lights", flags).GetValue(house);
            var enabled = (bool[])typeof(HouseLights).GetField("initiallyEnabled", flags).GetValue(house);
            return lights.Where((light, index) => enabled[index]).ToArray();
        }
        [UnityTest]
        public IEnumerator HouseFadesInAfterTheLastHighwayLamp() => UniTask.ToCoroutine(async () =>
        {
            float previousTimeScale = Time.timeScale;
            try
            {
                await SceneManager.LoadSceneAsync(TestLocationLauncher.HighwayPath, LoadSceneMode.Additive);
                await SceneManager.LoadSceneAsync(TestLocationLauncher.HousePath, LoadSceneMode.Additive);
                var highway = Object.FindFirstObjectByType<HighwayLights>();
                var house = Object.FindFirstObjectByType<HouseLights>();
                Assert.IsNotNull(highway);
                Assert.IsNotNull(house);
                var lights = AuthoredLights(house);
                Assert.IsNotEmpty(lights);
                Assert.IsTrue(lights.Any(light => !light.transform.IsChildOf(house.transform)),
                    "Test must include lights on separate house scene roots.");
                Assert.IsTrue(lights.All(light => !light.enabled), "House must start completely unlit.");
                Time.timeScale = 20;
                float deadline = Time.realtimeSinceStartup + 10;
                bool sawPartiallyLitHighway = false;
                while (!highway.IsFullyLit && Time.realtimeSinceStartup < deadline)
                {
                    sawPartiallyLitHighway |= highway.GetComponentsInChildren<Light>().Any(light => light.enabled);
                    Assert.IsTrue(lights.All(light => !light.enabled), "House lit before the highway finished.");
                    await UniTask.NextFrame();
                }
                Assert.IsTrue(sawPartiallyLitHighway, "Test must cover the active lamp sequence.");
                Assert.IsTrue(highway.IsFullyLit);
                Assert.IsTrue(highway.GetComponentsInChildren<Light>().All(light => light.enabled && light.intensity >= 24));
                await UniTask.NextFrame();
                var firstStep = lights.Select(light => light.intensity).ToArray();
                var previous = firstStep;
                int risingFrames = 0;
                float finishAt = Time.time + highway.FadeDuration;
                deadline = Time.realtimeSinceStartup + 10;
                while (Time.time < finishAt && Time.realtimeSinceStartup < deadline)
                {
                    await UniTask.NextFrame();
                    var current = lights.Select(light => light.intensity).ToArray();
                    for (int i = 0; i < lights.Length; i++)
                        Assert.GreaterOrEqual(current[i], previous[i], "Brightness must increase smoothly.");
                    if (current[0] > previous[0]) risingFrames++;
                    previous = current;
                }
                Assert.GreaterOrEqual(Time.time, finishAt);
                Assert.Greater(risingFrames, 2, "House must fade over multiple frames, not switch on instantly.");
                Assert.IsTrue(lights.All(light => light.enabled && light.intensity > 0));
                for (int i = 0; i < lights.Length; i++)
                    Assert.Less(firstStep[i], lights[i].intensity * 0.2f, "The first fade frame must remain dim.");
                var fullBrightness = lights.Select(light => light.intensity).ToArray();
                await UniTask.NextFrame();
                CollectionAssert.AreEqual(fullBrightness, lights.Select(light => light.intensity).ToArray());
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                foreach (string path in new[] { TestLocationLauncher.HousePath, TestLocationLauncher.HighwayPath })
                    if (SceneManager.GetSceneByPath(path).isLoaded) await SceneManager.UnloadSceneAsync(path);
            }
        });

        [UnityTest]
        public IEnumerator StandaloneHouseIsLit() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await SceneManager.LoadSceneAsync(TestLocationLauncher.HousePath, LoadSceneMode.Additive);
                await UniTask.NextFrame();
                var house = Object.FindFirstObjectByType<HouseLights>();
                Assert.IsNotNull(house);
                Assert.IsTrue(AuthoredLights(house).All(light => light.enabled));
            }
            finally
            {
                if (SceneManager.GetSceneByPath(TestLocationLauncher.HousePath).isLoaded)
                    await SceneManager.UnloadSceneAsync(TestLocationLauncher.HousePath);
            }
        });

        [UnityTest]
        public IEnumerator PreparedHouseWaitsForExplicitReaction() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Explicit light reaction");
            var light = root.AddComponent<Light>();
            var house = root.AddComponent<HouseLights>();
            try
            {
                await UniTask.NextFrame();
                house.Prepare(.01f);
                await UniTask.DelayFrame(5);
                Assert.IsFalse(light.enabled, "Preparing a sequence must not start it implicitly.");
                house.Execute();
                await UniTask.WaitUntil(() => light.enabled).Timeout(System.TimeSpan.FromSeconds(3));
            }
            finally { Object.Destroy(root); }
        });
    }
}
