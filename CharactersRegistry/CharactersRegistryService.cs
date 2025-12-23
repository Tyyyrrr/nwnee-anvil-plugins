
using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

using Anvil.API;
using Anvil.Services;
using NLog;

using ServerData;
using MySQLClient;


namespace CharactersRegistry
{

  [ServiceBinding(typeof(CharactersRegistryService))]
  public class CharactersRegistryService
  {
    private const string CHARACTERS_REGISTRY_FLAG = "CharactersRegistryFlag";

    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly MySQLService _mySQL;

    private static bool TryGetPlayerCharacterGuidAndKickPlayerOnFailure(NwPlayer player, NwCreature character, out Guid guid)
    {
      try
      {
        if (!character.TryGetUUID(out guid))
        {
          _log.Warn($"Character {character.Name} has no UUID. - Forcing refresh.");
          character.ForceRefreshUUID();
          if (!character.HasUUID)
          {
            _log.Error("Failed to force refresh object uuid.");
            player.BootPlayer($"Serwer napotkał problem techniczny.");
            return false;
          }
          else guid = character.UUID;
        }
        return true;
      }
      catch (Exception ex)
      {
        _log.Error($"Character {character.Name} has invalid UUID. - {ex.Message}");
        player.BootPlayer("Błąd serwera. Spróbuj dołączyć ponownie.");
        guid = default;
        return false;
      }
    }

    /// <summary>
    /// Check if the character is registered. Kick with error message on failure.
    /// </summary>
    /// <returns>True if everything went smooth. False if the client has been removed from the server, or is DM.</returns>
    public bool KickPlayerIfCharacterNotRegistered(NwPlayer player, [NotNullWhen(true)] out NwCreature? playerCharacter)
    {
      if (player.IsDM)
      {
        playerCharacter = null;
        return false;
      }

      playerCharacter = player.LoginCreature;

      if (playerCharacter == null)
      {
        _log.Error($"No character under control of player {player.PlayerName}");
        player.BootPlayer($"Serwer napotkał problem techniczny.");
        return false;
      }

      if (playerCharacter != player.ControlledCreature)
      {
        _log.Warn("Returned player character will not be the currently controlled creature.");
      }

      if (playerCharacter.GetObjectVariable<LocalVariableBool>(CHARACTERS_REGISTRY_FLAG).Value)
        return true;

      if (!TryGetPlayerCharacterGuidAndKickPlayerOnFailure(player, playerCharacter, out Guid guid))
        return false;

      var uuidStr = guid.ToUUIDString();

      var sqlMap = DataProviders.PlayerSQLMap;

      var builder = _mySQL.QueryBuilder;

      builder.Select(sqlMap.TableName, sqlMap.Character).Where(sqlMap.UUID, uuidStr).Limit(2);
      using var result = _mySQL.ExecuteQuery();
      var count = result.Count();
      switch (count)
      {
        case 0:
          player.BootPlayer("Nie znaleziono postaci w bazie danych serwera. Spróbuj dołączyć ponownie.");
          return false;

        case 1:
          playerCharacter.GetObjectVariable<LocalVariableBool>(CHARACTERS_REGISTRY_FLAG).Value = true;
          return true;

        case -1:
          _log.Error("Failed to execute query: " + builder.Build());
            player.BootPlayer($"Serwer napotkał problem techniczny.");
          return false;

        default:
          _log.Warn("Multiple database entries for the same character: " + playerCharacter.Name);
          player.BootPlayer("Błąd bazy danych. Skontaktuj się z administracją serwera.");
          return false;
      }


    }


    public CharactersRegistryService(MySQLService mySQL)
    {
      _mySQL = mySQL;
    }

  }
}