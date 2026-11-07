using System.Collections;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class ToggleMedicineCabinetReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private float openAngle = 105f;
        [SerializeField, Min(.1f)] private float duration = .6f;
        private bool open;
        private Coroutine doorRoutine;

        public string StateKey => "medicineCabinetDoor";
        public string CaptureState() => open ? "open" : "closed";

        public override void Execute()
        {
            if (hinge == null || doorRoutine != null) return;
            open = !open;
            GetComponent<ObjectStateTracker>()?.Capture();
            doorRoutine = StartCoroutine(Animate());
        }

        public void RestoreState(string state)
        {
            if (doorRoutine != null) StopCoroutine(doorRoutine);
            doorRoutine = null;
            open = state == "open";
            if (hinge != null) hinge.localRotation = TargetRotation;
        }

        private Quaternion TargetRotation => Quaternion.Euler(0, open ? openAngle : 0, 0);

        private IEnumerator Animate()
        {
            var start = hinge.localRotation;
            var target = TargetRotation;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(start, target,
                    Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(.1f, duration)));
                yield return null;
            }
            hinge.localRotation = target;
            doorRoutine = null;
        }

        private void OnDisable()
        {
            if (doorRoutine != null) StopCoroutine(doorRoutine);
            doorRoutine = null;
            if (hinge != null) hinge.localRotation = TargetRotation;
        }
    }
}
