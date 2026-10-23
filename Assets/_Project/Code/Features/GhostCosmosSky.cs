using UnityEngine;

namespace Memories.Gameplay
{
    [RequireComponent(typeof(Camera), typeof(Skybox))]
    public sealed class GhostCosmosSky : MonoBehaviour
    {
        [SerializeField] private Material skyMaterial;
        [SerializeField, Range(0, 0.1f)] private float shimmer = 0.025f;
        [SerializeField] private Mesh[] asteroidMeshes;
        [SerializeField] private Material asteroidMaterial;
        [SerializeField, Range(16, 400)] private int asteroidCount = 180;
        private AsteroidBelt asteroidBelt;
        private Camera skyCamera;
        private Material instance;
        private Color tint;
        private HighwayLights highway;

        private void Awake()
        {
            if (skyMaterial == null) return;
            instance = new Material(skyMaterial);
            tint = instance.GetColor("_Color");
            highway = FindFirstObjectByType<HighwayLights>();
            GetComponent<Skybox>().material = instance;
            GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
            skyCamera = GetComponent<Camera>();
            asteroidBelt = new AsteroidBelt(asteroidMeshes, asteroidMaterial, asteroidCount);
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
            asteroidBelt?.Draw(skyCamera);
        }

        private void Apply()
        {
            if (instance == null) return;
            float pulse = 1 + shimmer * (0.7f * Mathf.Sin(Time.time * 0.47f)
                + 0.3f * Mathf.Sin(Time.time * 0.83f));
            instance.SetColor("_Color", tint * pulse * (highway != null ? highway.SkyBrightness : 1));
        }

        private void OnDestroy()
        {
            if (instance != null) Destroy(instance);
        }
    }
}
