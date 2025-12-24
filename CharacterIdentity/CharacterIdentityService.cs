using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

using MySQLClient;

using EasyConfig;

using CharacterIdentity.Configuration;
using CharactersRegistry;
using CharacterAppearance;
using System;


namespace CharacterIdentity
{
    [ServiceBinding(typeof(CharacterIdentityService))]
    public sealed class CharacterIdentityService
    {
        internal static CharacterIdentityServiceConfig ServiceConfig { get; private set; } = new();
        internal static IdentityEditorConfig IdentityEditorConfig { get; private set; } = new();
        internal static PolymorphCreatureNamesConfig PolymorphCreatureNamesConfig { get; private set; } = new();

        private readonly CharactersRegistryService _charactersRegistry;


        public CharacterIdentityService(
            EventService eventService,
            MySQLService mySQL,
            ConfigurationService easyCfg,
            PortraitStorageService portraitStorage,
            CharactersRegistryService charactersRegistry,
            CharacterAppearanceService characterAppearance
        )
        {
            ServiceConfig = easyCfg.GetConfig<CharacterIdentityServiceConfig>();
            IdentityEditorConfig = easyCfg.GetConfig<IdentityEditorConfig>();
            PolymorphCreatureNamesConfig = easyCfg.GetConfig<PolymorphCreatureNamesConfig>();

            IdentityManager.EventService = eventService;
            IdentityManager.PortraitStorage = portraitStorage;
            IdentityManager.MySQL = mySQL;
            IdentityManager.CharacterAppearance = characterAppearance;

            _charactersRegistry = charactersRegistry;

            NwModule.Instance.OnClientEnter += OnClientEnter;
            NwModule.Instance.OnClientLeave += OnClientLeave;
            NwModule.Instance.OnClientDisconnect += OnClientDisconnect;

            CharacterAppearanceService.OnBodyAppearanceEditComplete += OnBodyAppearanceEditComplete;
        }

        void OnBodyAppearanceEditComplete(NwPlayer player, bool applyChanges)
        {
            var cis = CharacterIdentityState.GetState(player);
            if(cis == null) return;
            var id = cis.ActiveIdentity.ID;

            var pc = player.LoginCreature;
            if(pc == null || pc != player.ControlledCreature) return;

            if (applyChanges)
            {
                IdentityManager.CharacterAppearance.SaveBodyAppearance(id, pc);
            }
            else
            {
                IdentityManager.CharacterAppearance.LoadBodyAppearance(id, pc);
            }
        }


        private void OnClientEnter(ModuleEvents.OnClientEnter eventData)
        {
            OnClientEnterAsync(eventData.Player);
        }

        private async void OnClientEnterAsync(NwPlayer player)
        {
            if(!_charactersRegistry.KickPlayerIfCharacterNotRegistered(player, out var pc))
                return;

            await NwTask.Delay(TimeSpan.FromSeconds(0.2));

            if (!player.IsValid || !pc.IsValid || !IdentityManager.EnsureTrueIdentity(pc))
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Player and/or PC invalidated while checking the CharactersRegistry");
                if(player.IsValid) player.BootPlayer("Błąd serwera. Nie udało się zapewnić prawdziwej tożsamości dla postaci " + (pc.IsValid ? pc.OriginalFirstName : "<nieprawidłowa postać>"));
                return;
            }

            await NwTask.Delay(TimeSpan.FromSeconds(0.2));

            if (!player.IsValid || !pc.IsValid || !IdentityManager.EnsureActiveIdentity(pc))
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Player and/or PC invalidated while ensuring true identity.");
                if(player.IsValid) player.BootPlayer("Błąd serwera. Nie udało się ustalić aktywnej tożsamości postaci " + (pc.IsValid ? pc.OriginalFirstName : "<nieprawidłowa postać>"));
                return;
            }

            await NwTask.Delay(TimeSpan.FromSeconds(0.2));

            if(!player.IsValid || !pc.IsValid)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Player and/or PC invalidated while ensuring active identity.");
                if(player.IsValid) player.BootPlayer("Błąd serwera. Nie udało się ustalić aktywnej tożsamości postaci " + (pc.IsValid ? pc.OriginalFirstName : "<nieprawidłowa postać>"));
                return;
            }
            
            CharacterIdentityState.CreateForPlayer(player);
        }

        static void OnClientLeave(ModuleEvents.OnClientLeave data) => CharacterIdentityState.ClearFromPlayer(data.Player);
        static void OnClientDisconnect(OnClientDisconnect data) => CharacterIdentityState.ClearFromPlayer(data.Player);

    }
}