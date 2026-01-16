using System;
using Anvil.API;

namespace QuestSystem.Graph
{
    internal interface INode : IDisposable
    {
        /// <summary>
        /// Key/Index of this node. Unique in the context of a quest graph
        /// </summary>
        public int ID {get;}

        /// <summary>
        /// Index of the next node in evaluation chain.<br/>
        /// Possible values:
        /// <list type="bullet">
        /// <item><b>-2:</b> Quest will be completed on the root node of this chain</item>
        /// <item><b>-1:</b> Player will stay indefinitely on this node until being moved to another position on the graph.<br/>
        /// Re-evaluation resets the progress of this node.</item>
        /// <item><b>>0:</b> A regular "go to" node</item>
        /// </list>
        /// </summary>
        public int NextID {get;}

        /// <summary>
        /// Triggered when a chain with this node should be evaluated for the player.
        /// <br/>This is the only outward signal a node emits.<br/>
        /// Nodes never evaluate themselves; they only request evaluation, and the graph decides when to run it.
        /// </summary>
        public event Action<INode, NwPlayer>? ShouldEvaluate;
        
        /// <summary>
        /// The "root" node can't have parent. Evaluation leading to such node shall be terminated and return success result.<br/>
        /// Root nodes are the only valid entry points to manually run the sequence (for example from a DM command).
        /// <br/><br/>This is the only structural metadata exposed by nodes. All other structural relationships are managed by the graph.
        /// </summary>
        public bool IsRoot {get;}

        /// <summary>
        /// Indicates whether this node is safe to roll back from failed evaluation. Re-evaluation will strart from the last rollback node.
        /// <br/><br/>This is the only evaluation‑flow hint a node provides. Rollback nodes define where evaluation resumes after failure.
        /// </summary>
        public bool Rollback {get;}

        /// <summary>
        /// Perform arbitrary operation for the player and return if it succeeded or failed.
        /// <br/><br/>This is the only behavior a node performs. 
        /// All state transitions, refcounting, and graph navigation are handled externally by the graph.
        /// </summary>
        /// <param name="nextId">Next node in the chain if evaluation was successfull</param>
        /// <returns></returns>
        public bool Evaluate(NwPlayer player, out int nextId);

        /// <summary>
        /// Release all data cached for this player.
        /// </summary>
        public void Reset(NwPlayer player);

        /// <summary>
        /// Initialize node storage for this player (optional)
        /// </summary>
        public void Enter(NwPlayer player);
    }
}