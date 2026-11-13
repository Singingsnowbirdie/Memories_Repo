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
        [SerializeField, InspectorName("Дом: дверь во двор")] private OpenCourtyardDoorReaction courtyardDoor;
        [SerializeField, InspectorName("Двор: вход")] private BackyardEntranceReaction backyardEntrance;
        [SerializeField, InspectorName("Двор: соединение с гаражом")] private BackyardGarageAttachment garageAttachment;
        [SerializeField, InspectorName("Гараж: соединение со школой")] private GarageSchoolConnection schoolConnection;
        [SerializeField, InspectorName("Гараж: ворота")] private OpenGarageGateReaction garageGate;
        [SerializeField, InspectorName("Школа: вход в спортзал")] private SchoolGymEntranceReaction gymEntrance;

        private static readonly Dictionary<int, HashSet<LocationBindings>> registered = new Dictionary<int, HashSet<LocationBindings>>();
        private int registeredScene;

        public OpenCourtyardDoorReaction CourtyardDoor => Require(courtyardDoor, "дверь во двор");
        public BackyardEntranceReaction BackyardEntrance => Require(backyardEntrance, "вход во двор");
        public BackyardGarageAttachment GarageAttachment => Require(garageAttachment, "соединение с гаражом");
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

        private T Require<T>(T reference, string label) where T : Component
        {
            if (reference == null)
                throw new InvalidOperationException("В конфигурации локации «" + gameObject.scene.name + "» не назначена ссылка: " + label);
            if (reference.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("Ссылка «" + label + "» должна принадлежать сцене «" + gameObject.scene.name + "».");
            return reference;
        }
    }
}