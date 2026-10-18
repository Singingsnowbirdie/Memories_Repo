using System.Collections.Generic;
using System.Linq;
using Memories.Gameplay.UI;
using XNode;

namespace Memories.Gameplay.Story
{
    public sealed class StoryTextGraph : NodeGraph
    {
        public bool TryGetSequence(out List<StoryTextFragment> fragments, out string error)
        {
            fragments = new List<StoryTextFragment>();
            error = null;
            if (nodes == null || nodes.Any(n => n == null) || nodes.OfType<StoryTextStartNode>().Count() != 1 ||
                nodes.OfType<StoryTextEndNode>().Count() != 1)
            { error = "Нужны ровно одно «Начало» и один «Конец», без отсутствующих нод."; return false; }
            Node node = nodes.OfType<StoryTextStartNode>().Single();
            var visited = new HashSet<Node>();
            while (true)
            {
                if (!nodes.Contains(node) || node.graph != this || !visited.Add(node))
                { error = "В графе цикл или ссылка на чужую ноду."; return false; }
                if (node is StoryTextEndNode)
                {
                    if (fragments.Count == 0 || visited.Count != nodes.Count)
                    { error = "Добавьте текст и подключите все ноды к основной цепочке."; return false; }
                    return true;
                }
                if (node is StoryTextNode text)
                {
                    if (string.IsNullOrWhiteSpace(text.Text) || float.IsNaN(text.Duration) ||
                        float.IsInfinity(text.Duration) || text.Duration <= 0)
                    { error = "У текстовой ноды отсутствует текст или некорректно время показа."; return false; }
                    fragments.Add(new StoryTextFragment(text.Text, text.Duration));
                }
                else if (!(node is StoryTextStartNode))
                { error = "В графе текста обнаружена неподдерживаемая нода."; return false; }
                var output = node.GetOutputPort("next");
                var input = output?.ConnectionCount == 1 ? output.GetConnection(0) : null;
                if (input == null || input.fieldName != "previous" || !input.IsInput ||
                    input.ConnectionCount != 1 || input.GetConnection(0) != output)
                { error = "Подключите выход «Далее» к входу следующей ноды."; return false; }
                node = input.node;
            }
        }
    }
}
