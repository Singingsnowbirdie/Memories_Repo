using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class SchoolAmbulanceBeacon : MonoBehaviour
    {
        [SerializeField] private Light[] lamps;
        private Renderer[] lenses;
        private Material lensMaterial;
        private MaterialPropertyBlock properties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private float elapsed;

        private void Awake()
        {
            lensMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lensMaterial.EnableKeyword("_EMISSION");
            properties = new MaterialPropertyBlock();
            lenses = new Renderer[lamps.Length];
            for (int i = 0; i < lamps.Length; i++)
            {
                if (lamps[i] == null) continue;
                var lens = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lens.name = "Emergency beacon lens";
                var collider = lens.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                lens.transform.SetParent(lamps[i].transform, false);
                lens.transform.localScale = new Vector3(.32f, .16f, .28f);
                lenses[i] = lens.GetComponent<Renderer>();
                lenses[i].sharedMaterial = lensMaterial;
                lenses[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void OnEnable()
        {
            elapsed = 0;
            RefreshLamps();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            RefreshLamps();
        }

        private void RefreshLamps()
        {
            for (int i = 0; i < lamps.Length; i++)
            {
                if (lamps[i] == null) continue;
                bool lit = Mathf.Repeat(elapsed * 2 + i * .5f, 1) < .25f;
                lamps[i].enabled = true;
                lamps[i].intensity = lit ? 5 : 0;
                if (lenses == null || lenses[i] == null) continue;
                properties.SetColor(BaseColor, lamps[i].color);
                properties.SetColor(EmissionColor, lamps[i].color * (lit ? 8 : .05f));
                lenses[i].SetPropertyBlock(properties);
            }
        }

        private void OnDisable()
        {
            foreach (var lamp in lamps)
                if (lamp != null) lamp.intensity = 0;
        }

        private void OnDestroy()
        {
            if (lensMaterial != null) Destroy(lensMaterial);
        }
    }
}
