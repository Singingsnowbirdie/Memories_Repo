using System;
using System.Collections.Generic;
using Memories.Gameplay.Reactions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Locations/Конфигурация связей локации")]
    public sealed class LocationBindings : MonoBehaviour
    {
        [SerializeField] private CustomVariableBank highwayVariables;
        [SerializeField] private ReactionGraphBinding highwayIntro;
        public CustomVariableBank HighwayVariables => Require(highwayVariables, "переменные вступления шоссе");
        public ReactionGraphBinding HighwayIntro => Require(highwayIntro, "вступление шоссе");
        [SerializeField, InspectorName("Дом: загрузка двора")] private Transform courtyardConnection;
        [SerializeField, InspectorName("Двор: вход")] private Memories.Gameplay.Triggers.SceneTrigger backyardEntrance;
        [SerializeField, InspectorName("Двор: освещение входа")] private Transform backyardEntranceLighting;
        [SerializeField, InspectorName("Двор: соединение с гаражом")] private Transform garageAttachment;
        [SerializeField, InspectorName("Двор: секции забора перед гаражом")] private GameObject[] garageFenceSections;
        [SerializeField, InspectorName("Гараж: соединение со школой")] private Transform schoolConnection;
        [SerializeField, InspectorName("Гараж: ворота")] private OpenGarageGateReaction garageGate;
        [SerializeField, InspectorName("Школа: вход в спортзал")] private Memories.Gameplay.Triggers.SceneTrigger gymEntrance;

        [SerializeField] private CustomVariableBank backyardVariables;
        [SerializeField] private Memories.Gameplay.Interaction.QuestItem baseballBat;
        [SerializeField] private GameObject baseballAreaNormal, baseballAreaReady, intactBaseballWindow, brokenBaseballWindow;
        [SerializeField] private GameObject benPitcher, seatedBen;
        public GameObject BenPitcher => Require(benPitcher, "Бен-питчер");
        public GameObject SeatedBen => Require(seatedBen, "сидящий Бен");
        public CustomVariableBank BackyardVariables => Require(backyardVariables, "переменные двора");
        public Memories.Gameplay.Interaction.QuestItem BaseballBat => Require(baseballBat, "бита");
        public GameObject BaseballAreaNormal => Require(baseballAreaNormal, "обычная площадка");
        public GameObject BaseballAreaReady => Require(baseballAreaReady, "готовая площадка");
        public GameObject IntactBaseballWindow => Require(intactBaseballWindow, "целое окно");
        public GameObject BrokenBaseballWindow => Require(brokenBaseballWindow, "разбитое окно");

        [SerializeField] private Memories.Gameplay.Triggers.SceneTrigger garageEntrance;
        [SerializeField] private GameObject garageSideDoor;
        public Memories.Gameplay.Triggers.SceneTrigger GarageEntrance => Require(garageEntrance, "вход в гараж");
        public GameObject GarageSideDoor => Require(garageSideDoor, "боковая створка гаража");

        [SerializeField] private GameObject standingGarageFather, kneelingGarageFather;
        public GameObject StandingGarageFather => Require(standingGarageFather, "стоящий отец в гараже");
        public GameObject KneelingGarageFather => Require(kneelingGarageFather, "отец на коленях в гараже");

        [SerializeField] private CustomVariableBank garageVariables;
        [SerializeField] private GameObject workingGarageFather, finalGarageFather;
        public CustomVariableBank GarageVariables => Require(garageVariables, "переменные гаража");
        public GameObject WorkingGarageFather => Require(workingGarageFather, "работающий отец");
        public GameObject FinalGarageFather => Require(finalGarageFather, "отец после финального воспоминания");

        [SerializeField] private SetCarHoodReaction garageHood;
        public SetCarHoodReaction GarageHood => Require(garageHood, "car hood");

        [SerializeField] private GameObject hoodGarageFather;
        public GameObject HoodGarageFather => Require(hoodGarageFather, "отец у закрытого капота");

        [Serializable]
        public sealed class ParkCinemaBindings
        {
            public GameObject lily, cashier;
            public GameObject entranceLily;
            public LightingGroup approachLighting;
            public Memories.Gameplay.Interaction.QuestItem tickets, bouquetItem;
            public LightingGroup cinemaLighting, benchLighting;
        }
        [SerializeField] private ParkCinemaBindings parkCinema;
        public ParkCinemaBindings ParkCinema => parkCinema;

        [Serializable]
        public sealed class ParkMimeBindings
        {
            public GameObject lily, cafeLily;
            public Memories.Gameplay.Interaction.ReactionInteractable suitcase;
            public Memories.Gameplay.Interaction.QuestItem pendants, tickets;
            public LightingGroup lighting, cafeLighting;
        }
        [SerializeField] private ParkMimeBindings parkMimes;
        public ParkMimeBindings ParkMimes => parkMimes;

        [Serializable]
        public sealed class ParkCafeBindings
        {
            public CustomVariableBank variables;
            public Memories.Gameplay.Interaction.QuestItem receipt;
            public Memories.Gameplay.Interaction.ReactionInteractable lily, truck, coffeeOne, coffeeTwo, napkin;
            public GameObject order;
            public LightingGroup lighting;
            public LightingGroup lakeLighting;
            public CustomVariableBank dockVariables;
            public Memories.Gameplay.Interaction.ReactionInteractable dockLily, dockBoat;
            public Memories.Gameplay.Interaction.ReactionInteractable gazeboLily;
            public LightingGroup gazeboLighting;
            public Memories.Gameplay.Interaction.ReactionInteractable exitLily, exitBin;
            public LightingGroup exitLighting;
            public Memories.Gameplay.Interaction.QuestItem returnedBouquet;
            public GameObject discardedBouquet;
            public OpenParkExitGateReaction exitGate;
        }
        [SerializeField] private ParkCafeBindings parkCafe;
        public ParkCafeBindings ParkCafe => parkCafe;

        private static readonly Dictionary<int, HashSet<LocationBindings>> registered = new Dictionary<int, HashSet<LocationBindings>>();
        private int registeredScene;

        public Transform CourtyardConnection => Require(courtyardConnection, "дверь во двор");
        public Memories.Gameplay.Triggers.SceneTrigger BackyardEntrance => Require(backyardEntrance, "вход во двор");
        public Transform BackyardEntranceLighting => Require(backyardEntranceLighting, "освещение входа во двор");
        public Transform GarageAttachment => Require(garageAttachment, "соединение с гаражом");
        public GameObject[] GarageFenceSections => garageFenceSections;
        public Transform SchoolConnection => Require(schoolConnection, "соединение со школой");
        public OpenGarageGateReaction GarageGate => Require(garageGate, "ворота гаража");
        public Memories.Gameplay.Triggers.SceneTrigger GymEntrance => Require(gymEntrance, "вход в спортзал");

        private void OnEnable()
        {
            registeredScene = gameObject.scene.handle;
            if (!registered.TryGetValue(registeredScene, out var configurations))
                registered.Add(registeredScene, configurations = new HashSet<LocationBindings>());
            configurations.Add(this);
        }

        private void OnDisable()
        {
            if (!registered.TryGetValue(registeredScene, out var configurations)) return;
            configurations.Remove(this);
            if (configurations.Count == 0) registered.Remove(registeredScene);
        }

        // Конфигурация регистрируется сама; иерархию сцены для поиска зависимостей не обходим.
        public static LocationBindings For(Scene scene)
        {
            if (!registered.TryGetValue(scene.handle, out var configurations) || configurations.Count == 0)
                throw new InvalidOperationException("В сцене «" + scene.name + "» отсутствует активная конфигурация LocationBindings.");
            if (configurations.Count != 1)
                throw new InvalidOperationException("В сцене «" + scene.name + "» должна быть ровно одна конфигурация LocationBindings.");
            foreach (var configuration in configurations) return configuration;
            throw new InvalidOperationException("Конфигурация локации недоступна: " + scene.name);
        }

        private T Require<T>(T reference, string label) where T : UnityEngine.Object
        {
            if (reference == null)
                throw new InvalidOperationException("В конфигурации локации «" + gameObject.scene.name + "» не назначена ссылка: " + label);
            var target = reference is Component component ? component.gameObject : reference as GameObject;
            if (target != null && target.scene != gameObject.scene)
                throw new InvalidOperationException("Ссылка «" + label + "» должна принадлежать сцене «" + gameObject.scene.name + "».");
            return reference;
        }
    }
}
