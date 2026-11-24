using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Player Spawn Point")]
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [Tooltip("Default location entrance. Otherwise select a tester checkpoint.")]
        public bool locationStart = true;
        public TesterCheckpoint checkpoint;

        public static Transform Find(Scene scene, TesterCheckpoint? checkpoint = null)
        {
            var points = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerSpawnPoint>(true))
                .Where(point => checkpoint.HasValue ? !point.locationStart && point.checkpoint == checkpoint.Value : point.locationStart).ToArray();
            if (points.Length != 1)
                throw new InvalidOperationException($"В сцене {scene.name} должна быть одна точка спавна: {checkpoint?.ToString() ?? "вход в локацию"}. Найдено: {points.Length}");
            return points[0].transform;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = locationStart ? Color.cyan : Color.yellow;
            var position = transform.position;
            var forward = Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.forward;
            Gizmos.DrawWireSphere(position, .2f);
            Gizmos.DrawLine(position, position + Vector3.up * 1.7f);
            Gizmos.DrawRay(position, forward);
            Gizmos.DrawLine(position + forward, position + forward * .7f + Vector3.Cross(forward, Vector3.up) * .2f);
            Gizmos.DrawLine(position + forward, position + forward * .7f - Vector3.Cross(forward, Vector3.up) * .2f);
        }
    }
}
