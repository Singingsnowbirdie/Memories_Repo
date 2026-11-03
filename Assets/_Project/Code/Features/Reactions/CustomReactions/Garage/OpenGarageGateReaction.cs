using System.Collections;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графом OpenGarageGate после взаимодействия игрока с гаражными воротами.
    /// Плавно поднимает полотно в заданную позицию и поворачивает его под потолок на 90 градусов.
    /// Повторный запуск во время подъёма и после открытия игнорируется.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OpenGarageGateReaction : CustomReaction
    {
        [SerializeField] private Transform panel;
        [SerializeField, Min(.1f)] private float duration = 3.5f;
        [SerializeField] private Vector3 raisedPosition = new Vector3(0, 3.35f, 1.65f);
        private bool raising;
        public bool IsOpen { get; private set; }

        public override void Execute()
        {
            if (panel == null || raising || IsOpen) return;
            raising = true;
            StartCoroutine(Raise());
        }

        private IEnumerator Raise()
        {
            var position = panel.localPosition;
            var rotation = panel.localRotation;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.1f, duration)));
                panel.localPosition = Vector3.Lerp(position, raisedPosition, t);
                panel.localRotation = rotation * Quaternion.Euler(90 * t, 0, 0);
                yield return null;
            }
            IsOpen = true;
            raising = false;
        }
    }
}
