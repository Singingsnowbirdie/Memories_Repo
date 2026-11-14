using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Source intensities stay constant. Non-emissive material slots are never written.
    public sealed class LightingTransition
    {
        private readonly List<(Light light, float intensity)> lights = new();
        private readonly List<(Renderer renderer, int slot, Color emission, Color color)> glow = new();
        private readonly MonoBehaviour owner;
        private MaterialPropertyBlock block;
        private Coroutine routine;
        public float Brightness { get; private set; } = 1;
        public bool IsRunning => routine != null;

        public LightingTransition(MonoBehaviour owner, Light[] sources, Renderer[] renderers, float[] intensities = null)
        {
            this.owner = owner;
            var seen = new HashSet<Light>();
            for (int i = 0; sources != null && i < sources.Length; i++)
            {
                var light = sources[i];
                if (!light || !seen.Add(light)) continue;
                // Explicit baselines describe lights authored off for an initial dark stage.
                float intensity = intensities != null && i < intensities.Length ? intensities[i] : light.intensity;
                if (intensity <= 0 || (intensities == null && !light.enabled)) continue;
                lights.Add((light, intensity));
            }
            var seenRenderers = new HashSet<Renderer>();
            foreach (var renderer in renderers ?? Array.Empty<Renderer>())
            {
                if (!renderer || !seenRenderers.Add(renderer)) continue;
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (!material || !material.IsKeywordEnabled("_EMISSION") ||
                        !material.HasProperty("_EmissionColor") || !material.HasProperty("_BaseColor")) continue;
                    var emission = material.GetColor("_EmissionColor");
                    if (emission.maxColorComponent <= 0) continue;
                    glow.Add((renderer, slot, emission, material.GetColor("_BaseColor")));
                }
            }
        }

        public void SetImmediate(float brightness)
        {
            Cancel();
            Apply(brightness);
        }

        public void Change(float brightness, float duration)
        {
            Cancel();
            if (duration <= 0 || Mathf.Approximately(Brightness, brightness)) { Apply(brightness); return; }
            routine = owner.StartCoroutine(Fade(brightness, duration));
        }

        public void Cancel()
        {
            if (routine != null && owner) owner.StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator Fade(float target, float duration)
        {
            float start = Brightness;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                Apply(Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, elapsed / duration)));
                yield return null;
            }
            Apply(target);
            routine = null;
        }

        private void Apply(float brightness)
        {
            Brightness = Mathf.Max(0, brightness);
            foreach (var entry in lights)
                if (entry.light)
                {
                    entry.light.intensity = entry.intensity * Brightness;
                    entry.light.enabled = Brightness > 0;
                }
            block ??= new MaterialPropertyBlock();
            foreach (var entry in glow)
            {
                if (!entry.renderer) continue;
                block.Clear();
                entry.renderer.GetPropertyBlock(block, entry.slot);
                block.SetColor("_EmissionColor", entry.emission * Brightness);
                block.SetColor("_BaseColor", Color.Lerp(entry.color * .12f, entry.color, Brightness));
                entry.renderer.SetPropertyBlock(block, entry.slot);
            }
        }
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Reactions/Lighting Group")]
    public sealed class LightingGroup : MonoBehaviour, IObjectStateParticipant
    {
        [SerializeField] private Light[] lights = Array.Empty<Light>();
        [SerializeField] private Renderer[] glowingRenderers = Array.Empty<Renderer>();
        [SerializeField] private float[] sourceIntensities = Array.Empty<float>();
        [SerializeField, Min(0)] private float initialBrightness = 1;
        private LightingTransition transition;
        private float targetBrightness = 1;
        public string StateKey => "lighting-group";
        public bool IsTransitioning => transition != null && transition.IsRunning;
        private LightingTransition Transition => transition ??= new LightingTransition(this, lights, glowingRenderers,
            sourceIntensities.Length == lights.Length && sourceIntensities.Length > 0 ? sourceIntensities : null);
        private void Awake()
        {
            if (transition == null) RestoreBrightness(initialBrightness);
        }
        // Only initialization/save restoration uses this path. Live transitions are graph actions.
        public void RestoreBrightness(float brightness)
        {
            targetBrightness = brightness;
            Transition.SetImmediate(brightness);
        }
        public void Change(float brightness, float duration)
        {
            targetBrightness = brightness;
            Transition.Change(brightness, duration);
            GetComponent<ObjectStateTracker>()?.Capture();
        }
        public string CaptureState() => targetBrightness.ToString(CultureInfo.InvariantCulture);
        public void RestoreState(string value)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var brightness) ||
                float.IsNaN(brightness) || float.IsInfinity(brightness) || brightness < 0) return;
            RestoreBrightness(brightness);
        }
        private void OnDisable() => transition?.SetImmediate(targetBrightness);
    }
}
