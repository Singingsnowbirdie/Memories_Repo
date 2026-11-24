using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Memories.Contracts;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Lives in Bootstrap for the whole gameplay session, independently of loaded locations.
    public sealed class ReactionManager : IDisposable
    {
        private sealed class Binding
        {
            public ReactionGraph Graph;
            public ReactionRunner Runner;
        }

        private readonly Dictionary<UnityEngine.Object, Binding> bindings = new();
        private readonly HashSet<ReactionRunner> executions = new();
        private readonly Dictionary<ReactionGraph, ReactionGraphBinding> registeredGraphs = new();
        private readonly CancellationTokenSource shutdown = new();
        private readonly CancellationToken sessionToken;
        private readonly StoryTextPresenter texts;
        private readonly MemoryPresenter memories;
        private readonly ILocationTransition locations;
        private readonly QuestInventory inventory;
        private readonly CustomVariableStore variables;
        private readonly PlayerLookAction playerLook;
        private readonly GameplayModeState gameplayMode;
        private bool disposed;

        public int ActiveExecutionCount => executions.Count;

        public ReactionManager(StoryTextPresenter texts, MemoryPresenter memories, ILocationTransition locations,
            QuestInventory inventory, CustomVariableStore variables, PlayerLookAction playerLook, GameplayModeState gameplayMode)
        {
            this.texts = texts;
            this.memories = memories;
            this.locations = locations;
            this.inventory = inventory;
            this.variables = variables;
            this.playerLook = playerLook;
            this.gameplayMode = gameplayMode;
            sessionToken = shutdown.Token;
        }

        public void Bind(UnityEngine.Object source, ReactionGraph graph,
            ReactionObjectBinding[] objects = null, CustomReactionBinding[] customReactions = null)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ReactionManager));
            if (ReferenceEquals(source, null)) throw new ArgumentNullException(nameof(source));
            if (bindings.TryGetValue(source, out var existing))
            {
                if (existing.Graph != graph) throw new InvalidOperationException("A reaction source is already bound to another graph.");
                return;
            }
            bindings.Add(source, new Binding { Graph = graph, Runner = CreateRunner(source, objects, customReactions) });
        }

        public bool CanRun(UnityEngine.Object source) => !disposed && TryGet(source, out var binding) && binding.Runner.CanRun(binding.Graph);
        public void RegisterGraph(ReactionGraphBinding source, ReactionGraph graph,
            ReactionObjectBinding[] objects = null, CustomReactionBinding[] customReactions = null)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ReactionManager));
            if (source == null || graph == null) throw new ArgumentException("Назначьте граф и его привязки.");
            if (registeredGraphs.TryGetValue(graph, out var existing) && existing != null && existing != source)
                throw new InvalidOperationException("Несколько наборов привязок вызываемого графа: " + graph.name);
            Bind(source, graph, objects, customReactions);
            registeredGraphs[graph] = source;
        }

        public bool TryRunGraph(ReactionGraph graph)
        {
            if (disposed || graph == null || !registeredGraphs.TryGetValue(graph, out var source) ||
                source == null || !source.isActiveAndEnabled) return false;
            return TryRun(source, allowDuringMemory: true);
        }
        public bool TryRun(UnityEngine.Object source, bool allowDuringMemory = false) =>
            !disposed && TryGet(source, out var binding) && binding.Runner.TryRun(binding.Graph, allowDuringMemory);
        public bool IsRunning(UnityEngine.Object source) => TryGet(source, out var binding) && binding.Runner.IsRunning;
        public string CaptureHints(UnityEngine.Object source) => TryGet(source, out var binding) ? binding.Runner.CaptureHints() : "";
        public void RestoreHints(UnityEngine.Object source, string state)
        {
            if (!disposed && TryGet(source, out var binding)) binding.Runner.RestoreHints(binding.Graph, state);
        }
        public void Stop(UnityEngine.Object source)
        {
            if (TryGet(source, out var binding)) binding.Runner.Stop();
        }
        public void StopAll()
        {
            foreach (var binding in bindings.Values.ToArray()) binding.Runner.Stop();
            foreach (var execution in executions.ToArray()) execution.Stop();
        }

        private bool TryGet(UnityEngine.Object source, out Binding binding)
        {
            binding = null;
            // Destroyed Unity objects remain valid invocation IDs until this session ends.
            return !ReferenceEquals(source, null) && bindings.TryGetValue(source, out binding);
        }

        internal ReactionRunner CreateRunner(UnityEngine.Object source, ReactionObjectBinding[] objects, CustomReactionBinding[] customReactions) =>
            new ReactionRunner(this, sessionToken, texts, memories, source, locations, inventory, variables, objects, customReactions, playerLook, gameplayMode);
        internal void Started(ReactionRunner execution) => executions.Add(execution);
        internal void Finished(ReactionRunner execution) => executions.Remove(execution);

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { shutdown.Cancel(); StopAll(); }
            finally { shutdown.Dispose(); bindings.Clear(); registeredGraphs.Clear(); }
        }
    }
}
