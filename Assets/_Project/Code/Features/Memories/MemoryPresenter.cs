using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Memories
{
    public sealed class MemoryPresenter : IDisposable
    {
        private readonly MemoryCollection collection;
        private readonly GameplayModeState state;
        private readonly IMemoryView view;
        private readonly IPauseInput input;
        private CancellationTokenSource playback;
        private string currentId;
        private bool disposed;
        public bool IsPlaying => playback != null;

        public MemoryPresenter(MemoryCollection collection, GameplayModeState state, IMemoryView view, IPauseInput input)
        {
            this.collection = collection;
            this.state = state;
            this.view = view;
            this.input = input;
            input.PauseRequested += Close;
            view.Hide();
        }

        public bool CanOpen(string id) => !disposed && !string.IsNullOrWhiteSpace(id)
            && state.Mode == GameplayMode.Playing && !collection.Contains(id);

        public bool Open(string id, MemoryGraph graph)
        {
            if (!CanOpen(id)) return false;
            if (graph == null) { Debug.LogWarning("Осколку не назначен граф воспоминания."); return false; }
            if (!graph.TryGetSequence(out var frames, out var error))
            { Debug.LogWarning("Воспоминание не запущено: " + error, graph); return false; }
            if (!state.TryEnter(GameplayMode.Reading)) return false;
            currentId = id;
            playback = new CancellationTokenSource();
            PlayAsync(frames, playback).Forget();
            return true;
        }

        private async UniTask PlayAsync(List<MemoryFrameNode> frames, CancellationTokenSource source)
        {
            bool completed = false;
            try
            {
                view.Show();
                foreach (var frame in frames)
                {
                    source.Token.ThrowIfCancellationRequested();
                    await view.ShowFrameAsync(frame, source.Token);
                    await UniTask.Delay(TimeSpan.FromSeconds(frame.DisplayDuration), ignoreTimeScale: true, cancellationToken: source.Token);
                }
                completed = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (playback == source)
                {
                    playback = null;
                    if (completed) collection.Collect(currentId);
                    currentId = null;
                    view.Hide();
                    state.Exit(GameplayMode.Reading);
                }
                source.Dispose();
            }
        }

        // Escape interrupts the viewing; only reaching End collects the memory.
        public void Close() => Cancel(true);

        private void Cancel(bool restoreControl)
        {
            if (playback == null) return;
            var source = playback;
            playback = null;
            currentId = null;
            source.Cancel();
            view.Hide();
            state.Exit(GameplayMode.Reading, restoreControl);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            input.PauseRequested -= Close;
            Cancel(false);
        }
    }
}