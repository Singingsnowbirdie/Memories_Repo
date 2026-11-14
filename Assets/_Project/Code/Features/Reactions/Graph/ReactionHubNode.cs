using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Memories.Gameplay.Reactions
{
    [Node.CreateNodeMenu("Хаб"), Node.NodeWidth(320), Node.NodeTint("#284C53")]
    public sealed class ReactionHubNode : Node
    {
        [SerializeField, Input(ShowBackingValue.Never, ConnectionType.Multiple)] private Node previous;
        [SerializeField] private List<string> branches = new List<string>();
        [SerializeField] private int nextPortId;
        public IReadOnlyList<string> Branches => branches;

        public string AddBranch()
        {
            string port;
            do { port = "branch_" + nextPortId++; } while (HasPort(port));
            AddDynamicOutput(typeof(Node), ConnectionType.Override, TypeConstraint.None, port);
            branches.Add(port);
            return port;
        }

        public void RemoveBranch(int index)
        {
            RemoveDynamicPort(branches[index]);
            branches.RemoveAt(index);
        }

        public bool Validate(out string error)
        {
            error = null;
            if (branches == null || branches.Count < 2 ||
                branches.Any(p => string.IsNullOrEmpty(p) || p == "next" || GetOutputPort(p) == null) ||
                branches.Distinct().Count() != branches.Count || Outputs.Count() != branches.Count)
            { error = "Добавьте к хабу хотя бы две ветки."; return false; }
            return true;
        }

        protected override void Init()
        {
            name = "Хаб";
            if (branches.Count == 0) { AddBranch(); AddBranch(); }
        }
        public override object GetValue(NodePort port) => null;
    }
}
