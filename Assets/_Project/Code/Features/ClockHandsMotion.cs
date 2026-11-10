using UnityEngine;

namespace Memories.Gameplay
{
    public sealed class ClockHandsMotion : MonoBehaviour
    {
        [SerializeField] private Transform minuteHand;
        [SerializeField] private Transform hourHand;
        [SerializeField] private float minuteDegreesPerSecond = 110f;
        [SerializeField] private float hourDegreesPerSecond = -75f;

        private void Update()
        {
            minuteHand.Rotate(Vector3.forward, minuteDegreesPerSecond * Time.deltaTime, Space.Self);
            hourHand.Rotate(Vector3.forward, hourDegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
