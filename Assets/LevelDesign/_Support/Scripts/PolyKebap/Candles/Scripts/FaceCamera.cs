using UnityEngine;

namespace PolyKebap{
    public class FaceCamera : MonoBehaviour
    {
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;
        }

        void LateUpdate() // Use LateUpdate so it moves after the camera moves
        {
            if (mainCamera != null)
            {
                // Makes the object face the camera
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                                mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}