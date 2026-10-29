using System.Linq;
using Memories.Gameplay.Reactions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Editor
{
    [InitializeOnLoad]
    public sealed class ObjectStateIdentityProcessor : IProcessSceneWithReport
    {
        static ObjectStateIdentityProcessor() => ObjectStateStore.EditorIdentity = Identity;
        public int callbackOrder => 0;
        private static string Identity(GameObject target)
        {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(target);
            return id.identifierType == 2 && id.targetObjectId != 0 ? id.ToString() : null;
        }
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var entries = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => new { target = transform.gameObject, id = Identity(transform.gameObject) })
                .Where(entry => entry.id != null).ToArray();
            var root = new GameObject("Object state identities");
            SceneManager.MoveGameObjectToScene(root, scene);
            var map = root.AddComponent<SceneObjectIds>();
            map.objects = entries.Select(entry => entry.target).ToArray();
            map.ids = entries.Select(entry => entry.id).ToArray();
        }
    }
}
