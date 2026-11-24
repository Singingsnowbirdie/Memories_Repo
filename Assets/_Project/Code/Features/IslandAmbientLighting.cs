using UnityEngine;
using UnityEngine.Rendering;

namespace Memories.Gameplay
{
    // Local diffuse sky fill for distant islands, retaining their original shaders.
    public sealed class IslandAmbientLighting : MonoBehaviour
    {
        [SerializeField] private Renderer[] targets = new Renderer[0];
        private MaterialPropertyBlock[] originalBlocks;
        private LightProbeUsage[] originalProbeUsage;
        private readonly SphericalHarmonicsL2[] probe = new SphericalHarmonicsL2[1];
        private MaterialPropertyBlock block;

        private void OnEnable()
        {
            block = new MaterialPropertyBlock();
            originalBlocks = new MaterialPropertyBlock[targets.Length];
            originalProbeUsage = new LightProbeUsage[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                originalBlocks[i] = new MaterialPropertyBlock();
                targets[i].GetPropertyBlock(originalBlocks[i]);
                originalProbeUsage[i] = targets[i].lightProbeUsage;
            }
            LateUpdate();
        }

        private void LateUpdate()
        {
            // Follow the current ambient light, including the highway's reveal.
            // A black ambient probe produces no fill: there is no self-illumination.
            var ambient = RenderSettings.ambientProbe;
            probe[0] = ambient * 0.65f;
            var color = new Color(Mathf.Max(0, ambient[0, 0]),
                Mathf.Max(0, ambient[1, 0]), Mathf.Max(0, ambient[2, 0]));
            probe[0].AddDirectionalLight(new Vector3(0.6f, 0.8f, 0.35f).normalized,
                color, 0.8f);
            foreach (var target in targets)
            {
                if (target == null) continue;
                target.lightProbeUsage = LightProbeUsage.CustomProvided;
                target.GetPropertyBlock(block);
                block.CopySHCoefficientArraysFrom(probe);
                target.SetPropertyBlock(block);
            }
        }

        private void OnDisable()
        {
            if (originalBlocks == null) return;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || originalBlocks[i] == null) continue;
                targets[i].lightProbeUsage = originalProbeUsage[i];
                targets[i].SetPropertyBlock(originalBlocks[i]);
            }
        }
    }
}
