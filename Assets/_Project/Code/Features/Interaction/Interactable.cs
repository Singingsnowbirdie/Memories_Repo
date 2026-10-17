using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    // Place on the collider itself or one of its parents.
    public abstract class Interactable : MonoBehaviour
    {
        public virtual bool CanInteract => isActiveAndEnabled;
        public abstract string Prompt { get; }
        public abstract void Interact();
    }
}
