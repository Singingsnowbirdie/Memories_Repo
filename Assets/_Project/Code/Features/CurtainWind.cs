using UnityEngine;

namespace Memories.Gameplay
{
    [RequireComponent(typeof(MeshFilter))]
    public sealed class CurtainWind : MonoBehaviour
    {
        [SerializeField, Min(0)] private float amplitude = 0.22f;
        [SerializeField, Min(0)] private float speed = 1.6f;
        [SerializeField] private float phase;
        private Mesh mesh;
        private Vector3[] rest;
        private Vector3[] vertices;
        private float height;

        private void Awake()
        {
            mesh = GetComponent<MeshFilter>().mesh;
            mesh.MarkDynamic();
            rest = mesh.vertices;
            vertices = new Vector3[rest.Length];
            height = Mathf.Max(mesh.bounds.size.y, 0.01f);
        }

        private void Update()
        {
            float time = Time.time * speed + phase;
            for (int i = 0; i < rest.Length; i++)
            {
                var p = rest[i];
                float weight = Mathf.Clamp01(-p.y / height);
                p.z += amplitude * weight * weight *
                    (Mathf.Sin(time + p.x * 3 + weight * 2) + 0.3f * Mathf.Sin(time * 2.3f + weight * 5));
                p.x += amplitude * 0.2f * weight * Mathf.Sin(time * 0.7f + weight);
                vertices[i] = p;
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
