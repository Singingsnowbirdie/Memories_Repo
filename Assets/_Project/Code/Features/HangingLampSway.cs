using UnityEngine;

namespace Memories.Gameplay
{
    public sealed class HangingLampSway : MonoBehaviour
    {
        [SerializeField, Range(0, 15)] private float angle = 5;
        [SerializeField, Min(0.1f)] private float period = 4.5f;
        [SerializeField, Range(0, 360)] private float phaseOffset;
        private Quaternion rest;
        private float elapsed;

        private void Awake() => rest = transform.localRotation;

        private void Update()
        {
            elapsed += Time.deltaTime;
            float phase = elapsed * 2 * Mathf.PI / period + phaseOffset * Mathf.Deg2Rad;
            transform.localRotation = rest * Quaternion.Euler(
                angle * 0.35f * Mathf.Sin(phase * 0.73f), 0, angle * Mathf.Sin(phase));
        }
    }
}
