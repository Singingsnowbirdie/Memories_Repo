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
        private bool completed;
        private readonly List<(Renderer renderer, Material[] originals)> originals = new();
        private readonly List<Material> temporary = new();
        public string StateKey => "park.lily.bouquet";
        public string CaptureState() => completed ? "1" : "0";

        public override void Execute()
        {
            if (completed) return;
            completed = true;
            ObjectReveal.Play(bouquet, dissolveShader, 1.5f, 0);
            GetComponent<ReactionInteractable>().SetInteractionEnabled(false);
            GetComponent<ObjectStateTracker>().Capture();
            StartCoroutine(Transition());
        }

        private IEnumerator Transition()
        {
            // Finish the explicitly started reveal before replacing its temporary materials.
            yield return new WaitUntil(() => bouquet.GetComponent<ObjectReveal>() == null);
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
            ObjectReveal.Play(cinemaLily, dissolveShader, duration, 0);
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
            cinemaLily.SetActive(true);
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
        private void OnDisable() { StopAllCoroutines(); RestoreMaterials(); }
        private void OnDestroy() => RestoreMaterials();
    }
}
