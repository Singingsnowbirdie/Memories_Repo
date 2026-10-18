using System.Threading;
using Cysharp.Threading.Tasks;

namespace Memories.Gameplay.Memories
{
    public interface IMemorySound
    {
        UniTask PlayAsync(MemorySoundNode node, CancellationToken token);
        void StopAll();
    }
}
