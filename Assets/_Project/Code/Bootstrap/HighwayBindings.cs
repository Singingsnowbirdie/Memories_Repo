using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Bootstrap
{
    [AddComponentMenu("Memories/Locations/Highway Bindings")]
    public sealed class HighwayBindings : LocationBindings
    {
        [field: Header("ПЕРЕМЕННЫЕ"), Space]
        [field: SerializeField]
        public CustomVariableBank HighwayVariables { get; private set; }

        [field: Header("ВСТУПЛЕНИЕ"), Space]
        [field: SerializeField]
        public ReactionGraphBinding HighwayIntro { get; private set; }

        protected override void ValidateBindings()
        {
            Require(HighwayVariables, "переменные вступления шоссе");
            Require(HighwayIntro, "вступление шоссе");
        }
    }
}
