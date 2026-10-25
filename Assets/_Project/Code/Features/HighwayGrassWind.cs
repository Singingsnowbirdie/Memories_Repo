using UnityEngine;

namespace Memories.Gameplay
{
    public sealed class HighwayGrassWind : MonoBehaviour
    {
        [SerializeField] private Transform[] plants = new Transform[0];
        [SerializeField, Range(0, 5)] private float angle = 1.8f;
        [SerializeField, Min(0.1f)] private float period = 5f;
        private Quaternion[] restRotations;
        private float[] phases;
        private float elapsed;

        private void Awake()
        {
            restRotations = new Quaternion[plants.Length];
            phases = new float[plants.Length];
            for (int i = 0; i < plants.Length; i++)
            {
                if (plants[i] == null) continue;
                restRotations[i] = plants[i].localRotation;
                Vector3 position = plants[i].position;
                phases[i] = position.x * 0.73f + position.z * 0.39f;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float time = elapsed * 2f * Mathf.PI / Mathf.Max(0.1f, period);
            float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed));
            for (int i = 0; i < plants.Length; i++)
            {
                var plant = plants[i];
                if (plant == null || !plant.gameObject.activeInHierarchy) continue;
                float phase = time + phases[i];
                float sway = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 1.37f) * 0.25f;
                plant.localRotation = restRotations[i] * Quaternion.Euler(
                    angle * fade * 0.35f * Mathf.Sin(phase * 0.79f), 0, angle * fade * sway);
            }
        }

        private void OnDisable()
        {
            elapsed = 0;
            if (restRotations == null) return;
            for (int i = 0; i < plants.Length; i++)
                if (plants[i] != null) plants[i].localRotation = restRotations[i];
        }
    }
}
