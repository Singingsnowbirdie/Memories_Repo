using UnityEngine.Scripting.APIUpdating;
using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Memories.Gameplay.Reactions
{
    [MovedFrom(true, sourceNamespace: "Memories.Gameplay.Triggers", sourceAssembly: "Memories.Gameplay", sourceClassName: "TriggerGraph")]
    public sealed class ReactionGraph : NodeGraph
    {
        public bool RunOnce => nodes.OfType<ReactionStartNode>().FirstOrDefault()?.RunOnce ?? true;

        public bool TryGetSequence(out List<ReactionNode> actions, out string error)
        {
            actions = new List<ReactionNode>();
            error = null;
            if (nodes == null || nodes.Any(n => n == null) || nodes.OfType<ReactionStartNode>().Count() != 1 ||
                nodes.OfType<ReactionEndNode>().Count() != 1)
            { error = "Нужны ровно одно «Начало» и один «Конец»."; return false; }
            Node node = nodes.OfType<ReactionStartNode>().Single();
            var visited = new HashSet<Node>();
            while (true)
            {
                if (!nodes.Contains(node) || node.graph != this || !visited.Add(node))
                { error = "В графе цикл или ссылка на чужую ноду."; return false; }
                if (node is ReactionEndNode)
                {
                    if (actions.Count == 0 || visited.Count != nodes.Count)
                    { error = "Добавьте реакции и подключите все ноды к основной цепочке."; return false; }
                    return true;
                }
                if (node is ReactionNode action)
                {
                    if (actions.Any(previous => previous.Kind == ReactionKind.ChangeLocation))
                    { error = "Переход в локацию должен быть последней реакцией перед концом графа."; return false; }
                    if (!action.Validate(out error)) return false;
                    actions.Add(action);
                }
                else if (!(node is ReactionStartNode))
                { error = "Неподдерживаемая нода в графе реакций."; return false; }
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
