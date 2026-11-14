# Project rules

- Do only the requested work. Preserve existing user changes; inspect diffs before editing.
- Do not change unrelated scenes, prefabs, serialized references, visuals, or styling.
- List every changed file in the final response.

## Explicit gameplay reactions — mandatory

- Interactables only launch reaction graphs. Do not introduce location-specific `Interactable` subclasses or put unique puzzle logic in an interactable. Such behavior belongs to a location-specific `CustomReaction`; use the generic `ReactionInteractable` for interaction.

The user explicitly requires that gameplay state changes be initiated explicitly through reaction graphs and custom reactions.

- Prefer existing built-in graph actions for standard behavior. Reduce custom reactions; do not add wrappers for object activity, interactivity, inventory or lighting.
- Custom reactions must not reference or invoke other custom reactions. Connect actions explicitly in the graph.
- Do not introduce `Sequence` classes or separate location-specific scenario controllers. Location-specific gameplay logic must live in `CustomReaction` subclasses, invoked explicitly through reactions. Renaming a controller without moving its entry points and behavior into custom reactions does not satisfy this requirement.
- Do not replace custom reactions with a bulk graph migration or expand the graph runtime to relocate their logic unless the user explicitly requests that architectural change.

- Never poll quest variables, inventory, collected memories, or another component's completion flags in `Update`, `LateUpdate`, `FixedUpdate`, DI ticks, repeating coroutines, or timers to discover and trigger gameplay changes.
- Do not replace polling with subscriptions to progress changes. Graph nodes must explicitly invoke the required action or `CustomReaction.Execute` where the change originates.
- Refresh dependent presentation (readiness, highlights, doors, windows, equipment) explicitly from the relevant graph actions. Do not hide polling in getters called every frame.
- Existing built-in graph actions for inventory, object activity, variables, and hints remain explicit actions; use custom reactions for component-specific behavior.
- Graph conditions must check custom variables, never inventory contents. Set acquisition flags explicitly in pickup graphs and keep them after items are consumed. Represent a grouped handoff by one result/stage variable, not a consumed flag for each item; use that result to prevent repeat handoffs.
- Restore already-completed progress once when binding a loaded location. Keep restoration separate from live gameplay transitions.
- Frame-based movement, rendering, input, animation interpolation, and spatial detection are not quest-state observers. They must not contain hidden progress-dependent consequences. Its continuation must be an explicit graph action, not a call from one custom reaction into another.
- UI bindings and persistence may observe changes only to display or save them, never to advance gameplay. Do not claim that all `Update` methods or all event subscriptions have been removed when only gameplay observers were audited.
- When adding a graph condition or progression change, inspect all affected paths, including item acquisition order, save restoration, and tester checkpoints.

- Never add restoration/initialization reaction graphs or an inspector field for them. Persist active hints through object state; do not replay reaction graphs when loading a save.

- Always store hint/reminder reaction graphs in Assets/_Project/Configs/Reactions/<Location>/Hints/, alongside their .meta files. Preserve asset GUIDs when moving them.
