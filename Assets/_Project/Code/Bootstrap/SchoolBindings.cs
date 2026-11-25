using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Bootstrap
{
    [AddComponentMenu("Memories/Locations/School Bindings")]
    public sealed class SchoolBindings : LocationBindings
    {
        [field: Header("ВХОД В СПОРТЗАЛ"), Space]
        [field: SerializeField, InspectorName("Школа: вход в спортзал")]
        public Memories.Gameplay.Triggers.SceneTrigger GymEntrance { get; private set; }

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
        public ObjectStateTracker GarageCorridor { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker GarageOpeningLeft { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker GarageOpeningRight { get; private set; }

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

        [field: SerializeField]
        public ObjectStateTracker Classroom102Door { get; private set; }


        [field: Header("СОЕДИНЕНИЕ С ГАРАЖОМ"), Space]
        [field: SerializeField] public Transform ArrivalMarker { get; private set; }
        [field: Header("ДВЕРЬ КЛАССА"), Space]
        [field: SerializeField] public Transform ClassroomDoorHinge { get; private set; }

        protected override void ValidateBindings()
        {
            Require(GymEntrance, "вход в спортзал");
        }
    }
}
