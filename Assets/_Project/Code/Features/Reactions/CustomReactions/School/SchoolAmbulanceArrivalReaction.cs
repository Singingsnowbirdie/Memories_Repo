using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Воспоминание у телефона явно открывает двор и показывает ожидающую скорую помощь.
    public sealed class SchoolAmbulanceArrivalReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private Transform[] hinges;
        [SerializeField] private Vector3[] openAngles;
        [SerializeField] private ReactionInteractable ambulance;
        [SerializeField] private GameObject doctor;
        [SerializeField] private ReactionInteractable coach;
        private bool opened;
        public string StateKey => "school-ambulance-arrival";
        public override void Execute()
        {
            opened = true;
            OpenDoors();
            GetComponent<ReactionInteractable>().SetInteractionEnabled(false);
            ambulance.SetInteractionEnabled(true);
            ambulance.gameObject.SetActive(true);
            doctor.SetActive(true);
            ambulance.GetComponent<ObjectStateTracker>()?.Capture();
            doctor.GetComponent<ObjectStateTracker>()?.Capture();
            GetComponent<ObjectStateTracker>()?.Capture();
        }
        private void OpenDoors()
        {
            for (int i = 0; i < hinges.Length; i++) hinges[i].localRotation = Quaternion.Euler(openAngles[i]);
            Physics.SyncTransforms();
        }
        public string CaptureState() => opened ? "open" : "closed";
        public void RestoreState(string value)
        {
            opened = value == "open";
            if (opened) OpenDoors();
        }
    }
}
