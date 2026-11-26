using System;
using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Bootstrap
{
    [AddComponentMenu("Memories/Locations/Park Bindings")]
    public sealed class ParkBindings : LocationBindings
    {
        [Serializable]
        public sealed class ParkCinemaBindings
        {
            [Header("ПЕРСОНАЖИ"), Space]
            public GameObject lily, cashier;
            public GameObject entranceLily;
            [Header("ОСВЕЩЕНИЕ ПОДХОДА"), Space]
            public LightingGroup approachLighting;
            [Header("ПРЕДМЕТЫ"), Space]
            public Memories.Gameplay.Interaction.QuestItem tickets, bouquetItem;
            [Header("ОСВЕЩЕНИЕ КИНОТЕАТРА И СКАМЕЙКИ"), Space]
            public LightingGroup cinemaLighting, benchLighting;
        }
        [field: Header("КИНОТЕАТР"), Space]
        [field: SerializeField]
        public ParkCinemaBindings ParkCinema { get; private set; }

        [Serializable]
        public sealed class ParkMimeBindings
        {
            [Header("ПЕРСОНАЖИ"), Space]
            public GameObject lily, cafeLily;
            [Header("ПРЕДМЕТЫ И ВЗАИМОДЕЙСТВИЕ"), Space]
            public Memories.Gameplay.Interaction.ReactionInteractable suitcase;
            public Memories.Gameplay.Interaction.QuestItem pendants, tickets;
            [Header("ОСВЕЩЕНИЕ"), Space]
            public LightingGroup lighting, cafeLighting;
        }
        [field: Header("МИМЫ"), Space]
        [field: SerializeField]
        public ParkMimeBindings ParkMimes { get; private set; }

        [Serializable]
        public sealed class ParkCafeBindings
        {
            [Header("ПЕРЕМЕННЫЕ КАФЕ"), Space]
            public CustomVariableBank variables;
            [Header("ЗАКАЗ И ВЗАИМОДЕЙСТВИЕ"), Space]
            public Memories.Gameplay.Interaction.QuestItem receipt;
            public Memories.Gameplay.Interaction.ReactionInteractable lily, truck, coffeeOne, coffeeTwo, napkin;
            public GameObject order;
            [Header("ОСВЕЩЕНИЕ КАФЕ И ОЗЕРА"), Space]
            public LightingGroup lighting;
            public LightingGroup lakeLighting;
            [Header("ПРИЧАЛ"), Space]
            public CustomVariableBank dockVariables;
            public Memories.Gameplay.Interaction.ReactionInteractable dockLily, dockBoat;
            [Header("БЕСЕДКА"), Space]
            public Memories.Gameplay.Interaction.ReactionInteractable gazeboLily;
            public LightingGroup gazeboLighting;
            [Header("ВЫХОД ИЗ ПАРКА"), Space]
            public Memories.Gameplay.Interaction.ReactionInteractable exitLily, exitBin;
            public Memories.Gameplay.Interaction.ReactionInteractable[] bouquetBins;
            public LightingGroup exitLighting;
            public Memories.Gameplay.Interaction.QuestItem returnedBouquet;
            public GameObject discardedBouquet;
            public OpenParkExitGateReaction exitGate;
        }
        [field: Header("КАФЕ И ПРОГУЛКА"), Space]
        [field: SerializeField]
        public ParkCafeBindings ParkCafe { get; private set; }
    }
}
