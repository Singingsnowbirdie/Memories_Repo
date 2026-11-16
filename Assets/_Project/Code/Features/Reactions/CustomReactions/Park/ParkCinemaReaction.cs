using System.Collections;
using System.Collections.Generic;
using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Only dissolves the two mannequins. The graph owns purchase, lighting and the next encounter.
    public sealed class ParkCinemaReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private GameObject lily, cashier;
        [SerializeField] private Shader dissolveShader;
        [SerializeField] private float fadeDuration = 4f;
        [SerializeField] private QuestItem tickets, bouquetItem;
        [SerializeField] private LightingGroup cinemaLighting, benchLighting;
        private bool finished;
        private int legacyStage;
        private readonly List<(Renderer renderer, Material[] materials)> originals = new();
        private readonly List<Material> dissolveMaterials = new();

        // Keep the old save key readable; no live stage controller remains.
        public string StateKey => "park.cinema.sequence";
        public string CaptureState() => finished ? "4" : legacyStage.ToString();
        public void RestoreState(string value)
        {
            int.TryParse(value, out legacyStage);
            finished = legacyStage >= 4;
        }

        // Once at load, also supports saves made before object activity was persisted.
        public void RestoreAfterLoad(QuestInventory inventory)
        {
            bool bought = inventory.WasCollected(tickets.Id);
            finished |= inventory.IsConsumed(tickets.Id);
            bool arrived = legacyStage > 0 || inventory.IsConsumed(bouquetItem.Id) || bought;
            if (!lily.GetComponent<ObjectStateTracker>().HasRestoredState || finished)
            {
                lily.SetActive(arrived && !finished);
            }
            if (!cashier.GetComponent<ObjectStateTracker>().HasRestoredState || finished)
            {
                bool revealed = legacyStage >= 2 || bought;
                cashier.SetActive(revealed && !finished);
            }
            // Hidden encounters are ready for their reveal, including old object snapshots.
            lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(!finished);
            cashier.GetComponent<ReactionInteractable>().SetInteractionEnabled(!bought && !finished);
            cinemaLighting.RestoreBrightness(arrived && !finished ? 1 : 0);
            benchLighting.RestoreBrightness(finished ? 0 : 1);
        }

        public override void Execute()
        {
            if (finished) return;
            finished = true;
            GetComponent<ObjectStateTracker>().Capture();
            StartCoroutine(FadeOut());
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
            foreach (var mannequin in new[] { lily, cashier })
                foreach (var renderer in mannequin.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            RestoreMaterials();
        }
        private void RestoreMaterials()
        {
            foreach (var entry in originals) if (entry.renderer) entry.renderer.sharedMaterials = entry.materials;
            originals.Clear();
            foreach (var material in dissolveMaterials) if (material) Destroy(material);
            dissolveMaterials.Clear();
        }
        private void OnDisable() { StopAllCoroutines(); RestoreMaterials(); }
        private void OnDestroy() => RestoreMaterials();
    }
}
