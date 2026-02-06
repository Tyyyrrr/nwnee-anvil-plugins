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
        public enum EvaluationPolicy
        {
            /// <summary>
            /// The player will automatically move to the next chain
            /// </summary>
            AutoProceed = 0,
            /// <summary>
            /// The player will stop on the node right before chain switch
            /// </summary>
            SuspendOnLeaf,
            /// <summary>
            /// The player will be restored to the start of the chain.<br/>
            /// Progress cached within the nodes will persist for this player.
            /// </summary>
            RollbackToRoot,
            /// <summary>
            /// The player will be restored to the start of the chain.
            /// Progress cached within the nodes will be cleared for this player.
            /// </summary>
            ResetChain,
            /// <summary>
            /// Entirely skip the evaluation and jump to the new chain. Old chain resets for this player.
            /// </summary>
            SkipToNextRoot,

            /// <inheritdoc cref="AutoProceed"/>
            Default = AutoProceed
        }

        public const int MaxChainLength = 100;
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        public readonly Quest Quest;

        private readonly Storage _storage;
        private readonly Runtime _runtime;
        private readonly Session _session;

        public QuestGraph(Quest quest, INodeLoader nodeLoader)
        {
            Quest = quest;
            _storage = new Storage(quest, nodeLoader, OnNodeShouldEvaluate);
            _runtime = new Runtime(quest.Tag, _storage);
            _session = new Session(_storage, OnQuestCompleted);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">ID of the root, or suspended node in the chain. Either player cursor's Node or Root property have to match this value.<br/>
        /// <see cref="EvaluationPolicy.SkipToNextRoot"/> is the only exception, in this case ID defines the next root id.
        /// </param>
        /// <param name="policy">Override behavior on success</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Evaluate(int id, NwPlayer player, EvaluationPolicy policy = EvaluationPolicy.Default)
        {
            _log.Info($"QuestGraph Evaluate\nPlayer: {(player.IsValid ? player.PlayerName : "INVALID PLAYER")}\nID: {id}\nPolicy: {policy}");

            if(_storage[id] == null)
                throw new InvalidOperationException("First node of the chain must exist on the graph to start evaluation");

            var state = _session[player] ?? throw new InvalidOperationException("Session must contain player state for evaluation to start");



            if(policy == EvaluationPolicy.SkipToNextRoot)
            {
                _session.MovePlayer(player,id);
                return;
            }
            
            if(state.Cursor.Node != id && state.Cursor.Root != id)
                throw new InvalidOperationException("Player is on a different node or chain");
            

            var oldCursor = state.Cursor;

            var outcome = _runtime.EvaluateChain(state.Cursor,player);

            if(outcome.Result == Runtime.EvaluationResult.Error || 
                outcome.Result == Runtime.EvaluationResult.Failure ||
                outcome.Result == Runtime.EvaluationResult.Suspend)
                return;


            if(outcome.Result != Runtime.EvaluationResult.Success)
            {            
                if(outcome.VisitedNodes.Count == 0) 
                    throw new InvalidOperationException("Not a single node visited during evaluation");
                _session.ApplyOutcome(outcome, player);
                return;
            }

            if(policy == EvaluationPolicy.AutoProceed)
            {
                _session.ApplyOutcome(outcome, player);
                return;
            }

            // policy overrides are applied only on "Success" result
            switch(policy)
            {
                case EvaluationPolicy.SuspendOnLeaf: // surgically remove last node from the output
                    var lastNode = outcome.VisitedNodes[^1];
                    outcome.VisitedNodes.RemoveAt(outcome.VisitedNodes.Count-1);
                    _storage[lastNode]?.Reset(player);
                    _storage.NodeDecrement(lastNode);
                    outcome.Result = Runtime.EvaluationResult.Suspend;
                    outcome.NewPosition = (oldCursor.Root,outcome.VisitedNodes[^1]);
                break;

                case EvaluationPolicy.RollbackToRoot:
                    outcome.Result = Runtime.EvaluationResult.Rollback; // rollback triggers chain reset but preserves cached data
                    outcome.NewPosition = oldCursor;
                break;

                case EvaluationPolicy.ResetChain:
                    outcome.Result = Runtime.EvaluationResult.Failure; // failure triggers full chain reset
                    outcome.NewPosition = oldCursor;
                break;

                default: 
                    throw new ArgumentException("Invalid EvaluationPolicy " + policy);
            }
        }

        public static event Action<string, NwPlayer>? QuestCompleted;
        private void OnQuestCompleted(NwPlayer player) => QuestCompleted?.Invoke(Quest.Tag, player);
        private void OnNodeShouldEvaluate(INode node, NwPlayer player) => Evaluate(node.ID, player);

        public void Dispose()
        {
            _log.Info("Disposing quest graph " + Quest.ToString());
            _session.Dispose();
            _storage.Dispose();
        }




        public bool IsEmpty => _storage.Count == 0;
        public int GetRoot(NwPlayer player) => _session[player]?.Cursor.Root ?? -1;
        public INode? GetRootNode(NwPlayer player)
        {
            int id = GetRoot(player);
            if(id < 0) return null;
            return _storage[id];
        }

        public int[]? CaptureSnapshot(NwPlayer player) => _session[player]?.CaptureSnapshot() ?? null;

        public bool AddPlayer(NwPlayer player, int rootStage)
        {
            _log.Info("Adding player to graph session");
            if(!_session.MovePlayer(player, rootStage))
                return _session.EnterGraph(player, rootStage);
            return true;
        }
        public bool AddPlayer(NwPlayer player, int[] snapshot)
        {
            _log.Info("Adding player to graph session with snapshot");
            if(!_session.EnterGraph(player, snapshot))
            {
                return _session.ExitGraph(player)
                && _session.EnterGraph(player, snapshot);
            }
            return true;
        }
        public bool RemovePlayer(NwPlayer player){
            _log.Info("Removing player from graph");
            return _session.ExitGraph(player);
        }
    }
}