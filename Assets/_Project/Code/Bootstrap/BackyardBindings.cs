using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Bootstrap
{
    [AddComponentMenu("Memories/Locations/Backyard Bindings")]
    public sealed class BackyardBindings : LocationBindings
    {
        [field: Header("ВХОД ИЗ ДОМА"), Space]
        [field: SerializeField, InspectorName("Двор: вход")] public Memories.Gameplay.Triggers.SceneTrigger BackyardEntrance { get; private set; }
        [field: SerializeField, InspectorName("Двор: освещение входа")] public Transform BackyardEntranceLighting { get; private set; }


        [field: Header("ПЕРЕХОД В ГАРАЖ"), Space]
        [field: SerializeField, InspectorName("Двор: соединение с гаражом")] public Transform GarageAttachment { get; private set; }


        [field: SerializeField, InspectorName("Двор: секции забора перед гаражом")] public GameObject[] GarageFenceSections { get; private set; }


        [field: Header("ПЕРЕМЕННЫЕ"), Space]
        [field: SerializeField] public CustomVariableBank BackyardVariables { get; private set; }


        [field: Header("БЕЙСБОЛЬНАЯ ПЛОЩАДКА"), Space]
        [field: SerializeField] public Memories.Gameplay.Interaction.QuestItem BaseballBat { get; private set; }
        [field: SerializeField] public GameObject BaseballAreaNormal { get; private set; }
        [field: SerializeField] public GameObject BaseballAreaReady { get; private set; }


        [field: Header("ОКНО"), Space]
        [field: SerializeField] public GameObject IntactBaseballWindow { get; private set; }
        [field: SerializeField] public GameObject BrokenBaseballWindow { get; private set; }

        [field: Header("МАНЕКЕНЫ"), Space]
        [field: SerializeField] public GameObject BenPitcher { get; private set; }
        [field: SerializeField] public GameObject SeatedBen { get; private set; }


        [field: Header("СОЕДИНЕНИЕ С ДОМОМ"), Space]
        [field: SerializeField] public Transform ArrivalMarker { get; private set; }
        [field: SerializeField] public Transform[] ConnectionIslands { get; private set; }
        [field: SerializeField] public Transform[] ConnectionHouses { get; private set; }

        protected override void ValidateBindings()
        {
            Require(BenPitcher, "Бен-питчер");
            Require(SeatedBen, "сидящий Бен");
            Require(BackyardVariables, "переменные двора");
            Require(BaseballBat, "бита");
            Require(BaseballAreaNormal, "обычная площадка");
            Require(BaseballAreaReady, "готовая площадка");
            Require(IntactBaseballWindow, "целое окно");
            Require(BrokenBaseballWindow, "разбитое окно");
            Require(BackyardEntrance, "вход во двор");
            Require(BackyardEntranceLighting, "освещение входа во двор");
            Require(GarageAttachment, "соединение с гаражом");
        }
    }
}
