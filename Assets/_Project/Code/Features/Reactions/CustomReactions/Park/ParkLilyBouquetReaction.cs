using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Memories.Gameplay.Interaction;

namespace Memories.Gameplay.Reactions
{
    // Started explicitly by the bouquet handover graph. Restoration never replays the animation.
    public sealed class ParkLilyBouquetReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private GameObject cinemaLily;
        [SerializeField] private GameObject bouquet;
        [SerializeField] private Shader dissolveShader;
        [SerializeField] private float duration = 4f;
        [SerializeField] private ParkCinemaSequence cinemaSequence;
        [SerializeField] private Light[] approachLights = System.Array.Empty<Light>();
        [SerializeField] private Renderer[] approachGlowingRenderers = System.Array.Empty<Renderer>();
        private readonly List<(Light light, bool enabled, float intensity)> approachLightDefaults = new();
        private readonly List<(Renderer renderer, int slot, Color emission, Color color)> approachGlowDefaults = new();
        private bool approachCached;
        private MaterialPropertyBlock approachBlock;
        private bool completed;
        public bool Completed => completed;
        private readonly List<(Renderer renderer, Material[] originals)> originals = new();
        private readonly List<Material> temporary = new();
        public string StateKey => "park.lily.bouquet";
        public string CaptureState() => completed ? "1" : "0";

        public override void Execute()
        {
            if (completed) return;
            completed = true;
            StartCoroutine(FadeApproachLighting());
            GetComponent<ReactionInteractable>().SetInteractionEnabled(false);
            GetComponent<ObjectStateTracker>().Capture();
            StartCoroutine(Transition());
        }

        private IEnumerator Transition()
        {
            yield return new WaitForSeconds(.6f);
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                originals.Add((renderer, source));
                var replacement = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i] == null) continue;
                    var material = new Material(source[i]) { shader = dissolveShader };
                    material.SetFloat("_Reveal", 1);
                    temporary.Add(material);
                    replacement[i] = material;
                }
                renderer.sharedMaterials = replacement;
            }
            if (cinemaSequence != null) cinemaSequence.RevealCinema();
            else ObjectReveal.Play(cinemaLily, dissolveShader, duration, 0);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                foreach (var material in temporary)
                    material.SetFloat("_Reveal", 1 - Mathf.SmoothStep(0, 1, elapsed / duration));
                yield return null;
            }
            RestoreMaterials();
            ApplyCompleted();
        }

        public void RestoreState(string state)
        {
            StopAllCoroutines();
            RestoreMaterials();
            completed = state == "1";
            SetApproachLighting(!completed);
            if (completed) ApplyCompleted();
            else
            {
                bouquet.SetActive(false);
                cinemaLily.SetActive(false);
                foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = true;
            }
        }

        private void ApplyCompleted()
        {
            // Keep the interaction owner alive so a concurrently displayed story text can finish.
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            bouquet.SetActive(false);
            if (cinemaSequence == null) cinemaLily.SetActive(true);
            GetComponent<ReactionInteractable>().SetInteractionEnabled(false);
        }

        private void RestoreMaterials()
        {
            foreach (var entry in originals)
                if (entry.renderer != null) entry.renderer.sharedMaterials = entry.originals;
            originals.Clear();
            foreach (var material in temporary) if (material != null) Destroy(material);
            temporary.Clear();
        }
        private IEnumerator FadeApproachLighting()
        {
            float fadeDuration = Mathf.Max(.01f, duration);
            for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
            {
                ApplyApproachLighting(1 - Mathf.SmoothStep(0, 1, elapsed / fadeDuration));
                yield return null;
            }
            ApplyApproachLighting(0);
        }

        // Saved progress restores the final lighting without replaying the transition.
        public void SetApproachLighting(bool enabled) => ApplyApproachLighting(enabled ? 1 : 0);

        private void ApplyApproachLighting(float brightness)
        {
            if (!approachCached)
            {
                foreach (var light in approachLights)
                    if (light) approachLightDefaults.Add((light, light.enabled, light.intensity));
                foreach (var renderer in approachGlowingRenderers)
                {
                    if (!renderer) continue;
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        if (!material || !material.IsKeywordEnabled("_EMISSION") || !material.HasProperty("_EmissionColor") || !material.HasProperty("_BaseColor")) continue;
                        var emission = material.GetColor("_EmissionColor");
                        if (emission.maxColorComponent <= 0) continue;
                        approachGlowDefaults.Add((renderer, i, emission, material.GetColor("_BaseColor")));
                    }
                }
                approachCached = true;
            }
            foreach (var entry in approachLightDefaults)
                if (entry.light)
                {
                    entry.light.intensity = entry.intensity * brightness;
                    entry.light.enabled = brightness > 0 && entry.enabled;
                }
            approachBlock ??= new MaterialPropertyBlock();
            foreach (var entry in approachGlowDefaults)
            {
                if (!entry.renderer) continue;
                entry.renderer.GetPropertyBlock(approachBlock, entry.slot);
                approachBlock.SetColor("_EmissionColor", entry.emission * brightness);
                approachBlock.SetColor("_BaseColor", Color.Lerp(entry.color * .12f, entry.color, brightness));
                entry.renderer.SetPropertyBlock(approachBlock, entry.slot);
            }
        }
        private void OnDisable() { StopAllCoroutines(); RestoreMaterials(); }
        private void OnDestroy() => RestoreMaterials();
    }
}
