using System.Collections;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графом OpenClassroomDoor после разрешённого взаимодействия игрока с дверью класса.
    /// Плавно поворачивает дверь на петлях на 100 градусов.
    /// Повторный запуск во время открытия и после него игнорируется.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OpenClassroomDoorReaction : CustomReaction
    {
        [SerializeField] private Transform hinge;
        [SerializeField, Min(.1f)] private float duration = 1;
        private bool opening;
        public bool IsOpen { get; private set; }

        public override void Execute()
        {
            if (hinge == null || opening || IsOpen) return;
            opening = true;
            StartCoroutine(Open());
        }

        private IEnumerator Open()
        {
            var rotation = hinge.localRotation;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.1f, duration)));
                hinge.localRotation = rotation * Quaternion.Euler(0, 100 * t, 0);
                yield return null;
            }
            IsOpen = true;
            opening = false;
        }
    }
}
