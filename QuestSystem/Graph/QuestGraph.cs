using System;
using Anvil.API;
using NLog;

namespace QuestSystem.Graph
{
    /// <summary>
    /// Directed acyclic graph (DAG) of quest nodes, with a linear evaluation flow
    /// overlaid on top of the lifetime structure.
    ///
    /// A <b>Root</b> node is both:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// A <b>lifetime root</b> — a node with no parents in the lifetime DAG.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// An <b>evaluation boundary</b> — a stable entry/end point of a linear evaluation
    /// </description>
    /// </item>
    /// </list>
    ///
    /// Player progress is tracked by a cursor:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>Root</b> — the ID of the current evaluation boundary (also a lifetime root).
    /// It identifies the chain the player is currently in.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Node</b> — the ID of the node where the last evaluation stopped.
    /// Initially equal to <b>Root</b>. Subsequent evaluations resume from this node
    /// rather than restarting from the root. A chain may, on success, move the player
    /// to a different root (switching to another chain) or back to the same root
    /// (resetting progress).
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    internal sealed partial class QuestGraph : IDisposable
    {
        public const int MaxChainLength = 100;
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private abstract class GraphComponent : IDisposable
        {
            public GraphComponent(string tag){Tag = tag;}
            public string Tag {get;}
            public abstract void Dispose();
        }

        private readonly Storage _storage;
        private readonly Runtime _runtime;
        private readonly Session _session;

        public QuestGraph(string tag, INodeLoader nodeLoader)
        {
            _storage = new Storage(tag, nodeLoader);
            _runtime = new Runtime(tag, _storage);
            _session = new Session(tag, _storage, OnQuestCompleted);
        }

        public void Evaluate(int id, NwPlayer player)
        {
            if(_storage[id] == null)
                throw new InvalidOperationException("First node of the chain must exist on the graph to start evaluation");

            var state = _session[player] ?? throw new InvalidOperationException("Session must contain player state for evaluation to start");
            
            var outcome = _runtime.EvaluateChain(state.Cursor,player);

            _session.ApplyOutcome(outcome, player);
        }

        public event Action<QuestGraph, NwPlayer>? QuestCompleted;
        private void OnQuestCompleted(NwPlayer player) => QuestCompleted?.Invoke(this, player);

        public void Dispose()
        {
            _session.Dispose();
            _runtime.Dispose();
            _storage.Dispose();
        }
    }
}