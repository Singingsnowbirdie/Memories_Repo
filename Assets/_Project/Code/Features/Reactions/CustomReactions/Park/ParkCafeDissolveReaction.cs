using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Graphs own progression, inventory, visibility and lighting. This reaction only animates dissolution.
    public sealed class ParkCafeDissolveReaction : CustomReaction
    {
        [SerializeField] private GameObject target;
        [SerializeField] private Shader dissolveShader;
        [SerializeField] private float duration = 4;
        [SerializeField] private bool restoresCafe;
        [SerializeField] private CustomVariableBank variables;
        [SerializeField] private QuestItem receipt;
        [SerializeField] private ReactionInteractable lily, truck, coffeeOne, coffeeTwo, napkin;
        [SerializeField] private GameObject order;
        [SerializeField] private LightingGroup lighting;
        [SerializeField] private LightingGroup lakeLighting;
        [SerializeField] private CustomVariableBank dockVariables;
        [SerializeField] private ReactionInteractable dockLily, dockBoat;

        public void RestoreAfterLoad(CustomVariableStore state, QuestInventory inventory)
        {
            if (!restoresCafe) return;
            bool arrived = lily.gameObject.activeSelf;
            bool bought = state.Get(variables, "ordered") || inventory.WasCollected(receipt.Id);
            bool coffeeRead = state.Get(variables, "coffee_read");
            bool napkinRead = state.Get(variables, "napkin_read");
            bool complete = state.Get(variables, "completed") || (coffeeRead && napkinRead);
            if (bought) state.Set(variables, "ordered", true);
            if (complete) state.Set(variables, "completed", true);
            lily.gameObject.SetActive(arrived && !bought);
            // Hidden objects stay ready for their explicit reveal graph; progress controls interaction.
            lily.SetInteractionEnabled(!bought && !complete && !state.Get(variables, "unlocked"));
            truck.SetInteractionEnabled(!bought && !complete && state.Get(variables, "unlocked"));
            order.SetActive(bought && !complete);
            coffeeOne.SetInteractionEnabled(!complete && !coffeeRead);
            coffeeTwo.SetInteractionEnabled(!complete && !coffeeRead);
            napkin.SetInteractionEnabled(!complete && !napkinRead);
            lighting.RestoreBrightness((arrived || bought) && !complete ? 1 : 0);
            if (lakeLighting != null) lakeLighting.RestoreBrightness(complete ? 1 : 0);
            if (dockLily != null && dockBoat != null && dockVariables != null)
            {
                bool talked = state.Get(dockVariables, "lily_spoken");
                dockLily.gameObject.SetActive(complete);
                dockLily.SetInteractionEnabled(!talked);
                dockBoat.SetInteractionEnabled(complete && talked);
            }
        }

        public override void Execute() => ExecuteAsync(destroyCancellationToken).Forget();
        public override async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            var originals = new List<(Renderer renderer, Material[] materials)>();
            var temporary = new List<Material>();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
                {
                    var source = renderer.sharedMaterials;
                    originals.Add((renderer, source));
                    var replacements = new Material[source.Length];
                    for (int i = 0; i < source.Length; i++)
                    {
                        if (!source[i]) continue;
                        var material = new Material(source[i]) { shader = dissolveShader };
                        material.SetFloat("_Reveal", 1);
                        replacements[i] = material;
                        temporary.Add(material);
                    }
                    renderer.sharedMaterials = replacements;
                }
                for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
                {
                    foreach (var material in temporary)
                        material.SetFloat("_Reveal", 1 - Mathf.SmoothStep(0, 1, elapsed / duration));
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
                foreach (var entry in originals) if (entry.renderer) entry.renderer.enabled = false;
            }
            finally
            {
                foreach (var entry in originals) if (entry.renderer) entry.renderer.sharedMaterials = entry.materials;
                foreach (var material in temporary) if (material) Destroy(material);
            }
        }
    }
}
