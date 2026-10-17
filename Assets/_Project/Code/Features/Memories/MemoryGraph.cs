using UnityEngine;
using XNode;

namespace Memories.Gameplay.Memories
{
    [CreateAssetMenu(menuName = "Memories/Memory Graph", fileName = "MemoryGraph")]
    [NodeGraph.RequireNode(typeof(MemoryStartNode))]
    public sealed class MemoryGraph : NodeGraph
    {
    }
}
