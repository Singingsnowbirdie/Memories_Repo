using System.Collections;
using System.Collections.Generic;
using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Only dissolves Lily. Availability and lighting are explicit graph actions.
    public sealed class ParkMimeReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private GameObject lily;
        [SerializeField] private ReactionInteractable suitcase;
        [SerializeField] private QuestItem pendants, tickets;
        [SerializeField] private Shader dissolveShader;
        [SerializeField] private LightingGroup lighting;
        [SerializeField] private float fadeDuration = 4f;
        private bool finished;
        private int legacyStage;
        private readonly List<(Renderer renderer, Material[] materials)> originals = new();
        private readonly List<Material> temporary = new();
        public string StateKey => "park.mimes.sequence";
        public string CaptureState() => finished ? "4" : legacyStage.ToString();
        public void RestoreState(string value)
        {
            int.TryParse(value, out legacyStage);
            finished = legacyStage >= 4;
        }

        public void RestoreAfterLoad(QuestInventory inventory)
        {
            bool bought = inventory.WasCollected(pendants.Id);
            finished |= inventory.IsConsumed(pendants.Id);
            bool arrived = legacyStage > 0 || inventory.IsConsumed(tickets.Id);
            // A save during the cinema fade has consumed tickets before the reveal node runs.
            lily.SetActive(arrived && !finished);
            lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(arrived && !finished);
            if (!suitcase.GetComponent<ObjectStateTracker>().HasRestoredState || bought || finished)
                suitcase.SetInteractionEnabled(legacyStage == 2 && !bought && !finished);
            lighting.RestoreBrightness(arrived && !finished ? 1 : 0);
        }

        public override void Execute()
        {
            if (finished) return;
            finished = true;
            GetComponent<ObjectStateTracker>().Capture();
            StartCoroutine(Dissolve());
        }

        private IEnumerator Dissolve()
        {
            foreach (var renderer in lily.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                originals.Add((renderer, source));
                var replacement = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    if (!source[i]) continue;
                    var material = new Material(source[i]) { shader = dissolveShader };
                    material.SetFloat("_Reveal", 1);
                    replacement[i] = material;
                    temporary.Add(material);
                }
                renderer.sharedMaterials = replacement;
            }
            for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
            {
                float value = 1 - Mathf.SmoothStep(0, 1, elapsed / fadeDuration);
                foreach (var material in temporary) material.SetFloat("_Reveal", value);
                yield return null;
            }
            lily.SetActive(false);
            RestoreMaterials();
        }

        private void RestoreMaterials()
        {
            foreach (var entry in originals) if (entry.renderer) entry.renderer.sharedMaterials = entry.materials;
            originals.Clear();
            foreach (var material in temporary) if (material) Destroy(material);
            temporary.Clear();
        }
        private void OnDisable() { StopAllCoroutines(); RestoreMaterials(); }
        private void OnDestroy() => RestoreMaterials();
    }
}
