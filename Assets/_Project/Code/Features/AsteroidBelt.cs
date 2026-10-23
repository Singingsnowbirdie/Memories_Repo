using UnityEngine;
using UnityEngine.Rendering;

namespace Memories.Gameplay
{
    // Distant scenery follows the player while retaining its world-space tilt and offset.
    internal sealed class AsteroidBelt
    {
        private readonly Mesh[] meshes;
        private readonly Material material;
        private readonly Rock[] rocks;
        private readonly Quaternion tilt = Quaternion.Euler(65, 0, 15);

        private struct Rock
        {
            public float angle, radius, height, size, speed, phase;
            public Vector3 axis;
            public Quaternion rotation;
        }

        public AsteroidBelt(Mesh[] meshes, Material material, int count)
        {
            this.meshes = meshes;
            this.material = material;
            rocks = new Rock[Mathf.Clamp(count, 16, 400)];
            var random = new System.Random(7319);
            float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);
            for (int i = 0; i < rocks.Length; i++)
                rocks[i] = new Rock {
                    angle = Next(0, Mathf.PI * 2), radius = Next(140, 175),
                    height = Next(-9, 9), size = Next(1.2f, 5.5f),
                    speed = Next(0.0015f, 0.0028f), phase = Next(0, 6.28f),
                    axis = new Vector3(Next(-1, 1), Next(-1, 1), Next(-1, 1)).normalized,
                    rotation = Quaternion.Euler(Next(0, 360), Next(0, 360), Next(0, 360))
                };
        }

        public void Draw(Camera camera)
        {
            if (camera == null || !camera.isActiveAndEnabled || material == null ||
                meshes == null || meshes.Length == 0) return;
            Vector3 center = camera.transform.position + new Vector3(70, -1.7f, 100);
            float time = Time.time;
            for (int i = 0; i < rocks.Length; i++)
            {
                Mesh mesh = meshes[i % meshes.Length];
                if (mesh == null) continue;
                Rock rock = rocks[i];
                float angle = rock.angle + time * rock.speed;
                float radius = rock.radius + 3 * Mathf.Sin(time * 0.035f + rock.phase);
                Vector3 orbit = new Vector3(Mathf.Cos(angle) * radius,
                    rock.height + 2 * Mathf.Sin(time * 0.045f + rock.phase), Mathf.Sin(angle) * radius);
                Quaternion rotation = Quaternion.AngleAxis(time * (0.7f + rock.phase * 0.2f), rock.axis) * rock.rotation;
                float extent = Mathf.Max(mesh.bounds.size.x, Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.z));
                Matrix4x4 matrix = Matrix4x4.TRS(center + tilt * orbit, rotation,
                    Vector3.one * (rock.size / Mathf.Max(extent, 0.001f))) *
                    Matrix4x4.Translate(-mesh.bounds.center);
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    Graphics.DrawMesh(mesh, matrix, material, camera.gameObject.layer, camera, sub, null,
                        ShadowCastingMode.Off, true, null, LightProbeUsage.BlendProbes);
            }
        }
    }
}
