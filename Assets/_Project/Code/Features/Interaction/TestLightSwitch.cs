using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    public sealed class TestLightSwitch : Interactable
    {
        [SerializeField] private Light controlledLight;
        public override bool CanInteract => base.CanInteract && controlledLight != null;
        public override string Prompt => controlledLight != null && controlledLight.enabled ? "Выключить свет" : "Включить свет";
        public void Initialize(Light light) => controlledLight = light;
        public override void Interact()
        {
            if (CanInteract) controlledLight.enabled = !controlledLight.enabled;
        }
    }
}
