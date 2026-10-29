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
            actions = new List<ReactionNode>();
            error = null;
            if (nodes == null || nodes.Any(n => n == null) || nodes.OfType<ReactionStartNode>().Count() != 1 ||
                !nodes.OfType<ReactionEndNode>().Any())
            { error = "Нужны ровно одно «Начало» и хотя бы один «Конец»."; return false; }
            var visited = new HashSet<Node>();
            var active = new HashSet<Node>();
            if (!Visit(nodes.OfType<ReactionStartNode>().Single(), visited, active, actions, out error)) return false;
            if (visited.Count != nodes.Count || (actions.Count == 0 && !nodes.OfType<ReactionConditionNode>().Any()))
            { error = "Добавьте реакции и подключите все ноды к графу."; return false; }
            return true;
        }

        public static Node Next(Node node, string outputName = "next") =>
            node.GetOutputPort(outputName)?.GetConnection(0)?.node;

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
            }
            else if (node is ReactionConditionNode condition)
            {
                if (!condition.Validate(out error)) return false;
            }
            else if (!(node is ReactionStartNode))
            { error = "Неподдерживаемая нода в графе реакций."; return false; }

            var outputs = node is ReactionConditionNode ? new[] { "ifPresent", "ifAbsent" } : new[] { "next" };
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
