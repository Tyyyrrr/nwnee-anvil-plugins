using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;

namespace QuestSystem.Graph
{
    internal sealed partial class QuestGraph
    {
        /// <summary>
        /// Responsible to tracking player states on the graph.
        /// </summary>
        private sealed class Session : GraphComponent
        {
            private readonly Dictionary<NwPlayer, PlayerState> _playerStates = new();

            private readonly Storage _storage;
            
            private readonly Action<NwPlayer> _completeQuestCallback;
            public Session(string tag, Storage storage, Action<NwPlayer> completeQuestCallback) : base(tag)
            {
                _storage = storage;
                _completeQuestCallback = completeQuestCallback;
            }

            public PlayerState? this[NwPlayer player] => _playerStates.TryGetValue(player, out var state) ? state : null;

            public sealed class PlayerState
            {
                public PlayerCursor Cursor {get;set;}

                private readonly Stack<PlayerCursor> chainsCompleted = new();
                private readonly Stack<int> currentChainFootprints = new();

                public IEnumerable<int> Footprints => currentChainFootprints;
                public int FootprintsCount => currentChainFootprints.Count;
                
                private Runtime.EvaluationResult lastEvaluationResult = Runtime.EvaluationResult.Success;

                public int[] CaptureSnapshot()
                {
                    int completedCount = chainsCompleted.Count;
                    int footprintCount = currentChainFootprints.Count;

                    int chainSectionLength = completedCount * 2;
                    int footprintStart = 1 + chainSectionLength;
                    int totalLength = footprintStart + footprintCount + 1;

                    var array = new int[totalLength];

                    array[0] = footprintStart;

                    int idx = 1;
                    foreach (var cursor in chainsCompleted)
                    {
                        array[idx++] = cursor.Root;
                        array[idx++] = cursor.Node;
                    }

                    foreach (var nodeId in currentChainFootprints)
                    {
                        array[idx++] = nodeId;
                    }

                    array[^1] = (int)lastEvaluationResult;

                    return array;
                }

                public static PlayerState CreateNew(int rootNode) => RestoreSnapshot(new int[]{1, rootNode, (int)Runtime.EvaluationResult.Success});
                public static PlayerState RestoreSnapshot(IReadOnlyList<int> snapshot)
                {
                    if(snapshot.Count < 3) 
                        throw new ArgumentException("Invalid snapshot. It must contain at least a single footprint, evaluation result and metadata");

                    if(!Enum.IsDefined(typeof(Runtime.EvaluationResult), snapshot[^1]))
                        throw new ArgumentException("Invalid snapshot. Unknown value for last evaluation result.");

                    var ps = new PlayerState();
                    for(int i = 1; i < snapshot.Count -1; i++)
                    {
                        if(i<snapshot[0])
                        {
                            ps.PushChain((snapshot[i], snapshot[i+1]));
                            i++;
                        }
                        else ps.PushNode(snapshot[i]);
                    }
                    ps.Cursor = (snapshot[snapshot[0]],snapshot[^2]);
                    ps.SetResult((Runtime.EvaluationResult)snapshot[^1]);
                    return ps;
                }

                public void PushChain(PlayerCursor cursor) => chainsCompleted.Push(cursor);
                public void PushNode(int id) => currentChainFootprints.Push(id);
                public int PopNode() => currentChainFootprints.Pop();
                public void SetResult(Runtime.EvaluationResult result) => lastEvaluationResult = result;
            }

            /// <summary>
            /// Essentially "set" the player on this quest.
            /// </summary>
            /// <param name="player">Entering player</param>
            /// <param name="rootNode">Initial root node to set the player on.</param>
            public bool EnterGraph(NwPlayer player, int rootNode)
            {
                if(!player.IsValid || _playerStates.ContainsKey(player))
                    return false;

                _log.Info($"Player {player.PlayerName} entering quest \'{Tag}\' graph at root node {rootNode}");

                var node = _storage.GetOrCreateNode(rootNode);

                if(node == null) return false;

                _storage.NodeIncrement(rootNode);

                if(!node.IsRoot)
                {
                    _storage.NodeDecrement(rootNode);
                    return false;
                }

                _playerStates.Add(player, PlayerState.CreateNew(rootNode));

                return true;
            }

            /// <inheritdoc cref="EnterGraph(NwPlayer, int)"/>
            /// <param name="snapshot">Track of the player progress on this quest. Use <see cref="CaptureSnapshot(NwPlayer)"/> to get a strongly typed serializable instance.</param>
            /// <returns></returns>
            public bool EnterGraph(NwPlayer player, IReadOnlyList<int> snapshot)
            {
                if(!player.IsValid || _playerStates.ContainsKey(player))
                    return false;

                var state = PlayerState.RestoreSnapshot(snapshot);

                List<INode> touchedNodes = new();
                foreach(var footprint in state.Footprints)
                {
                    var node = _storage.GetOrCreateNode(footprint);
                    if(node == null)
                    {
                        foreach(var tn in touchedNodes)
                        {
                            _storage.NodeDecrement(tn.ID);
                        }
                        return false; 
                    }
                    touchedNodes.Add(node);
                    _storage.NodeIncrement(node.ID);
                }

                _playerStates.Add(player, state);
                return true;
            }

            /// <summary>
            /// Clear the player from this graph, and release any orphaned resources.
            /// </summary>
            public bool ExitGraph(NwPlayer player)
            {
                if(!_playerStates.TryGetValue(player, out var state))
                    return false;

                foreach(var footprint in state.Footprints)
                    _storage.NodeDecrement(footprint);
                
                _playerStates.Remove(player);
                
                return true;
            }

            public override void Dispose()
            {
                var players = _playerStates.Keys.ToArray();

                foreach(var player in players)
                    _ = ExitGraph(player);
            }

            public void ApplyOutcome(Runtime.EvaluationOutcome outcome, NwPlayer player)
            {
                var state = _playerStates[player];

                var result = outcome.Result;

                state.SetResult(result);

                var oldPos = outcome.OldPosition;
                var newPos = outcome.NewPosition;

                state.Cursor = newPos;

                switch(result)
                {
                    // reached a root node
                    case Runtime.EvaluationResult.Success:

                        if(oldPos.Root == newPos.Root)
                        {
                            while (state.FootprintsCount > 1)
                            {
                                var fp = state.PopNode();
                                _storage.NodeDecrement(fp);
                            }
                        }
                        else
                        {
                            _storage.NodeIncrement(newPos.Root);
                            state.PushChain(oldPos);
                            while(state.FootprintsCount > 0)
                            {
                                var fp = state.PopNode();
                                _storage.NodeDecrement(fp);
                            }
                            state.PushNode(newPos.Root);
                        }
                    break;

                    // root node failed to evaluate
                    case Runtime.EvaluationResult.Failure:
                        while(state.FootprintsCount > 1) // rollback to old cursor position, or root node
                        {
                            var fp = state.PopNode();
                            if(fp == oldPos.Node)
                            {
                                state.PushNode(fp);
                                break;
                            }
                            _storage.NodeDecrement(fp);
                        }
                    break;

                    // some child node in the chain failed to evaluate
                    case Runtime.EvaluationResult.Rollback:                    
                        while(state.FootprintsCount > 1) // rollback to the new cursor position, or root node
                        {
                            var fp = state.PopNode();
                            if(fp == newPos.Node)
                            {
                                state.PushNode(fp);
                                break;
                            }
                            _storage.NodeDecrement(fp);
                        }
                    break;

                    // reached a sticky node
                    case Runtime.EvaluationResult.Suspend: 
                        // do nothing, the state is "frozen" now
                        break;

                    // reached a terminal node
                    case Runtime.EvaluationResult.Complete:
                        state.PushChain(newPos);
                        while (state.FootprintsCount > 0)
                        {
                            var fp = state.PopNode();
                            _storage.NodeDecrement(fp);
                        }
                        _completeQuestCallback(player); // notify about completion
                        break;

                    // some error occurred (Cursor and result are already set)
                    default: break;
                }
            }
        }
    }
}