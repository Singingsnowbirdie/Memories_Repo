using UnityEngine;

namespace Memories.Gameplay
{
    [RequireComponent(typeof(Renderer))]
    public sealed class TVComicPlayback : MonoBehaviour
    {
        private static readonly int StartTime = Shader.PropertyToID("_StartTime");
        private void OnEnable()
        {
            var screen = GetComponent<Renderer>();
            var properties = new MaterialPropertyBlock();
            screen.GetPropertyBlock(properties);
            properties.SetFloat(StartTime, Time.time);
            screen.SetPropertyBlock(properties);
        }
    }
}
