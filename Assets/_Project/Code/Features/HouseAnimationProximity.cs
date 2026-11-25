using System.Collections.Generic;
using Memories.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay
{
    // Создаётся только для дома; также приостанавливает шейдерный ветер листвы, пока игрок внутри.
    public sealed class HouseAnimationProximity : MonoBehaviour
    {
        private static readonly int VegetationWindPaused = Shader.PropertyToID("_MemoriesVegetationWindPaused");
        private const float EnterDistance = 15f;
        private const float ExitDistance = 20f;
        private readonly List<FloatingScenery> animations = new List<FloatingScenery>();
        private Bounds houseBounds;
        private PlayerMotor player;
        private bool nearby;
        private float nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Shader.SetGlobalFloat(VegetationWindPaused, 0);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                OnSceneLoaded(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != "Assets/_Project/Locations/House.unity") return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<HouseAnimationProximity>(true) != null) return;

            var host = new GameObject("House animation proximity");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<HouseAnimationProximity>().Initialize(scene);
        }

        private void Initialize(Scene scene)
        {
            bool hasBounds = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var animation in root.GetComponentsInChildren<FloatingScenery>(true))
                {
                    animations.Add(animation);
                    animation.DistancePaused = true;
                    foreach (var renderer in animation.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!hasBounds) { houseBounds = renderer.bounds; hasBounds = true; }
                        else houseBounds.Encapsulate(renderer.bounds);
                    }
                }
            }
            // Проверяем горизонтальные границы дома, чтобы все этажи считались его внутренним пространством.
            CheckDistance();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            CheckDistance();
        }

        private void CheckDistance()
        {
            nextCheck = Time.unscaledTime + 0.5f;
            if (player == null) player = FindFirstObjectByType<PlayerMotor>();
            bool shouldRun = false;
            bool insideHouse = false;
            if (player != null && player.isActiveAndEnabled)
            {
                Vector3 position = player.transform.position;
                position.y = houseBounds.center.y;
                insideHouse = houseBounds.Contains(position);
                float distance = nearby ? ExitDistance : EnterDistance;
                shouldRun = houseBounds.SqrDistance(position) <= distance * distance;
            }
            Shader.SetGlobalFloat(VegetationWindPaused, insideHouse ? 1 : 0);
            nearby = shouldRun;
            foreach (var animation in animations)
                if (animation != null) animation.DistancePaused = !nearby;
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(VegetationWindPaused, 0);
            foreach (var animation in animations)
                if (animation != null) animation.DistancePaused = false;
        }
    }
}
