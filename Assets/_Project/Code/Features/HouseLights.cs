using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay
{
    public sealed class HouseLights : MonoBehaviour
    {
        private Light[] lights;
        private bool[] initiallyEnabled;
        private float[] targetIntensities;
        private HighwayLights highway;
        private float fadeElapsed;
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
            highway = FindFirstObjectByType<HighwayLights>();
            Apply(highway == null ? 1 : 0);
        }

        private void Update()
        {
            if (highway == null)
            {
                Apply(1);
                return;
            }
            fadeElapsed = highway.IsFullyLit ? fadeElapsed + Time.deltaTime : 0;
            Apply(Mathf.SmoothStep(0, 1, Mathf.Clamp01(fadeElapsed / highway.FadeDuration)));
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
