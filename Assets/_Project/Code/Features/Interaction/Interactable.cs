using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    // Place on the collider itself or one of its parents.
    public abstract class Interactable : MonoBehaviour
    {
        private bool interactionEnabled = true;
        public void SetInteractionEnabled(bool value) => interactionEnabled = value;
        public virtual bool CanInteract => isActiveAndEnabled && interactionEnabled;
        public abstract string Prompt { get; }
        public abstract void Interact();
    }
}
