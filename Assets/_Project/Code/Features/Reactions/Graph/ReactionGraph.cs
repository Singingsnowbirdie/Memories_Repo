using UnityEngine.Scripting.APIUpdating;
using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Memories.Gameplay.Reactions
{
    [MovedFrom(true, sourceNamespace: "Memories.Gameplay.Triggers", sourceAssembly: "Memories.Gameplay", sourceClassName: "TriggerGraph")]
    public sealed class ReactionGraph : NodeGraph
    {
        [UnityEngine.SerializeField] private List<string> objectKeys = new List<string>();
        public IReadOnlyList<string> ObjectKeys => objectKeys;
        public bool HasObjectKey(string key) => !string.IsNullOrWhiteSpace(key) && objectKeys.Count(value => value == key) == 1;
        public bool RunOnce => nodes.OfType<ReactionStartNode>().FirstOrDefault()?.RunOnce ?? true;

        public bool TryGetSequence(out List<ReactionNode> actions, out string error)
        {
            validating ??= new HashSet<ReactionGraph>();
            if (!validating.Add(this))
            { actions = new List<ReactionNode>(); error = "Обнаружен циклический вызов графов реакций."; return false; }
            try { return ValidateSequence(out actions, out error); }
            finally { validating.Remove(this); }
        }

        [System.ThreadStatic] private static HashSet<ReactionGraph> validating;

        private bool ValidateSequence(out List<ReactionNode> actions, out string error)
        {
            actions = new List<ReactionNode>();
            error = null;
            if (nodes == null || nodes.Any(n => n == null) || nodes.OfType<ReactionStartNode>().Count() != 1 ||
                !nodes.OfType<ReactionEndNode>().Any())
            { error = "Нужны ровно одно «Начало» и хотя бы один «Конец»."; return false; }
            var visited = new HashSet<Node>();
            var active = new HashSet<Node>();
            if (!Visit(nodes.OfType<ReactionStartNode>().Single(), visited, active, actions, out error)) return false;
            foreach (var hub in nodes.OfType<ReactionHubNode>())
            {
                var join = HubJoin(hub);
                if (join == null)
                { error = "Все ветки хаба должны сходиться в общую ноду."; return false; }
                var occupied = CollectPath(join);
                foreach (var port in hub.Branches)
                {
                    var branch = CollectPath(Next(hub, port), join);
                    if (branch.Overlaps(occupied))
                    { error = "Ветки хаба не должны пересекаться до общей ноды продолжения."; return false; }
                    occupied.UnionWith(branch);
                    foreach (var action in branch.OfType<ReactionNode>())
                    {
                        var branchActions = new List<ReactionNode> { action };
                        if (action.Kind == ReactionKind.RunGraph)
                        {
                            if (!action.ReactionGraph.TryGetSequence(out var nested, out error)) return false;
                            branchActions.AddRange(nested);
                        }
                        if (branchActions.Any(a => a.Kind == ReactionKind.ChangeLocation))
                        { error = "Переход в другую локацию разместите после объединения веток хаба."; return false; }
                    }
                }
            }
            if (visited.Count != nodes.Count || (actions.Count == 0 && !nodes.OfType<ReactionConditionNode>().Any()))
            { error = "Добавьте реакции и подключите все ноды к графу."; return false; }
            return true;
        }

        public static Node Next(Node node, string outputName = "next") =>
            node.GetOutputPort(outputName)?.GetConnection(0)?.node;

        // Called after structural validation: the graph is acyclic and all outputs are connected.
        public static Node HubJoin(ReactionHubNode hub)
        {
            var cache = new Dictionary<Node, HashSet<Node>>();
            HashSet<Node> Required(Node node)
            {
                if (node == null) return new HashSet<Node>();
                if (cache.TryGetValue(node, out var known)) return known;
                HashSet<Node> required = null;
                foreach (var output in node.Outputs)
                {
                    var child = Required(output.GetConnection(0)?.node);
                    if (required == null) required = new HashSet<Node>(child);
                    else required.IntersectWith(child);
                }
                required ??= new HashSet<Node>();
                required.Add(node);
                cache[node] = required;
                return required;
            }
            HashSet<Node> common = null;
            foreach (var port in hub.Branches)
            {
                var required = Required(Next(hub, port));
                if (common == null) common = new HashSet<Node>(required);
                else common.IntersectWith(required);
            }
            // The nearest common node has all later common nodes in its required set.
            return common?.OrderByDescending(node => Required(node).Count).FirstOrDefault();
        }

        private static HashSet<Node> CollectPath(Node start, Node stopBefore = null)
        {
            var result = new HashSet<Node>();
            var pending = new Stack<Node>();
            pending.Push(start);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (node == null || node == stopBefore || node is ReactionEndNode || !result.Add(node)) continue;
                foreach (var output in node.Outputs)
                    foreach (var input in output.GetConnections()) pending.Push(input.node);
            }
            return result;
        }

        private bool Visit(Node node, HashSet<Node> visited, HashSet<Node> active,
            List<ReactionNode> actions, out string error)
        {
            error = null;
            if (node == null || !nodes.Contains(node) || node.graph != this || active.Contains(node))
            { error = "В графе цикл или ссылка на чужую ноду."; return false; }
            if (!visited.Add(node)) return true;
            active.Add(node);
            if (node is ReactionEndNode) { active.Remove(node); return true; }
            if (node is ReactionNode action)
            {
                if (!action.Validate(out error)) return false;
                actions.Add(action);
                if (action.Kind == ReactionKind.RunGraph)
                {
                    if (!action.ReactionGraph.TryGetSequence(out var nested, out error)) return false;
                    actions.AddRange(nested);
                }
            }
            else if (node is ReactionConditionNode condition)
            {
                if (!condition.Validate(out error)) return false;
            }
            else if (node is ReactionRandomNode random)
            {
                if (!random.Validate(out error)) return false;
            }
            else if (node is ReactionHubNode hub)
            {
                if (!hub.Validate(out error)) return false;
            }
            else if (!(node is ReactionStartNode))
            { error = "Неподдерживаемая нода в графе реакций."; return false; }

            var outputs = node is ReactionHubNode hubNode ? hubNode.Branches.ToArray()
                : node is ReactionRandomNode randomNode
                ? randomNode.Choices.Select(choice => choice.PortName).ToArray()
                : node is ReactionConditionNode ? new[] { "ifPresent", "ifAbsent" } : new[] { "next" };
            foreach (var field in outputs)
            {
                var output = node.GetOutputPort(field);
                var input = output?.ConnectionCount == 1 ? output.GetConnection(0) : null;
                if (input == null || input.fieldName != "previous" || !input.IsInput || !input.GetConnections().Contains(output))
                { error = "Подключите каждый выход ко входу следующей ноды."; return false; }
                if (node is ReactionNode transition && transition.Kind == ReactionKind.ChangeLocation &&
                    !(input.node is ReactionEndNode))
                { error = "Переход в локацию должен быть последней реакцией перед концом графа."; return false; }
                if (!Visit(input.node, visited, active, actions, out error)) return false;
            }
            active.Remove(node);
            return true;
        }
    }
}
