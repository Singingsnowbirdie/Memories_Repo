using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Bootstrap
{
    [AddComponentMenu("Memories/Locations/School Bindings")]
    public sealed class SchoolBindings : LocationBindings
    {
        [field: Header("ВОССТАНОВЛЕНИЕ СОСТОЯНИЯ"), Space]
        [field: SerializeField]
        public ObjectStateTracker GymDoorClosed { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker GymDoorOpen { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker EnvironmentCorridor { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker EnvironmentLockerRooms { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker AmbulanceBeacons { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker YardAnimation { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker CourtyardLeftClosed { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker CourtyardLeftOpen { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker CourtyardRightClosed { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker CourtyardRightOpen { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker Ambulance { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker AmbulanceDoctor { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker Giles { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker InteriorAnimation { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker Payphone { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker VendingCoin { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker Coach { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker SpectatorCommentator { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker Lily { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker FirstAidCabinet { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker IceContainer { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker TeacherRuler { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker TissueBox { get; private set; }



        [field: Header("СОЕДИНЕНИЕ С ГАРАЖОМ"), Space]
        [field: SerializeField] public Transform ArrivalMarker { get; private set; }

        protected override void ValidateBindings()
        {
            Require(ArrivalMarker, "соединение с гаражом");
        }
    }
}
