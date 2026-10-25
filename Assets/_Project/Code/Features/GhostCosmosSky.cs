using UnityEngine;

namespace Memories.Gameplay
{
    [RequireComponent(typeof(Camera), typeof(Skybox))]
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
        private Camera skyCamera;
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
            instance = new Material(skyMaterial);
            tint = instance.GetColor("_Color");
            starIntensity = instance.GetFloat("_D2I");
            blueIntensity = instance.GetFloat("_BlueIntensity");
            redIntensity = instance.GetFloat("_RedIntensity");
            highway = FindFirstObjectByType<HighwayLights>();
            GetComponent<Skybox>().material = instance;
            GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
            skyCamera = GetComponent<Camera>();
            asteroidBelt = new AsteroidBelt(asteroidMeshes, asteroidMaterial, asteroidCount);
            Apply();
        }

        private void LateUpdate()
        {
            revealElapsed = highway != null && highway.IsFullyLit ? revealElapsed + Time.deltaTime : 0;
            Apply();
            asteroidBelt?.Draw(skyCamera);
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
            if (instance != null) Destroy(instance);
        }
    }
}
