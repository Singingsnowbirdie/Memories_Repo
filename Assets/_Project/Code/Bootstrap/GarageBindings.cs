using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Bootstrap
{
    [AddComponentMenu("Memories/Locations/Garage Bindings")]
    public sealed class GarageBindings : LocationBindings
    {
        [field: Header("ВЫХОД В ШКОЛУ"), Space]
        [field: SerializeField, InspectorName("Гараж: соединение со школой")]
        public Transform SchoolConnection { get; private set; }

        [field: SerializeField]
        public Memories.Gameplay.Triggers.SceneTrigger GymEntrance { get; private set; }

        [field: SerializeField, InspectorName("Гараж: ворота")]
        public OpenGarageGateReaction GarageGate { get; private set; }

        [field: Header("ВХОД ИЗ ДВОРА"), Space]
        [field: SerializeField]
        public Memories.Gameplay.Triggers.SceneTrigger GarageEntrance { get; private set; }

        [field: SerializeField]
        public GameObject GarageSideDoor { get; private set; }

        [field: Header("ПЕРЕМЕННЫЕ"), Space]
        [field: SerializeField]
        public CustomVariableBank GarageVariables { get; private set; }

        [field: Header("МАШИНА"), Space]
        [field: SerializeField]
        public SetCarHoodReaction GarageHood { get; private set; }

        [field: Header("МАНЕКЕНЫ"), Space]
        [field: SerializeField]
        public GameObject StandingGarageFather { get; private set; }

        [field: SerializeField]
        public GameObject KneelingGarageFather { get; private set; }

        [field: SerializeField]
        public GameObject WorkingGarageFather { get; private set; }

        [field: SerializeField]
        public GameObject FinalGarageFather { get; private set; }

        [field: SerializeField]
        public GameObject HoodGarageFather { get; private set; }

        [field: Header("ПРЕДМЕТЫ"), Space]
        [field: SerializeField]
        public ObjectStateTracker Headlamp { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker Toolbox { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker CompressorPickup { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker VacuumHose { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker SiliconeSpray { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker PaperWipes { get; private set; }

        public ObjectStateTracker[] PickupItems => new[] { Headlamp, Toolbox, CompressorPickup, VacuumHose, SiliconeSpray, PaperWipes };

        [field: Header("СОЕДИНЕНИЕ СО ДВОРОМ"), Space]
        [field: SerializeField] public Transform ArrivalMarker { get; private set; }

        protected override void ValidateBindings()
        {
            Require(GarageEntrance, "вход в гараж");
            Require(GarageSideDoor, "боковая створка гаража");
            Require(StandingGarageFather, "стоящий отец в гараже");
            Require(KneelingGarageFather, "отец на коленях в гараже");
            Require(GarageVariables, "переменные гаража");
            Require(WorkingGarageFather, "работающий отец");
            Require(FinalGarageFather, "отец после финального воспоминания");
            Require(GarageHood, "car hood");
            Require(HoodGarageFather, "отец у закрытого капота");
            Require(GymEntrance, "переход в спортзал");
            Require(SchoolConnection, "соединение со школой");
            Require(GarageGate, "ворота гаража");
        }
    }
}
