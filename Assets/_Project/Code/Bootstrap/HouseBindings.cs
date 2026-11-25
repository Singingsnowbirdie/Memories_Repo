using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Bootstrap
{
    [AddComponentMenu("Memories/Locations/House Bindings")]
    public sealed class HouseBindings : LocationBindings
    {
        [field: Header("ПЕРЕХОД ВО ДВОР"), Space]
        [field: SerializeField, InspectorName("Дом: загрузка двора")]
        public Transform CourtyardConnection { get; private set; }

        [field: Header("ВОССТАНОВЛЕНИЕ СОСТОЯНИЯ"), Space]
        [field: SerializeField]
        public ObjectStateTracker EntranceOpen { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker EntranceClosed { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker StairOpen { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker StairClosed { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker CourtyardOpen { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker CourtyardClosed { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker CourtyardPlaceholders { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker Lighting { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker TvStaticScreen { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker TvBaseballScreen { get; private set; }

        [field: SerializeField]
        public ObjectStateTracker RemoteInHand { get; private set; }

        [field: SerializeField]
        public GameObject CourtyardCoverRoot { get; private set; }

        [field: Header("ВХОД В ДОМ"), Space]
        [field: SerializeField] public Transform EntranceFrame { get; private set; }

        protected override void ValidateBindings()
        {
            Require(CourtyardConnection, "дверь во двор");
            Require(CourtyardCoverRoot, "перекрытие прохода во двор");
        }
    }
}
