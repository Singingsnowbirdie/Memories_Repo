using System.Threading;
using Cysharp.Threading.Tasks;

namespace Memories.Gameplay.UI
{
    public interface IStoryTextView
    {
        CancellationToken LifetimeToken { get; }
        UniTask ShowAsync(string text, CancellationToken token);
        UniTask ChangeTextAsync(string text, CancellationToken token);
        UniTask HideAsync(CancellationToken token);
        void HideImmediately();
    }
}
