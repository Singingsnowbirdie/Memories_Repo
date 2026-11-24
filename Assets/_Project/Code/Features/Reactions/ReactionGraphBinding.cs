using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Passive bindings for a graph invoked explicitly by another graph through ReactionManager.
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Reactions/Привязки вызываемого графа")]
    public sealed class ReactionGraphBinding : MonoBehaviour
    {
        [SerializeField] private ReactionGraph graph;
        [SerializeField] private ReactionObjectBinding[] objectBindings = new ReactionObjectBinding[0];
        [SerializeField] private CustomReactionBinding[] customReactions = new CustomReactionBinding[0];

        public void Initialize(ReactionManager manager) => manager.RegisterGraph(this, graph, objectBindings, customReactions);
    }
}
