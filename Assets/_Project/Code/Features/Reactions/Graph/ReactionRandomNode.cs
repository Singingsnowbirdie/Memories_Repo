using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Reactions
{
    [Node.CreateNodeMenu("Случайный выбор"), Node.NodeWidth(320), Node.NodeTint("#69542E")]
    public sealed class ReactionRandomNode : Node
    {
        [Serializable]
        public sealed class Choice
        {
            [SerializeField] private string portName;
            [SerializeField] private float weight = 1;
            public string PortName => portName;
            public float Weight => weight;
            public Choice(string portName, float weight) { this.portName = portName; this.weight = weight; }
        }

        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Multiple)] private Node previous;
        [SerializeField] private List<Choice> choices = new List<Choice>();
        [SerializeField] private int nextPortId;
        public IReadOnlyList<Choice> Choices => choices;

        public string AddChoice(float weight = 1)
        {
            string portName;
            do { portName = "choice_" + nextPortId++; } while (HasPort(portName));
            AddDynamicOutput(typeof(Node), ConnectionType.Override, TypeConstraint.None, portName);
            choices.Add(new Choice(portName, weight));
            return portName;
        }

        public void RemoveChoice(int index)
        {
            RemoveDynamicPort(choices[index].PortName);
            choices.RemoveAt(index);
        }

        public bool Validate(out string error)
        {
            error = null;
            if (choices == null || choices.Count == 0 || choices.Any(c => c == null ||
                string.IsNullOrEmpty(c.PortName) || GetOutputPort(c.PortName) == null ||
                float.IsNaN(c.Weight) || float.IsInfinity(c.Weight) || c.Weight < 0) ||
                choices.Select(c => c.PortName).Distinct().Count() != choices.Count ||
                !choices.Any(c => c.Weight > 0) || Outputs.Count() != choices.Count)
            {
                error = "Добавьте выходы с конечными неотрицательными весами. Хотя бы один вес должен быть больше нуля.";
                return false;
            }
            return true;
        }

        // Validation never draws random numbers. Only execution chooses a branch.
        public string SelectOutput(double sample)
        {
            if (!Validate(out var error)) throw new InvalidOperationException(error);
            if (double.IsNaN(sample) || sample < 0 || sample > 1) throw new ArgumentOutOfRangeException(nameof(sample));
            double total = choices.Sum(c => (double)c.Weight);
            double remaining = sample * total;
            string lastPositive = null;
            foreach (var choice in choices)
            {
                if (choice.Weight <= 0) continue;
                lastPositive = choice.PortName;
                if (remaining < choice.Weight) return choice.PortName;
                remaining -= choice.Weight;
            }
            return lastPositive; // Unity's Random.value may equal one.
        }

        protected override void Init() => name = "Случайный выбор";
        public override object GetValue(NodePort port) => null;
    }
}
