using System.Threading.Tasks;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

using CharactersRegistry;
using MySQLClient;

namespace QuestSystem
{
    [ServiceBinding(typeof(QuestsService))]
    internal sealed class QuestsService
    {
        private readonly MySQLService _mySQL;
        private readonly CharactersRegistryService _charactersRegistry;


        public QuestsService(MySQLService mySQL, CharactersRegistryService charactersRegistry, PluginStorageService pluginStorage)
        {
            QuestPackManager.GetPacks(pluginStorage.GetPluginStoragePath(typeof(QuestsService).Assembly));

            _mySQL = mySQL;
            _charactersRegistry = charactersRegistry;

            NwModule.Instance.OnClientEnter += OnClientEnter;
            NwModule.Instance.OnClientLeave += OnClientLeave;
        }

        void OnClientEnter(ModuleEvents.OnClientEnter data)
        {
            var player = data.Player;

            if(!_charactersRegistry.KickPlayerIfCharacterNotRegistered(player, out var pc))
                return;

            LoadQuestsAsync(pc);
        }

        async void LoadQuestsAsync(NwCreature pc)
        {
            // todo: read pc quests from database, load current quest stages into memory

            // test:
            var testTag = "test_quest_1";
            var testStage = 2;

            var result = await SetPCOnQuestAsync(pc, testTag, testStage);

            if(!result) NLog.LogManager.GetCurrentClassLogger().Error($"Failed to set PC on stage {testStage} of quest {testTag}");
            else NLog.LogManager.GetCurrentClassLogger().Warn($"Successfully set PC on stage {testStage} of quest {testTag}");
        }

        void OnClientLeave(ModuleEvents.OnClientLeave data)
        {
            var pc = data.Player.LoginCreature;

            if(pc != null) Quest.ClearPlayer(pc);
        }

        static async ValueTask<bool> SetPCOnQuestAsync(NwCreature pc, string questTag, int stageID)
        {
            var player = pc.ControllingPlayer;
            if(player == null || player.LoginCreature != pc || !pc.IsValid) return false;

            var questStage = await GetOrLoadStageAsync(questTag, stageID);
            await NwTask.SwitchToMainThread();

            if(questStage == null) return false;

            if (!pc.IsValid)
            {
                Quest.ClearPlayer(pc);
                return false;
            }

            questStage.TrackProgress(pc);
        }

        static async ValueTask<QuestStage?> GetOrLoadStageAsync(string questTag, int stageID)
        {
            // fast path (quest stage can be already cached in memory):
            var quest = Quest.GetQuest(questTag);

            if(quest != null)
            {
                var stage = quest.GetStage(stageID);

                if(stage != null) return stage;
            }

            var pack = QuestPackManager.FindPack(questTag);

            if(pack == null) return null; // return if none of packs


            // slow path (read from .zip archive and store quest and questStage in RAM asynchronously):
            if(quest != null)
            {
                var stage = await pack.GetStageAsync(questTag, stageID);
                if(stage == null) return null;

                quest.RegisterStage(stage);

                return stage;
            }
            else
            {
                quest = await pack.GetQuestAsync(questTag);
                if(quest == null) return null;

                var stage = await pack.GetStageAsync(questTag, stageID);
                if(stage == null) return null;

                Quest.RegisterQuest(quest);
                quest.RegisterStage(stage);

                return stage;
            }
        }
    }
}