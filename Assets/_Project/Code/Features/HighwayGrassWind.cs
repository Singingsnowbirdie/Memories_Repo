using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay
{
    public sealed class HighwayGrassWind : MonoBehaviour
    {
        [SerializeField] private Transform[] plants = new Transform[0];
        [SerializeField, Range(0, 5)] private float angle = 1.8f;
        [SerializeField, Min(0.1f)] private float period = 5f;
        [SerializeField, Min(0)] private float animationRadius;
        private Transform viewer;
        private Renderer[] renderers;
        private MeshFilter[] filters;
        private readonly List<int> nearby = new List<int>();
        private float nextDistanceCheck;
        private Quaternion[] restRotations;
        private float[] phases;
        private float elapsed;
        private GrassPatch[] patches;
        internal bool PausedInsideHouse { get; set; }

        public void Initialize(Transform player)
        {
            viewer = player;
            nextDistanceCheck = 0;
        }

        private sealed class GrassPatch
        {
            public MeshFilter Filter;
            public Mesh Original;
            public Mesh Animated;
            public Vector3[] Rest;
            public Vector3[] Vertices;
            public float[] Weights;
        }

        private void Awake()
        {
            restRotations = new Quaternion[plants.Length];
            phases = new float[plants.Length];
            patches = new GrassPatch[plants.Length];
            renderers = new Renderer[plants.Length];
            filters = new MeshFilter[plants.Length];
            for (int i = 0; i < plants.Length; i++)
            {
                if (plants[i] == null) continue;
                restRotations[i] = plants[i].localRotation;
                Vector3 position = plants[i].position;
                phases[i] = position.x * 0.73f + position.z * 0.39f;
                renderers[i] = plants[i].GetComponent<Renderer>();
                var filter = plants[i].GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null ||
                    filter.sharedMesh.name != "SM_Gen_Env_Grass_06") continue;
                filters[i] = filter;
                if (animationRadius <= 0) CreatePatch(i);
            }
        }

        private void CreatePatch(int i)
        {
            var filter = filters[i];
            if (filter == null || patches[i] != null) return;
            var original = filter.sharedMesh;
            var mesh = Instantiate(original);
            mesh.MarkDynamic();
            var rest = mesh.vertices;
            var weights = new float[rest.Length];
            float height = Mathf.Max(mesh.bounds.size.y, 0.001f);
            for (int v = 0; v < rest.Length; v++)
            {
                float fraction = Mathf.Clamp01((rest[v].y - mesh.bounds.min.y) / height);
                // Основание остаётся неподвижным; изгиб усиливается к кончикам травинок.
                weights[v] = height * fraction * fraction;
            }
            patches[i] = new GrassPatch
            {
                Filter = filter, Original = original, Animated = mesh,
                Rest = rest, Vertices = new Vector3[rest.Length], Weights = weights
            };
            filter.sharedMesh = mesh;
        }

        private void Update()
        {
            if (PausedInsideHouse) return;
            elapsed += Time.deltaTime;
            if (animationRadius > 0 && elapsed >= nextDistanceCheck)
            {
                nextDistanceCheck = elapsed + 0.25f;
                nearby.Clear();
                if (viewer != null)
                {
                    float radiusSquared = animationRadius * animationRadius;
                    Vector3 position = viewer.position;
                    for (int i = 0; i < plants.Length; i++)
                    {
                        if (plants[i] == null || !plants[i].gameObject.activeInHierarchy) continue;
                        var renderer = renderers[i];
                        float distance = renderer != null
                            ? renderer.bounds.SqrDistance(position)
                            : (plants[i].position - position).sqrMagnitude;
                        if (distance <= radiusSquared) nearby.Add(i);
                    }
                }
            }
            float time = elapsed * 2f * Mathf.PI / Mathf.Max(0.1f, period);
            float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed));
            int count = animationRadius > 0 ? nearby.Count : plants.Length;
            for (int index = 0; index < count; index++)
            {
                int i = animationRadius > 0 ? nearby[index] : index;
                var plant = plants[i];
                if (plant == null || !plant.gameObject.activeInHierarchy) continue;
                CreatePatch(i);
                float phase = time + phases[i];
                float sway = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 1.37f) * 0.25f;
                var patch = patches[i];
                if (patch != null)
                {
                    // Горизонтальная деформация не поднимает ни один конец длинного участка.
                    float strength = Mathf.Tan(angle * Mathf.Deg2Rad) * fade *
                        plant.TransformVector(Vector3.up).magnitude;
                    Vector3 offset = plant.InverseTransformVector(new Vector3(
                        sway, 0, 0.35f * Mathf.Sin(phase * 0.79f)) * strength);
                    for (int v = 0; v < patch.Rest.Length; v++)
                        patch.Vertices[v] = patch.Rest[v] + offset * patch.Weights[v];
                    patch.Animated.vertices = patch.Vertices;
                    patch.Animated.RecalculateNormals();
                    patch.Animated.RecalculateBounds();
                    continue;
                }
                plant.localRotation = restRotations[i] * Quaternion.Euler(
                    angle * fade * 0.35f * Mathf.Sin(phase * 0.79f), 0, angle * fade * sway);
            }
        }

        private void OnDisable()
        {
            elapsed = 0;
            nextDistanceCheck = 0;
            nearby.Clear();
            if (restRotations == null) return;
            for (int i = 0; i < plants.Length; i++)
            {
                if (plants[i] != null) plants[i].localRotation = restRotations[i];
                var patch = patches[i];
                if (patch == null) continue;
                patch.Animated.vertices = patch.Rest;
                patch.Animated.RecalculateNormals();
                patch.Animated.RecalculateBounds();
            }
        }

        private void OnDestroy()
        {
            if (patches == null) return;
            foreach (var patch in patches)
            {
                if (patch == null) continue;
                if (patch.Filter != null) patch.Filter.sharedMesh = patch.Original;
                Destroy(patch.Animated);
            }
        }
    }
}
