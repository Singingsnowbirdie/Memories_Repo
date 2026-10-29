using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Memories.Gameplay
{
    public sealed class GhostCosmosSky : MonoBehaviour
    {
        [SerializeField] private Material skyMaterial;
        [SerializeField, Range(0, 0.1f)] private float shimmer = 0.025f;
        [SerializeField, Min(0.1f)] private float starsFadeDuration = 6f;
        [SerializeField, Min(0.1f)] private float nebulaFadeDuration = 10f;
        [SerializeField] private Mesh[] asteroidMeshes;
        [SerializeField] private Material asteroidMaterial;
        [SerializeField, Range(16, 400)] private int asteroidCount = 180;
        private AsteroidBelt asteroidBelt;
        private static GhostCosmosSky shared;
        private readonly List<Camera> cameras = new List<Camera>();
        private Material instance;
        private Color tint;
        private HighwayLights highway;
        private float revealElapsed;
        private float starIntensity;
        private float blueIntensity;
        private float redIntensity;

        private void Awake()
        {
            if (skyMaterial == null) return;
            shared = this;
            instance = new Material(skyMaterial);
            tint = instance.GetColor("_Color");
            starIntensity = instance.GetFloat("_D2I");
            blueIntensity = instance.GetFloat("_BlueIntensity");
            redIntensity = instance.GetFloat("_RedIntensity");
            highway = FindFirstObjectByType<HighwayLights>();
            asteroidBelt = new AsteroidBelt(asteroidMeshes, asteroidMaterial, asteroidCount);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                OnSceneLoaded(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
            RenderSettings.skybox = instance;
            Apply();
        }

        public static void Bind(Camera camera)
        {
            if (shared == null || shared.instance == null || camera == null) return;
            var sky = camera.GetComponent<Skybox>();
            if (sky == null) sky = camera.gameObject.AddComponent<Skybox>();
            sky.enabled = true;
            sky.material = shared.instance;
            camera.clearFlags = CameraClearFlags.Skybox;
            if (!shared.cameras.Contains(camera)) shared.cameras.Add(camera);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (highway == null) highway = root.GetComponentInChildren<HighwayLights>(true);
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    if (camera.CompareTag("MainCamera")) Bind(camera);
            }
        }

        private void OnActiveSceneChanged(Scene previous, Scene current)
        {
            if (instance != null) RenderSettings.skybox = instance;
        }

        private void LateUpdate()
        {
            revealElapsed = highway != null && highway.IsFullyLit ? revealElapsed + Time.deltaTime : 0;
            Apply();
            for (int i = cameras.Count - 1; i >= 0; i--)
            {
                if (cameras[i] == null) cameras.RemoveAt(i);
                else asteroidBelt?.Draw(cameras[i]);
            }
        }

        private void Apply()
        {
            if (instance == null) return;
            float pulse = 1 + shimmer * (0.7f * Mathf.Sin(Time.time * 0.47f)
                + 0.3f * Mathf.Sin(Time.time * 0.83f));
            float starsDuration = Mathf.Max(0.1f, starsFadeDuration);
            float stars = highway == null ? 1 : Mathf.SmoothStep(0, 1,
                Mathf.Clamp01(revealElapsed / starsDuration));
            float nebula = highway == null ? 1 : Mathf.SmoothStep(0, 1,
                Mathf.Clamp01((revealElapsed - starsDuration) / Mathf.Max(0.1f, nebulaFadeDuration)));
            instance.SetColor("_Color", tint * pulse);
            instance.SetFloat("_D2I", starIntensity * stars);
            instance.SetFloat("_BlueIntensity", blueIntensity * nebula);
            instance.SetFloat("_RedIntensity", redIntensity * nebula);
        }

        private void OnDisable() => revealElapsed = 0;

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            if (shared == this) shared = null;
            if (RenderSettings.skybox == instance) RenderSettings.skybox = null;
            if (instance != null)
            {
                if (Application.isPlaying) Destroy(instance);
                else DestroyImmediate(instance);
            }
        }
    }
}
