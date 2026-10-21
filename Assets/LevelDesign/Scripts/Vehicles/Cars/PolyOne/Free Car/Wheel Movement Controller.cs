using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PolyOneStudio
{
    [ExecuteAlways]
    public class WheelMovementController : MonoBehaviour
    {
        public enum RotationAxis { X, Y, Z }

        [SerializeField] private List<Transform> m_wheelTransforms;
        [SerializeField] [Range(0f, 1000f)] private float m_value = 100f;
        [SerializeField] private bool m_reverse = false;
        [SerializeField] private bool m_isPlay = true;
        [SerializeField] private RotationAxis m_axis = RotationAxis.X;

        private float m_lastTime;

#if UNITY_EDITOR
        private void OnEnable()
        {
            m_lastTime = Time.realtimeSinceStartup;
            EditorApplication.update += EditorUpdate; // Đăng ký callback cho Edit Mode
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate; // Hủy đăng ký khi tắt
        }

        private void EditorUpdate()
        {
            if (!Application.isPlaying) // Chỉ chạy trong Edit Mode
            {
                RotateWheels();
            }
        }
#endif

        private void Update()
        {
            if (Application.isPlaying) // Chỉ chạy trong Play Mode
            {
                RotateWheels();
            }
        }

        private void RotateWheels()
        {
            if (!m_isPlay || m_wheelTransforms == null || m_wheelTransforms.Count == 0)
                return;

            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - m_lastTime;
            m_lastTime = currentTime;

            float direction = m_reverse ? 1f : -1f;
            float rotationAmount = m_value * deltaTime * direction;

            Vector3 axisVector = Vector3.right;
            switch (m_axis)
            {
                case RotationAxis.X: axisVector = Vector3.right; break;
                case RotationAxis.Y: axisVector = Vector3.up; break;
                case RotationAxis.Z: axisVector = Vector3.forward; break;
            }

            foreach (var wheel in m_wheelTransforms)
            {
                if (wheel != null)
                {
                    wheel.Rotate(axisVector, rotationAmount, Space.Self);
                }
            }
        }
    }
}