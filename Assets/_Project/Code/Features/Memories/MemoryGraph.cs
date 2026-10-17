using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Memories.Gameplay.Memories
{
    [CreateAssetMenu(menuName = "Memories/Memory Graph", fileName = "MemoryGraph")]
    [NodeGraph.RequireNode(typeof(MemoryStartNode))]
    public sealed class MemoryGraph : NodeGraph
    {
        public bool TryGetSequence(out List<Node> frames, out string error)
        {
            frames = new List<Node>();
            error = null;
            if (nodes.Any(n => n == null) || nodes.OfType<MemoryStartNode>().Count() != 1 || nodes.OfType<MemoryEndNode>().Count() != 1)
            { error = "Нужны ровно одно «Начало» и один «Конец», без отсутствующих нод."; return false; }
            Node node = nodes.OfType<MemoryStartNode>().Single();
            var visited = new HashSet<Node>();
            while (true)
            {
                if (!nodes.Contains(node) || node.graph != this || !visited.Add(node))
                { error = "В графе цикл или ссылка на чужую ноду."; return false; }
                if (node is MemoryEndNode)
                {
                    if (!frames.OfType<MemoryFrameNode>().Any() || visited.Count != nodes.Count)
                    { error = "Добавьте кадры и подключите все ноды к основной цепочке."; return false; }
                    return true;
                }
                if (node is MemoryFrameNode frame)
                {
                    if (frame.Image == null || !ValidTime(frame.DisplayDuration) || frame.DisplayDuration <= 0 ||
                        !ValidTime(frame.TransitionDuration) || !System.Enum.IsDefined(typeof(MemoryFrameTransition), frame.Transition))
                    { error = "У кадра отсутствует изображение или некорректны настройки времени/перехода."; return false; }
                    frames.Add(frame);
                }
                else if (node is MemoryMusicNode music)
                {
                    if (!System.Enum.IsDefined(typeof(MemoryMusicAction), music.Action) || !ValidTime(music.FadeDuration) ||
                        !ValidTime(music.Volume) || music.Volume > 1)
                    { error = "У ноды музыки некорректны настройки действия, громкости или фейда."; return false; }
                    frames.Add(music);
                }
                else if (!(node is MemoryStartNode))
                { error = "Неподдерживаемая нода."; return false; }
                var output = node.GetOutputPort("next");
                var input = output?.ConnectionCount == 1 ? output.GetConnection(0) : null;
                if (input == null || input.fieldName != "previous" || !input.IsInput || input.ConnectionCount != 1 || input.GetConnection(0) != output)
                { error = "Подключите выход «Далее» к входу следующей ноды."; return false; }
                node = input.node;
            }
        }
        private static bool ValidTime(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
    }
}
