using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Memories.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.Rendering;

namespace Memories.Gameplay.Reactions
{
    // Используем только временные материалы во время игры: исходные ресурсы и состояние рендереров восстанавливаются.
    [DisallowMultipleComponent]
    public sealed class ObjectReveal : MonoBehaviour
    {
        readonly List<(Renderer renderer, Material[] materials, ShadowCastingMode shadows)> renderers = new();
        readonly List<Material> temporary = new();
        readonly List<(Collider collider, bool enabled)> colliders = new();
        readonly List<(ReactionInteractable item, bool enabled)> interactions = new();
        bool restored;

        public static void Play(GameObject target, Shader shader, float duration, float delay)
        {
            if (target.TryGetComponent<ObjectReveal>(out _)) { target.SetActive(true); return; }
            var effect = target.AddComponent<ObjectReveal>();
            effect.Prepare(shader);
            target.SetActive(true);
            // OnEnable может настроить интерактивный объект, поэтому блокируем ввод и после активации.
            foreach (var state in effect.interactions) if (state.item != null) state.item.enabled = false;
            effect.StartCoroutine(effect.Animate(Mathf.Max(.01f, duration), Mathf.Max(0, delay)));
        }

        void Prepare(Shader shader)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var originals = renderer.sharedMaterials;
                renderers.Add((renderer, originals, renderer.shadowCastingMode));
                var replacements = originals.Select(original =>
                {
                    if (original == null) return null;
                    var material = new Material(original) { shader = shader, hideFlags = HideFlags.HideAndDontSave };
                    material.SetFloat("_Reveal", 0);
                    temporary.Add(material);
                    return material;
                }).ToArray();
                renderer.sharedMaterials = replacements;
                // Не допускаем сплошной тени до появления самого объекта.
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var c in GetComponentsInChildren<Collider>(true)) { colliders.Add((c, c.enabled)); c.enabled = false; }
            foreach (var item in GetComponentsInChildren<ReactionInteractable>(true))
            { interactions.Add((item, item.enabled)); item.enabled = false; }
        }

        IEnumerator Animate(float duration, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            float elapsed = 0;
            while (elapsed < duration)
            {
                float value = Mathf.SmoothStep(0, 1, elapsed / duration);
                foreach (var material in temporary) material.SetFloat("_Reveal", value);
                yield return null;
                elapsed += Time.deltaTime;
            }
            Finish();
        }

        void Finish()
        {
            StopAllCoroutines();
            Restore();
            Destroy(this);
        }

        void Restore()
        {
            if (restored) return;
            restored = true;
            foreach (var state in renderers) if (state.renderer != null)
            { state.renderer.sharedMaterials = state.materials; state.renderer.shadowCastingMode = state.shadows; }
            foreach (var state in colliders) if (state.collider != null) state.collider.enabled = state.enabled;
            foreach (var state in interactions) if (state.item != null) state.item.enabled = state.enabled;
            foreach (var material in temporary) if (material != null)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }

        void OnDisable() { StopAllCoroutines(); Restore(); if (Application.isPlaying) Destroy(this); }
        void OnDestroy() => Restore();
    }
}
