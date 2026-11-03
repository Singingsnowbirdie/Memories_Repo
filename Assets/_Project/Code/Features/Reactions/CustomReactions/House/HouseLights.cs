using System.Collections.Generic;
using System.Collections;
using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Gameplay
{
    /// <summary>
    /// Execute явно вызывается HighwayLights после завершения включения всех фонарей шоссе;
    /// при поздней регистрации на уже освещённом шоссе запускается сразу.
    /// Плавно доводит освещение дома до исходной яркости, сохраняя авторский набор включённых ламп.
    /// Если дом запущен отдельно без шоссе, Start сразу восстанавливает освещение через ShowImmediately.
    /// </summary>
    public sealed class HouseLights : CustomReaction
    {
        private Light[] lights;
        private bool[] initiallyEnabled;
        private float[] targetIntensities;
        private float fadeDuration = 4f;
        private Coroutine fade;
        private float brightness = -1;

        private void Awake()
        {
            // Floors can be separate scene roots, outside this controller's hierarchy.
            var sceneLights = new List<Light>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                sceneLights.AddRange(root.GetComponentsInChildren<Light>(true));
            lights = sceneLights.ToArray();
            initiallyEnabled = new bool[lights.Length];
            targetIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                initiallyEnabled[i] = lights[i].enabled;
                targetIntensities[i] = lights[i].intensity;
            }
            Apply(0);
        }

        private void Start()
        {
            // Bootstrap loads the highway first. An isolated house test has no highway.
            var highway = FindFirstObjectByType<HighwayLights>();
            if (highway == null) ShowImmediately();
            else highway.RegisterHouse(this);
        }

        public void Prepare(float duration)
        {
            if (fade != null) StopCoroutine(fade);
            fade = null;
            fadeDuration = Mathf.Max(.01f, duration);
            Apply(0);
        }

        public override void Execute()
        {
            if (fade == null && brightness < 1) fade = StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Apply(Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / fadeDuration)));
            }
            fade = null;
        }

        public void ShowImmediately()
        {
            if (fade != null) StopCoroutine(fade);
            fade = null;
            Apply(1);
        }

        private void Apply(float value)
        {
            if (value == brightness) return;
            brightness = value;
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null)
                {
                    lights[i].intensity = targetIntensities[i] * value;
                    lights[i].enabled = value > 0 && initiallyEnabled[i];
                }
        }
    }
}
