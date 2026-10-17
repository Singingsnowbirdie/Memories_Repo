using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    // Test-scene fixture. The primitives belong to this scene object.
    public sealed class InteractionTestStand : MonoBehaviour
    {
        private void Awake()
        {
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "Light switch pedestal";
            pedestal.transform.SetParent(transform, false);
            pedestal.transform.localPosition = new Vector3(0, .7f, 0);
            pedestal.transform.localScale = new Vector3(.8f, 1.4f, .6f);

            var button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Interact here - E";
            button.transform.SetParent(transform, false);
            button.transform.localPosition = new Vector3(0, 1.65f, -.1f);
            button.transform.localScale = new Vector3(.5f, .5f, .35f);

            var lamp = new GameObject("Test lamp");
            lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = new Vector3(0, 2.6f, -.5f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .75f, .35f);
            light.range = 8;
            light.intensity = 5;
            light.enabled = false;
            button.AddComponent<TestLightSwitch>().Initialize(light);
        }
    }
}
