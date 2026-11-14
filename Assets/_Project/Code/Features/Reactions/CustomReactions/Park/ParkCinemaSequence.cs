using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Progress is changed only by graph actions or the explicitly started bouquet transition.
    public sealed class ParkCinemaSequence : MonoBehaviour, IObjectStateParticipant
    {
        [SerializeField] private ParkLilyBouquetReaction bouquetReaction;
        [SerializeField] private GameObject lily, cashier;
        [SerializeField] private Shader dissolveShader;
        [SerializeField] private QuestItem tickets;
        [SerializeField] private Renderer[] glowingRenderers = Array.Empty<Renderer>();
        [SerializeField] private Light[] lights = Array.Empty<Light>();
        [SerializeField] private float[] intensities = Array.Empty<float>();
        [SerializeField] private float fadeDuration = 4f;
        private int stage; // 0: dark, 1: Lily arrived, 2: cashier available, 3: bought, 4: finished.
        private readonly List<(Renderer renderer, int slot, Color emission, Color color)> glow = new();
        private readonly List<(Renderer renderer, Material[] materials)> originals = new();
        private readonly List<Material> dissolveMaterials = new();
        private MaterialPropertyBlock block;
        public string StateKey => "park.cinema.sequence";
        public string CaptureState() => stage.ToString();
        public void RestoreState(string value) { if (int.TryParse(value, out var saved)) stage = Mathf.Clamp(saved, 0, 4); }

        private void Awake()
        {
            foreach (var renderer in glowingRenderers)
            {
                if (!renderer) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var m = materials[i];
                    if (!m || !m.HasProperty("_EmissionColor")) continue;
                    var emission = m.GetColor("_EmissionColor");
                    if (emission.maxColorComponent <= 0) continue;
                    glow.Add((renderer, i, emission, m.GetColor("_BaseColor")));
                }
            }
            SetLight(0);
        }

        // Called once after all object states and tester checkpoints have been restored.
        public void Bind(QuestInventory inventory)
        {
            if (stage == 0 && bouquetReaction.Completed) stage = 1; // Older saves.
            if (inventory.IsConsumed(tickets.Id)) stage = 4;
            else if (inventory.Contains(tickets.Id)) stage = 3;
            lily.SetActive(stage > 0 && stage < 4);
            lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(stage > 0 && stage < 4);
            cashier.SetActive(stage >= 2 && stage < 4);
            cashier.GetComponent<ReactionInteractable>().SetInteractionEnabled(stage == 2);
            SetLight(stage > 0 && stage < 4 ? 1 : 0);
        }

        public void RevealCinema()
        {
            if (stage != 0) return;
            stage = 1;
            Save();
            RevealAsync().Forget();
        }

        private async UniTask RevealAsync()
        {
            try
            {
                var token = this.GetCancellationTokenOnDestroy();
                lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
                ObjectReveal.Play(lily, dissolveShader, fadeDuration, 0);
                for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
                {
                    SetLight(Mathf.SmoothStep(0, 1, elapsed / fadeDuration));
                    await UniTask.NextFrame(cancellationToken: token);
                }
                SetLight(1);
            }
            catch (OperationCanceledException) { }
        }

        public void OpenCashier()
        {
            if (stage != 1) return;
            stage = 2;
            cashier.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
            ObjectReveal.Play(cashier, dissolveShader, 1.5f, 0);
            Save();
        }
        public void TicketsPurchased()
        {
            if (stage != 2) return;
            stage = 3;
            cashier.GetComponent<ReactionInteractable>().SetInteractionEnabled(false);
            Save();
        }
        public void Finish()
        {
            if (stage == 4) return;
            stage = 4;
            lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(false);
            cashier.GetComponent<ReactionInteractable>().SetInteractionEnabled(false);
            Save();
            StartCoroutine(FadeOut()); // No camera control in this transition.
        }

        private IEnumerator FadeOut()
        {
            foreach (var mannequin in new[] { lily, cashier })
            foreach (var renderer in mannequin.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                originals.Add((renderer, source));
                var replacement = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    if (!source[i]) continue;
                    replacement[i] = new Material(source[i]) { shader = dissolveShader };
                    replacement[i].SetFloat("_Reveal", 1);
                    dissolveMaterials.Add(replacement[i]);
                }
                renderer.sharedMaterials = replacement;
            }
            for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
            {
                float value = 1 - Mathf.SmoothStep(0, 1, elapsed / fadeDuration);
                foreach (var material in dissolveMaterials) material.SetFloat("_Reveal", value);
                SetLight(value);
                yield return null;
            }
            lily.SetActive(false);
            cashier.SetActive(false);
            RestoreMaterials();
            SetLight(0);
        }
        private void Save() => GetComponent<ObjectStateTracker>().Capture();
        private void SetLight(float value)
        {
            block ??= new MaterialPropertyBlock();
            foreach (var entry in glow)
            {
                if (!entry.renderer) continue;
                entry.renderer.GetPropertyBlock(block, entry.slot);
                block.SetColor("_EmissionColor", entry.emission * value);
                block.SetColor("_BaseColor", Color.Lerp(entry.color * .12f, entry.color, value));
                entry.renderer.SetPropertyBlock(block, entry.slot);
                block.Clear();
            }
            for (int i = 0; i < lights.Length; i++)
                if (lights[i]) { lights[i].intensity = intensities[i] * value; lights[i].enabled = value > 0; }
        }
        private void RestoreMaterials()
        {
            foreach (var entry in originals) if (entry.renderer) entry.renderer.sharedMaterials = entry.materials;
            originals.Clear();
            foreach (var material in dissolveMaterials) if (material) Destroy(material);
            dissolveMaterials.Clear();
        }
        private void OnDestroy() => RestoreMaterials();
    }
}
