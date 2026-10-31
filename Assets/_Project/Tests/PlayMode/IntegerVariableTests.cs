using System;
using System.Collections.Generic;
using System.Reflection;
using Memories.Gameplay.Reactions;
using NUnit.Framework;
using UnityEngine;

namespace Memories.Tests.PlayMode
{
    public sealed class IntegerVariableTests
    {
        static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);

        [Test]
        public void IntegersAndLegacyBooleansPersistTogether()
        {
            string key = "Memories.Test.Integer." + Guid.NewGuid();
            var bank = ScriptableObject.CreateInstance<CustomVariableBank>();
            try
            {
                var flag = new CustomVariableBank.Variable("Flag");
                var count = new CustomVariableBank.Variable("Count", -7);
                Set(bank, "variables", new List<CustomVariableBank.Variable> { flag, count });
                PlayerPrefs.SetString(key, "{\"entries\":[{\"id\":\"" + bank.Id + ":" + flag.Id + "\",\"value\":true}]}");
                var store = new CustomVariableStore(key);
                Assert.IsTrue(store.Get(bank, flag.Id));
                Assert.AreEqual(-7, store.GetInt(bank, count.Id));
                Assert.IsFalse(store.HasSavedValue(bank, count.Id));
                foreach (int value in new[] { 0, -123, int.MinValue, int.MaxValue })
                {
                    store.Set(bank, count.Id, value);
                    store = new CustomVariableStore(key);
                    Assert.AreEqual(value, store.GetInt(bank, count.Id));
                    Assert.IsTrue(store.Get(bank, flag.Id));
                }
                Assert.Throws<ArgumentException>(() => store.Get(bank, count.Id));
                Assert.Throws<ArgumentException>(() => store.Set(bank, flag.Id, 5));
                Set(count, "name", "Renamed");
                Assert.AreEqual(int.MaxValue, store.GetInt(bank, count.Id));
                var temporary = new CustomVariableStore(key, false);
                temporary.Set(bank, count.Id, 42);
                Assert.AreEqual(int.MaxValue, new CustomVariableStore(key).GetInt(bank, count.Id));
                Set(count, "type", CustomVariableType.Boolean);
                Assert.IsFalse(store.HasSavedValue(bank, count.Id));
                Assert.IsFalse(store.Get(bank, count.Id));
            }
            finally { CustomVariableStore.Clear(key); UnityEngine.Object.DestroyImmediate(bank); }
        }

        [Test]
        public void IntegerComparisonsSelectCorrectBranch()
        {
            var bank = ScriptableObject.CreateInstance<CustomVariableBank>();
            var condition = ScriptableObject.CreateInstance<ReactionConditionNode>();
            try
            {
                var count = new CustomVariableBank.Variable("Count", 5);
                Set(bank, "variables", new List<CustomVariableBank.Variable> { count });
                Set(condition, "kind", ReactionConditionKind.CustomVariable);
                Set(condition, "variableBank", bank); Set(condition, "variableId", count.Id);
                var store = new CustomVariableStore(persistProgress: false);
                foreach (IntegerComparison op in Enum.GetValues(typeof(IntegerComparison)))
                foreach (int rhs in new[] { 4, 5, 6 })
                {
                    Set(condition, "integerComparison", op); Set(condition, "integerValue", rhs);
                    bool expected = op switch { IntegerComparison.Equal => 5 == rhs, IntegerComparison.NotEqual => 5 != rhs,
                        IntegerComparison.Less => 5 < rhs, IntegerComparison.LessOrEqual => 5 <= rhs,
                        IntegerComparison.Greater => 5 > rhs, _ => 5 >= rhs };
                    Assert.IsTrue(condition.Validate(out _));
                    Assert.AreEqual(expected ? "ifPresent" : "ifAbsent", condition.SelectOutput(null, store));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(condition); UnityEngine.Object.DestroyImmediate(bank); }
        }

        [Test]
        public void ReactionRunnerWritesInteger()
        {
            var bank = ScriptableObject.CreateInstance<CustomVariableBank>();
            var graph = ScriptableObject.CreateInstance<ReactionGraph>();
            try
            {
                var count = new CustomVariableBank.Variable("Count", 0);
                Set(bank, "variables", new List<CustomVariableBank.Variable> { count });
                var start = graph.AddNode<ReactionStartNode>(); var action = graph.AddNode<ReactionNode>(); var end = graph.AddNode<ReactionEndNode>();
                Set(action, "kind", ReactionKind.SetCustomVariable); Set(action, "variableBank", bank);
                Set(action, "variableId", count.Id); Set(action, "variableIntValue", -42);
                start.GetOutputPort("next").Connect(action.GetInputPort("previous"));
                action.GetOutputPort("next").Connect(end.GetInputPort("previous"));
                var store = new CustomVariableStore(persistProgress: false);
                using var runner = new ReactionRunner(null, null, null, variables: store);
                Assert.IsTrue(runner.TryRun(graph)); Assert.AreEqual(-42, store.GetInt(bank, count.Id));
            }
            finally { foreach (var node in graph.nodes) UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(graph); UnityEngine.Object.DestroyImmediate(bank); }
        }
    }
}
