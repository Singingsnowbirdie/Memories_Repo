using UnityEngine;

namespace Memories.Gameplay
{
    public sealed class FloatingScenery : MonoBehaviour
    {
        [SerializeField] private Transform[] objects = new Transform[0];
        [SerializeField, Min(0)] private float bobHeight = 0.25f;
        [SerializeField, Min(1)] private float period = 14f;
        [SerializeField, Range(0, 10)] private float tiltAngle = 2f;
        [SerializeField, Range(0, 30)] private float turnAngle = 6f;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private float[] phases;
        private float elapsed;

        private void Awake()
        {
            positions = new Vector3[objects.Length];
            rotations = new Quaternion[objects.Length];
            phases = new float[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;
                positions[i] = objects[i].localPosition;
                rotations[i] = objects[i].localRotation;
                phases[i] = Mathf.Repeat(positions[i].x * 0.37f + positions[i].z * 0.19f, 2f * Mathf.PI);
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float time = elapsed * 2f * Mathf.PI / Mathf.Max(1f, period);
            float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 3f));
            for (int i = 0; i < objects.Length; i++)
            {
                var target = objects[i];
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                float phase = phases[i];
                float wave = time * (0.85f + 0.3f * phase / (2f * Mathf.PI));
                target.localPosition = positions[i] + Vector3.up * (bobHeight * fade * Mathf.Sin(wave + phase));
                target.localRotation = rotations[i] * Quaternion.Euler(
                    tiltAngle * fade * Mathf.Sin(wave * 0.53f + phase),
                    turnAngle * fade * Mathf.Sin(wave * 0.31f + phase),
                    tiltAngle * fade * 0.7f * Mathf.Sin(wave * 0.41f - phase));
            }
        }

        private void OnDisable()
        {
            elapsed = 0;
            if (positions == null) return;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;
                objects[i].localPosition = positions[i];
                objects[i].localRotation = rotations[i];
            }
        }
    }
}
