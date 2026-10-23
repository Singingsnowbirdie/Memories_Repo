using UnityEngine;
using UnityEngine.Rendering;
using Memories.Gameplay.Player;

namespace Memories.Gameplay
{
    [RequireComponent(typeof(Light))]
    public sealed class GuidingLight : MonoBehaviour
    {
        [SerializeField, Min(0)] private float delay = 5f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 6f;
        [SerializeField, Min(0)] private float targetIntensity = 3f;
        [SerializeField] private Material fireflyMaterial;
        [SerializeField, Range(8, 128)] private int fireflyCount = 48;
        [SerializeField, Min(0.1f)] private float swarmRadius = 1.8f;
        [SerializeField, Min(0.1f)] private float colorCycleDuration = 24f;
        [SerializeField] private Color coldColor = new Color(0.25f, 0.65f, 1f);
        [SerializeField] private Color warmColor = new Color(1f, 0.6f, 0.15f);
        [SerializeField, Min(1)] private float keepAheadDistance = 10f;

        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int Tint = Shader.PropertyToID("_Tint");
        private Light source;
        private ParticleSystem swarm;
        private ParticleSystemRenderer glow;
        private MaterialPropertyBlock properties;
        private float elapsed;
        private PlayerPresenter player;
        private Transform playerTransform;
        private Vector3 retreatDirection;
        public float TimeSinceAppearance => isActiveAndEnabled && elapsed > delay ? elapsed - delay : -1f;

        public void Initialize(PlayerPresenter presenter, Transform playerPosition)
        {
            player = presenter;
            playerTransform = playerPosition;
            retreatDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            player.ControlsEnabled = !isActiveAndEnabled || elapsed > delay;
        }

        private void Awake()
        {
            source = GetComponent<Light>();
            var root = new GameObject("Warm fireflies");
            root.SetActive(false);
            root.transform.SetParent(transform, false);
            swarm = root.AddComponent<ParticleSystem>();
            var main = swarm.main;
            main.playOnAwake = false;
            main.loop = true;
            main.prewarm = true;
            main.duration = 12;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8, 12);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.8f, 0.8f, 0.8f), Color.white);
            main.maxParticles = fireflyCount;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var emission = swarm.emission;
            emission.rateOverTime = fireflyCount / 10f;
            var shape = swarm.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = swarmRadius;
            shape.scale = new Vector3(1, 0.55f, 0.75f);

            var noise = swarm.noise;
            noise.enabled = true;
            noise.strength = 0.28f;
            noise.frequency = 0.45f;
            noise.scrollSpeed = 0.18f;
            noise.damping = true;

            // Different lifetimes and birth times keep the pulses out of sync.
            var color = swarm.colorOverLifetime;
            color.enabled = true;
            var flicker = new Gradient();
            flicker.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] {
                    new GradientAlphaKey(0, 0),
                    new GradientAlphaKey(0.9f, 0.15f),
                    new GradientAlphaKey(0.25f, 0.35f),
                    new GradientAlphaKey(1, 0.55f),
                    new GradientAlphaKey(0.35f, 0.75f),
                    new GradientAlphaKey(0, 1)
                });
            color.color = flicker;

            glow = root.GetComponent<ParticleSystemRenderer>();
            glow.renderMode = ParticleSystemRenderMode.Billboard;
            glow.sharedMaterial = fireflyMaterial;
            glow.shadowCastingMode = ShadowCastingMode.Off;
            glow.receiveShadows = false;
            properties = new MaterialPropertyBlock();
            ApplyBrightness(0);
            root.SetActive(true);
        }

        private void OnEnable()
        {
            elapsed = 0;
            ApplyBrightness(0);
            swarm.Play();
        }

        private void Update()
        {
            // Pause also freezes the reveal and the particle simulation.
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01((elapsed - delay) / Mathf.Max(0.01f, fadeDuration));
            ApplyBrightness(Mathf.SmoothStep(0, 1, progress));
        }

        private void ApplyBrightness(float brightness)
        {
            float cycleTime = Mathf.Max(0, elapsed - delay);
            float phase = Mathf.Repeat(cycleTime / Mathf.Max(0.1f, colorCycleDuration), 1f);
            float warmth = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            Color tint = Color.Lerp(coldColor, warmColor, warmth);
            source.color = tint;
            source.intensity = targetIntensity * brightness;
            source.enabled = brightness > 0;
            glow.enabled = brightness > 0;
            if (player != null) player.ControlsEnabled = brightness > 0;
            properties.SetFloat(Brightness, brightness * 1.8f);
            // Tint all living particles immediately, not just newly emitted ones.
            properties.SetColor(Tint, tint);
            glow.SetPropertyBlock(properties);
        }

        private void LateUpdate()
        {
            if (playerTransform == null || elapsed <= delay || Time.deltaTime <= 0) return;
            // Run after player movement. Only retreat: never follow the player back.
            float gap = Vector3.Dot(transform.position - playerTransform.position, retreatDirection);
            float distance = Mathf.Max(keepAheadDistance, swarmRadius * 2 + 2);
            if (gap < distance)
                transform.position += retreatDirection * (distance - gap);
        }

        private void OnDisable()
        {
            ApplyBrightness(0);
            swarm.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (player != null) player.ControlsEnabled = true;
        }
    }
}
