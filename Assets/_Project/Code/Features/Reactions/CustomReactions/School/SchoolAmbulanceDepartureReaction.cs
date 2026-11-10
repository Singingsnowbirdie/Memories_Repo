using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class SchoolAmbulanceDepartureReaction : CustomReaction
    {
        [SerializeField] private GameObject doctor;
        public override void Execute()
        {
            doctor.SetActive(false);
            doctor.GetComponent<ObjectStateTracker>()?.Capture();
            gameObject.SetActive(false);
            GetComponent<ObjectStateTracker>()?.Capture();
        }
    }
}
