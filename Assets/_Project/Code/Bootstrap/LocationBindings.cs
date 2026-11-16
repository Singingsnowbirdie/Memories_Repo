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
        [SerializeField, InspectorName("Дом: загрузка двора")] private Transform courtyardConnection;
        [SerializeField, InspectorName("Двор: вход")] private Memories.Gameplay.Triggers.SceneTrigger backyardEntrance;
        [SerializeField, InspectorName("Двор: освещение входа")] private Transform backyardEntranceLighting;
        [SerializeField, InspectorName("Двор: соединение с гаражом")] private Transform garageAttachment;
        [SerializeField, InspectorName("Двор: секции забора перед гаражом")] private GameObject[] garageFenceSections;
        [SerializeField, InspectorName("Гараж: соединение со школой")] private GarageSchoolConnection schoolConnection;
        [SerializeField, InspectorName("Гараж: ворота")] private OpenGarageGateReaction garageGate;
        [SerializeField, InspectorName("Школа: вход в спортзал")] private SchoolGymEntranceReaction gymEntrance;

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

        private static readonly Dictionary<int, HashSet<LocationBindings>> registered = new Dictionary<int, HashSet<LocationBindings>>();
        private int registeredScene;

        public Transform CourtyardConnection => Require(courtyardConnection, "дверь во двор");
        public Memories.Gameplay.Triggers.SceneTrigger BackyardEntrance => Require(backyardEntrance, "вход во двор");
        public Transform BackyardEntranceLighting => Require(backyardEntranceLighting, "освещение входа во двор");
        public Transform GarageAttachment => Require(garageAttachment, "соединение с гаражом");
        public GameObject[] GarageFenceSections => garageFenceSections;
        public GarageSchoolConnection SchoolConnection => Require(schoolConnection, "соединение со школой");
        public OpenGarageGateReaction GarageGate => Require(garageGate, "ворота гаража");
        public SchoolGymEntranceReaction GymEntrance => Require(gymEntrance, "вход в спортзал");

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