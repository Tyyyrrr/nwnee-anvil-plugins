using System;
using System.Collections.Generic;
using Anvil.API;

namespace QuestSystem.Graph
{
    internal sealed partial class QuestGraph
    {
        /// <summary>
        /// Responsible for evaluating node chains.
        /// </summary>
        private sealed partial class Runtime : GraphComponent
        {
            private readonly ChainEvaluator _evaluator;
            private readonly Storage _storage;

            public sealed class EvaluationOutcome
            {
                public readonly List<int> VisitedNodes = new();
                public PlayerCursor OldPosition {get;set;}
                public PlayerCursor NewPosition {get;set;}
                public EvaluationResult Result {get;set;}
            }
            
            public enum EvaluationResult
            {
                Error,
                Success,
                Failure,
                Rollback,
                Suspend,
                Complete
            }
                
            private sealed class ChainEvaluator
            {
                private readonly Func<PlayerCursor,INode?> _nodeGetter;

                public PlayerCursor Cursor {get;private set;}

                public ChainEvaluator(Func<PlayerCursor, INode?> nodeGetter)
                {
                    _nodeGetter = nodeGetter;
                }

                public event Action<int>? NodeVisited;

                /// <summary>
                /// Traverse the chain one node after another.<br/>
                /// If the evaluation fails at any point, the next re-evaluation will start from the last "rollback" node that succeeded.
                /// </summary>
                public EvaluationResult Evaluate(NwPlayer player, PlayerCursor initPos)
                {
                    Cursor = initPos;
                    int rollback = initPos.Node;
                    bool started = false;

                    for(int i = 0; i < QuestGraph.MaxChainLength; i++)
                    {
                        if(!Cursor.IsOnGraph) 
                            break;

                        var node = _nodeGetter(Cursor.Node);

                        if(node == null) 
                            break;

                        if(node.IsRoot) // do not evaluate root nodes but the initial one (if the initial node is a root node)
                        {
                            if (started)
                            {
                                Cursor = node.ID;
                                return EvaluationResult.Success;
                            }

                            started = true;
                        }
                        
                        if(i > 0) NodeVisited?.Invoke(node.ID); // don't "touch" initial node. It is already counted.

                        if (!node.Evaluate(player))
                        {
                            Cursor = (initPos.Root, rollback);
                            return node.IsRoot ? EvaluationResult.Failure : EvaluationResult.Rollback;
                        }

                        switch (node.NextID)
                        {
                            case -1:
                                Cursor = (initPos.Root, node.ID);
                                return EvaluationResult.Suspend;
                            
                            case -2: 
                                Cursor = PlayerCursor.None;
                                return EvaluationResult.Complete;

                            default:
                                Cursor = (initPos.Root, node.NextID);
                                rollback = node.Rollback ? node.ID : rollback;
                                break;
                        }
                    }
                    
                    return EvaluationResult.Error;
                }
            }

            public Runtime(string tag, Storage storage) : base(tag)
            {
                _storage = storage;
                _evaluator = new(_storage.GetOrCreateNode);
            }

            public EvaluationOutcome EvaluateChain(PlayerCursor initialPosition, NwPlayer player)
            {
                var outcome = new EvaluationOutcome(){OldPosition = initialPosition};
                _evaluator.NodeVisited += outcome.VisitedNodes.Add;
                try
                {
                    outcome.Result = _evaluator.Evaluate(player, initialPosition);
                    outcome.NewPosition = _evaluator.Cursor;
                    if(outcome.Result == EvaluationResult.Error)
                        _log.Error($"Failed to evaluate chain for the player starting from node {initialPosition} of quest \'{Tag}\'. Error occurred at node {_evaluator.Cursor.Node}");
                    return outcome;
                }
                finally
                {
                    _evaluator.NodeVisited -= outcome.VisitedNodes.Add;
                }
            }

            public override void Dispose()
            {
                // nothing to dispose
            }
        }
    }
}