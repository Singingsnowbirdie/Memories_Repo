using UnityEngine;
using System.Collections.Generic;

namespace Memories.Gameplay
{
    public sealed class HighwayLights : MonoBehaviour
    {
        [SerializeField, Min(0)] private float startDelay = 10f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 4f;
        [SerializeField, Min(0.01f)] private float pairInterval = 1f;
        [SerializeField, Min(0.01f)] private float pairSpacing = 10f;
        [SerializeField] private Color lightColor = new Color(0.65f, 0.8f, 1f);
        [SerializeField, Min(0)] private float intensity = 24f;
        private sealed class Lamp
        {
            public Light[] Lights;
            public Renderer[] Renderers;
            public float Distance;
            public float Delay;
        }
        private readonly List<Lamp> lamps = new List<Lamp>();
        private MaterialPropertyBlock properties;
        private float elapsed;
        public bool IsFullyLit { get; private set; }
        public float FadeDuration => Mathf.Max(0.01f, fadeDuration);
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            float firstDistance = float.PositiveInfinity;
            foreach (Transform child in transform)
            {
                var lights = child.GetComponentsInChildren<Light>(true);
                if (lights.Length == 0) continue;
                float distance = child.localPosition.z;
                firstDistance = Mathf.Min(firstDistance, distance);
                lamps.Add(new Lamp
                {
                    Lights = lights,
                    Renderers = child.GetComponentsInChildren<Renderer>(true),
                    Distance = distance
                });
            }
            foreach (var lamp in lamps)
                lamp.Delay = Mathf.Round((lamp.Distance - firstDistance) / Mathf.Max(0.01f, pairSpacing)) * pairInterval;
            properties = new MaterialPropertyBlock();
            foreach (var lamp in lamps) Apply(lamp, 0);
        }

        private void OnEnable()
        {
            elapsed = 0;
            IsFullyLit = false;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            bool complete = lamps.Count > 0;
            foreach (var lamp in lamps)
            {
                float progress = Mathf.Clamp01((elapsed - startDelay - lamp.Delay) / Mathf.Max(0.01f, fadeDuration));
                Apply(lamp, Mathf.SmoothStep(0, 1, progress));
                if (progress < 1) complete = false;
            }
            IsFullyLit = complete;
        }

        private void Apply(Lamp group, float brightness)
        {
            foreach (var lamp in group.Lights)
            {
                lamp.color = lightColor;
                lamp.intensity = intensity * brightness;
                lamp.enabled = brightness > 0;
            }
            foreach (var renderer in group.Renderers)
            {
                renderer.GetPropertyBlock(properties);
                properties.SetColor(EmissionColor, lightColor * (4f * brightness));
                renderer.SetPropertyBlock(properties);
            }
        }

        private void OnDisable()
        {
            IsFullyLit = false;
            foreach (var lamp in lamps) Apply(lamp, 0);
        }
    }
}
