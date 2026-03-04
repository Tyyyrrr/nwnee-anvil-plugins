using System.Collections.Generic;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using CharactersRegistry;

namespace PCTimers
{
  [ServiceBinding(typeof(PCTimersService))]
  public class PCTimersService
  {
    private readonly Dictionary<NwCreature, PCTimer> _timers = new();
    private readonly CharactersRegistryService _charReg;
    public PCTimersService(CharactersRegistryService charReg)
    {
      _charReg = charReg;

      NwModule.Instance.OnClientEnter += OnClientEnter;
      NwModule.Instance.OnClientLeave += OnClientLeave;
      NwModule.Instance.OnHeartbeat += OnModuleHB;
    }

    void OnClientEnter(ModuleEvents.OnClientEnter data)
    {
      var player = data.Player;

      if(!_charReg.KickPlayerIfCharacterNotRegistered(player, out var pc))
        return;

      if(_timers.TryGetValue(pc, out var timer))
        timer.Reset();
      else
      {
        timer = new(pc);
        _timers.Add(pc,timer);
      }

    }

    void OnClientLeave(ModuleEvents.OnClientLeave data)
    {
      var pc = data.Player.LoginCreature;
      if(pc == null) return;
      _ = _timers.Remove(pc);
    }

    void OnModuleHB(ModuleEvents.OnHeartbeat _)
    {
      List<NwCreature> keysToRemove = new();
      foreach(var kvp in _timers)
      {
        var pc = kvp.Key;
        if (!pc.IsValid)
        {
          keysToRemove.Add(pc);
          continue;
        }

        kvp.Value.Tick();
      }

      foreach(var ktr in keysToRemove)
      {
        _timers.Remove(ktr);
      }
    }
  }
}