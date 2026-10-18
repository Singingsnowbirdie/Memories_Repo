using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Memories.Gameplay.Triggers
{
    public sealed class TriggerGraph : NodeGraph
    {
        public bool TriggerOnce => nodes.OfType<TriggerStartNode>().FirstOrDefault()?.TriggerOnce ?? true;

        public bool TryGetSequence(out List<TriggerActionNode> actions, out string error)
        {
            actions = new List<TriggerActionNode>();
            error = null;
            if (nodes == null || nodes.Any(n => n == null) || nodes.OfType<TriggerStartNode>().Count() != 1 ||
                nodes.OfType<TriggerEndNode>().Count() != 1)
            { error = "Нужны ровно одно «Начало» и один «Конец»."; return false; }
            Node node = nodes.OfType<TriggerStartNode>().Single();
            var visited = new HashSet<Node>();
            while (true)
            {
                if (!nodes.Contains(node) || node.graph != this || !visited.Add(node))
                { error = "В графе цикл или ссылка на чужую ноду."; return false; }
                if (node is TriggerEndNode)
                {
                    if (actions.Count == 0 || visited.Count != nodes.Count)
                    { error = "Добавьте события и подключите все ноды к основной цепочке."; return false; }
                    return true;
                }
                if (node is TriggerActionNode action)
                {
                    if (!action.Validate(out error)) return false;
                    actions.Add(action);
                }
                else if (!(node is TriggerStartNode))
                { error = "Неподдерживаемая нода в графе триггера."; return false; }
                var output = node.GetOutputPort("next");
                var input = output?.ConnectionCount == 1 ? output.GetConnection(0) : null;
                if (input == null || input.fieldName != "previous" || !input.IsInput ||
                    input.ConnectionCount != 1 || input.GetConnection(0) != output)
                { error = "Соедините выход «Далее» со входом следующей ноды."; return false; }
                node = input.node;
            }
        }
    }
}
