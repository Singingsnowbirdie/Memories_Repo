using System.Collections.Generic;
using Memories.Gameplay.Player;
using UnityEngine;

namespace Memories.Gameplay.Triggers
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Memories/Triggers/Scene Trigger")]
    public sealed class SceneTrigger : MonoBehaviour
    {
        [SerializeField] private TriggerEvent triggerEvent;
        [SerializeField] private bool triggerOnce = true;
        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        private bool fired;

        private void Awake()
        {
            // Keep the designer's collider shape; a kinematic body guarantees trigger callbacks.
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            if (triggerEvent == null) triggerEvent = GetComponent<TriggerEvent>();
            var volume = GetComponent<Collider>();
            if (volume == null || !volume.isTrigger)
                Debug.LogWarning("Scene Trigger requires a Collider with Is Trigger enabled.", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActiveAndEnabled || other.GetComponentInParent<PlayerMotor>() == null) return;
            occupants.RemoveWhere(item => item == null || !item.enabled || !item.gameObject.activeInHierarchy);
            if (!occupants.Add(other) || occupants.Count != 1 || (triggerOnce && fired)) return;
            if (triggerEvent != null && triggerEvent.isActiveAndEnabled && triggerEvent.TryExecute()) fired = true;
        }

        private void OnTriggerExit(Collider other) => occupants.Remove(other);
        private void OnDisable() => occupants.Clear();
    }
}
