using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    [DisallowMultipleComponent]
    public abstract class LocationBindings : MonoBehaviour
    {
        private static readonly Dictionary<int, HashSet<LocationBindings>> registered = new Dictionary<int, HashSet<LocationBindings>>();
        private int registeredScene;

        protected virtual void OnEnable()
        {
            registeredScene = gameObject.scene.handle;
            if (!registered.TryGetValue(registeredScene, out var configurations))
                registered.Add(registeredScene, configurations = new HashSet<LocationBindings>());
            configurations.Add(this);
        }

        protected virtual void OnDisable()
        {
            if (!registered.TryGetValue(registeredScene, out var configurations)) return;
            configurations.Remove(this);
            if (configurations.Count == 0) registered.Remove(registeredScene);
        }

        // Конфигурация регистрируется сама; иерархию сцены для поиска зависимостей не обходим.
        public static T For<T>(Scene scene) where T : LocationBindings
        {
            if (!registered.TryGetValue(scene.handle, out var configurations) || configurations.Count == 0)
                throw new InvalidOperationException("В сцене «" + scene.name + "» отсутствует активная конфигурация LocationBindings.");
            if (configurations.Count != 1)
                throw new InvalidOperationException("В сцене «" + scene.name + "» должна быть ровно одна конфигурация LocationBindings.");
            foreach (var configuration in configurations)
            {
                if (configuration is T typed)
                {
                    typed.ValidateBindings();
                    return typed;
                }
                throw new InvalidOperationException("Scene " + scene.name + " requires " + typeof(T).Name +
                    ", but has " + configuration.GetType().Name + ".");
            }
            throw new InvalidOperationException("Конфигурация локации недоступна: " + scene.name);
        }

        protected virtual void ValidateBindings() { }

        protected void Require<T>(T reference, string label) where T : UnityEngine.Object
        {
            if (reference == null)
                throw new InvalidOperationException("В конфигурации локации «" + gameObject.scene.name + "» не назначена ссылка: " + label);
            var target = reference is Component component ? component.gameObject : reference as GameObject;
            if (target != null && target.scene != gameObject.scene)
                throw new InvalidOperationException("Ссылка «" + label + "» должна принадлежать сцене «" + gameObject.scene.name + "».");
        }
    }
}
