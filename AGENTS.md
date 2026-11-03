# Project rules

- Do only the requested work. Preserve existing user changes; inspect diffs before editing.
- Do not change unrelated scenes, prefabs, serialized references, visuals, or styling.
- List every changed file in the final response.

## Explicit gameplay reactions — mandatory

- Interactables only launch reaction graphs. Do not introduce location-specific `Interactable` subclasses or put unique puzzle logic in an interactable. Such behavior belongs to a location-specific `CustomReaction`; use the generic `ReactionInteractable` for interaction.

The user explicitly requires that gameplay state changes be initiated explicitly through reaction graphs and custom reactions.

- Never poll quest variables, inventory, collected memories, or another component's completion flags in `Update`, `LateUpdate`, `FixedUpdate`, DI ticks, repeating coroutines, or timers to discover and trigger gameplay changes.
- Do not replace polling with subscriptions to progress changes. Graph nodes must explicitly invoke the required action or `CustomReaction.Execute` where the change originates.
- Refresh dependent presentation (readiness, highlights, doors, windows, equipment) explicitly from the relevant graph actions. Do not hide polling in getters called every frame.
- Existing built-in graph actions for inventory, object activity, variables, and hints remain explicit actions; use custom reactions for component-specific behavior.
- Restore already-completed progress once when binding a loaded location. Keep restoration separate from live gameplay transitions.
- Frame-based movement, rendering, input, animation interpolation, and spatial detection are not quest-state observers. They must not contain hidden progress-dependent consequences. Completion of an explicitly started animation may explicitly invoke its next custom reaction.
- UI bindings and persistence may observe changes only to display or save them, never to advance gameplay. Do not claim that all `Update` methods or all event subscriptions have been removed when only gameplay observers were audited.
- When adding a graph condition or progression change, inspect all affected paths, including item acquisition order, save restoration, and tester checkpoints.
