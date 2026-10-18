using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.UI;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Memories
{
    public sealed class MemoryPresenter : IDisposable
    {
        private readonly MemoryCollection collection;
        private readonly GameplayModeState state;
        private readonly IMemoryView view;
        private readonly IPauseInput input;
        private readonly IMemoryMusic music;
        private readonly IMemorySound sounds;
        private CancellationTokenSource playback;
        private string currentId;
        private bool disposed;
        public bool IsPlaying => playback != null;
        public bool CanStartPlayback => !disposed && state.Mode == GameplayMode.Playing;

        public MemoryPresenter(MemoryCollection collection, GameplayModeState state, IMemoryView view, IPauseInput input, IMemoryMusic music, IMemorySound sounds)
        {
            this.collection = collection;
            this.state = state;
            this.view = view;
            this.input = input;
            this.music = music;
            this.sounds = sounds;
            input.PauseRequested += Close;
            view.Hide();
        }

        public bool CanOpen(string id, bool allowReplay = false) => CanStartPlayback && !string.IsNullOrWhiteSpace(id)
            && (allowReplay || !collection.Contains(id));

        public bool Open(string id, MemoryGraph graph, bool allowReplay = false)
        {
            if (!CanOpen(id, allowReplay)) return false;
            if (graph == null) { Debug.LogWarning("Осколку не назначен граф воспоминания."); return false; }
            if (!graph.TryGetSequence(out var frames, out var error))
            { Debug.LogWarning("Воспоминание не запущено: " + error, graph); return false; }
            if (!state.TryEnter(GameplayMode.Reading)) return false;
            currentId = id;
            playback = new CancellationTokenSource();
            PlayAsync(frames, playback).Forget();
            return true;
        }

        private async UniTask PlayAsync(List<Node> frames, CancellationTokenSource source)
        {
            bool completed = false;
            try
            {
                view.Show();
                foreach (var step in frames)
                {
                    source.Token.ThrowIfCancellationRequested();
                    if (step is MemoryMusicNode musicNode)
                    {
                        music.Execute(musicNode);
                        continue;
                    }
                    if (step is MemorySoundNode soundNode)
                    {
                        var sound = sounds.PlayAsync(soundNode, source.Token);
                        if (soundNode.WaitForCompletion) await sound;
                        else sound.Forget();
                        continue;
                    }
                    var frame = (MemoryFrameNode)step;
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
                    music.StopImmediately();
                    sounds.StopAll();
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
            music.StopImmediately();
            sounds.StopAll();
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
