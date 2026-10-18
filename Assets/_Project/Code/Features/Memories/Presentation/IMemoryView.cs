using System.Threading;
using Cysharp.Threading.Tasks;

namespace Memories.Gameplay.Memories
{
    public interface IMemoryView
    {
        void Show();
        UniTask ShowFrameAsync(MemoryFrameNode frame, CancellationToken token);
        void Hide();
    }
}