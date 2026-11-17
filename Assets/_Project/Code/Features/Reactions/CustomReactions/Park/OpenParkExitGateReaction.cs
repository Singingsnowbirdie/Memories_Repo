using System.Collections;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // The bin graph starts this animation explicitly. Loading restores its final pose separately.
    public sealed class OpenParkExitGateReaction : CustomReaction
    {
        [SerializeField] private Transform leftLeaf;
        [SerializeField] private Transform rightLeaf;
        [SerializeField, Min(.1f)] private float duration = 4f;
        private bool opening;

        public override void Execute()
        {
            if (opening || leftLeaf == null || rightLeaf == null) return;
            opening = true;
            StartCoroutine(Open());
        }

        public void RestoreOpen(bool open)
        {
            StopAllCoroutines();
            opening = open;
            if (leftLeaf != null) leftLeaf.localRotation = Quaternion.Euler(0, open ? -100 : 0, 0);
            if (rightLeaf != null) rightLeaf.localRotation = Quaternion.Euler(0, open ? 100 : 0, 0);
        }

        private IEnumerator Open()
        {
            var left = leftLeaf.localRotation;
            var right = rightLeaf.localRotation;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.1f, duration)));
                leftLeaf.localRotation = Quaternion.Slerp(left, Quaternion.Euler(0, -100, 0), t);
                rightLeaf.localRotation = Quaternion.Slerp(right, Quaternion.Euler(0, 100, 0), t);
                yield return null;
            }
        }
    }
}
