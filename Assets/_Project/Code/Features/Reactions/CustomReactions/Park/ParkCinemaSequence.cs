using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private float fadeDuration = 4f;
        [SerializeField] private LightingGroup cinemaLighting, benchLighting;
        [SerializeField] private ReactionGraph revealLightingGraph;
        private ReactionRunner lightingRunner;
        private int stage; // 0: dark, 1: Lily arrived, 2: cashier available, 3: bought, 4: finished.

        private readonly List<(Renderer renderer, Material[] materials)> originals = new();
        private readonly List<Material> dissolveMaterials = new();

        public string StateKey => "park.cinema.sequence";
        public string CaptureState() => stage.ToString();
        public void RestoreState(string value) { if (int.TryParse(value, out var saved)) stage = Mathf.Clamp(saved, 0, 4); }

        // Called once after all object states and tester checkpoints have been restored.
        public void Bind(QuestInventory inventory)
        {
            if (stage == 0 && bouquetReaction.Completed) stage = 1; // Older saves.
            if (inventory.IsConsumed(tickets.Id)) stage = 4;
            else if (inventory.Contains(tickets.Id)) stage = 3;
            bouquetReaction.RestoreApproachLighting(stage == 0 && !bouquetReaction.Completed);
            lily.SetActive(stage > 0 && stage < 4);
            lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(stage > 0 && stage < 4);
            cashier.SetActive(stage >= 2 && stage < 4);
            cashier.GetComponent<ReactionInteractable>().SetInteractionEnabled(stage == 2);
            RestoreCinemaLighting(stage > 0 && stage < 4 ? 1 : 0);
            RestoreBenchLighting(stage < 4 ? 1 : 0);
        }

        public void RevealCinema()
        {
            if (stage != 0) return;
            stage = 1;
            Save();
            lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
            ObjectReveal.Play(lily, dissolveShader, fadeDuration, 0);
            lightingRunner ??= new ReactionRunner(null, null, this,
                objectBindings: new[] { new ReactionObjectBinding("Освещение кинотеатра", cinemaLighting.gameObject) });
            if (!lightingRunner.TryRun(revealLightingGraph))
                throw new InvalidOperationException("Не удалось запустить граф появления света кинотеатра.");
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

                yield return null;
            }
            lily.SetActive(false);
            cashier.SetActive(false);
            RestoreMaterials();


        }
        private void RestoreBenchLighting(float value) => benchLighting.RestoreBrightness(value);
        private void Save() => GetComponent<ObjectStateTracker>().Capture();
        private void RestoreCinemaLighting(float value) => cinemaLighting.RestoreBrightness(value);
        private void RestoreMaterials()
        {
            foreach (var entry in originals) if (entry.renderer) entry.renderer.sharedMaterials = entry.materials;
            originals.Clear();
            foreach (var material in dissolveMaterials) if (material) Destroy(material);
            dissolveMaterials.Clear();
        }
        private void OnDisable() => lightingRunner?.Stop();
        private void OnDestroy() { lightingRunner?.Dispose(); RestoreMaterials(); }
    }
}
