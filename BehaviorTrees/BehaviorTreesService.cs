using System;
using Anvil.API;
using Anvil.Services;
using BehaviorTrees.Core.Nodes;
using NLog;

namespace BehaviorTrees
{
  [ServiceBinding(typeof(BehaviorTreesService))]
  public class BehaviorTreesService : IDisposable
  {
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly TreeRunner _treeRunner;

    public BehaviorTreesService(SchedulerService scheduler)
    {
      _treeRunner = new(scheduler);
      NwModule.Instance.OnModuleLoad += _ => _treeRunner.Start();
    }


    /// <summary>
    /// Default OnHeartbeat handler
    /// </summary>
    [ScriptHandler("nw_c2_default1")]
    public ScriptHandleResult HandleOnHeartbeat(CallInfo info)
    {
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      //...

      return ScriptHandleResult.Handled;
    }

    /// <summary>
    /// Default OnPerception handler
    /// </summary>
    [ScriptHandler("nw_c2_default2")]
    public ScriptHandleResult HandleOnPerception(CallInfo info)
    {
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      var perceived = NWN.Core.NWScript.GetLastPerceived().ToNwObjectSafe<NwCreature>();

      if(perceived == null || !perceived.IsValid)
      {
        _log.Warn("Perceived game object is not a creature. Skipping.");
        return ScriptHandleResult.NotHandled;
      }

      

      return ScriptHandleResult.Handled;
    }


    /// <summary>
    /// Default EndOfCombatRound handler
    /// </summary>
    [ScriptHandler("nw_c2_default3")]
    public ScriptHandleResult HandleEndOfCombatRound(CallInfo info)
    {      
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      //...
      
      return ScriptHandleResult.Handled;
    }
    

    /// <summary>
    /// Default OnConversation handler
    /// </summary>
    [ScriptHandler("nw_c2_default4")]
    public ScriptHandleResult HandleOnConversation(CallInfo info)
    {      
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      //...
      
      return ScriptHandleResult.Handled;
    }

    
    /// <summary>
    /// Default OnAttacked handler
    /// </summary>
    [ScriptHandler("nw_c2_default4")]
    public ScriptHandleResult HandleOnAttacked(CallInfo info)
    {      
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      //...
      
      return ScriptHandleResult.Handled;
    }

        
    /// <summary>
    /// Default OnDamaged handler
    /// </summary>
    [ScriptHandler("nw_c2_default5")]
    public ScriptHandleResult HandleOnDamaged(CallInfo info)
    {      
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      //...
      
      return ScriptHandleResult.Handled;
    }

    static object? GetBehaviorTreeRootForCreature(NwCreature? creature)
    {
      return ServerData.DataProviders.BehaviorTreesProvider.GetBehaviorTreeRootForCreature(creature);
    }

    /// <summary>
    /// Default OnSpawn handler
    /// </summary>
    [ScriptHandler("nw_c2_default7")]
    [ScriptHandler("nw_c2_default9")]
    public ScriptHandleResult HandleOnSpawn(CallInfo info)
    {      
      _log.Info("ON SPAWN");

      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped. ");
        return ScriptHandleResult.Handled;
      }

      if(creature.Master != null || creature.ControllingPlayer != null || creature.IsDMAvatar || creature.IsDMPossessed) // skip attaching behaviors to player or dm controlled creatures
      {
        _log.Info("Skipping player-controlled creature (fallback to default)");
        return ScriptHandleResult.NotHandled;
      }

      // if(creature.AiLevel < AiLevel.Low)
      // {
      //   _log.Info("Skipping low ai level creature (fallback to default)");
      //   return ScriptHandleResult.NotHandled;
      // }

      var root = GetBehaviorTreeRootForCreature(creature);

      if(root == null)
      {
        _log.Warn("No root! Fallback to default");
        return ScriptHandleResult.NotHandled;
      }

      if(root is not Node node)
      {
        var rTypeName = root.GetType().Name;
        var rAsmLocation = root.GetType().Assembly.Location;

        var nTypeName = typeof(Node).Name;
        var nAsmLocation = typeof(Node).Assembly.Location;

        _log.Error($"ROOT IS NOT A NODE. \nRoot: {rTypeName}, {rAsmLocation}\nNode: {nTypeName}, {nAsmLocation}");
        return ScriptHandleResult.Handled;
      }

      creature.RegisterBehaviorTree(node);

      _log.Info("Registered behavior tree for creature. Root node type: " + root.GetType().Name);
      
      return ScriptHandleResult.Handled;
    }

    /// <summary>
    /// Default OnSpellcastAt handler
    /// </summary>
    [ScriptHandler("nw_c2_defaultb")]
    public ScriptHandleResult HandleOnSpellcastAt(CallInfo info)
    {      
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      //...
      
      return ScriptHandleResult.Handled;
    }

    
    /// <summary>
    /// Default OnUserDefined handler
    /// </summary>
    [ScriptHandler("nw_c2_defaultd")]
    public ScriptHandleResult HandleOnUserDefined(CallInfo info)
    {      
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      //...
      
      return ScriptHandleResult.Handled;
    }

        
    /// <summary>
    /// Default OnBlocked handler
    /// </summary>
    [ScriptHandler("nw_c2_defaulte")]
    public ScriptHandleResult HandleOnBlocked(CallInfo info)
    {      
      var creature = info.ObjectSelf as NwCreature;
      if(creature == null || !creature.IsValid)
      {
        _log.Error("Invalid creature, Behavior Tree skipped.");
        return ScriptHandleResult.NotHandled;
      }

      //...
      
      return ScriptHandleResult.Handled;
    }

        public void Dispose()
        {
            _treeRunner.Dispose();
        }
    }
}