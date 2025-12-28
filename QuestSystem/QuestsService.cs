using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

using CharactersRegistry;
using MySQLClient;
using QuestSystem.Objectives;

namespace QuestSystem
{
    [ServiceBinding(typeof(QuestsService))]
    internal sealed class QuestsService
    {
        private readonly MySQLService _mySQL;
        private readonly CharactersRegistryService _charactersRegistry;

        //tmp:
        async void InitializeTestAsync(string questPacksPath)
        {            
            /// create test quest pack
            using (var newpack = QuestPack.OpenWrite(Path.Combine(questPacksPath, "testQuestPack"+QuestPack.FileExtension)))
            {
                var newquest = new Quest();
                newquest.Tag = "test_quest_1";
                newquest.Name = "Test Quest";
                newquest.JournalEntry = " Test Quest Journal Entry";
                
                await newpack.AddQuestAsync(newquest);
                
                var stage1 = new QuestStage();
                stage1.ID = 0;
                stage1.NextStageID = 1;
                stage1.JournalEntry = "Stage 1 Journal Entry";
                var obj1 = new ObjectiveInteract();
                obj1.Interaction = ObjectiveInteract.InteractionType.ObjectExamine;
                obj1.Tag = "PLC_BTLR_MIRROR";
                obj1.ResRef = "btlr_mirror";
                obj1.PartyMembersAllowed = false;
                obj1.AreaTags = new string[]{"_test_area"};
                obj1.JournalEntry = "Objective journal entry";

                stage1.Objectives = new Objective[]{obj1};

                await newpack.SetStageAsync(newquest.Tag,stage1);

                string s = "ENTRIES:\n";
                foreach(var e in newpack.Entries)
                {

                    using (var sr = new StreamReader(e.Open()))
                    {
                        
                        s += e.FullName + " : \n " + sr.ReadToEnd();
                    }
                }

                NLog.LogManager.GetCurrentClassLogger().Warn(s);
            }

            await NwTask.SwitchToMainThread();
            
            QuestPackManager.GetPacks(questPacksPath); 

            var pack = QuestPackManager.FindPack("test_quest_1") ?? throw new Exception("pack not found by quest tag");

            var quest = await pack.GetQuestAsync("test_quest_1") ?? throw new Exception("pack does not have test quest");

            var stage = await pack.GetStageAsync(quest.Tag, 0) ?? throw new Exception("stage 1 not found in test quest in pack");

            var obj = stage.Objectives.FirstOrDefault() ?? throw new Exception("No objectives in stage");

            foreach(var o in stage.Objectives)
                o.QuestStage = stage;

            stage.Quest = quest;

            string str = $@"QUEST TAG: {quest.Tag}
            QUEST NAME: {quest.Name}
            QUEST JOURNAL ENTRY: {quest.JournalEntry}
            STAGE ID: {stage.ID}
            NEXT STAGE ID: {stage.NextStageID}
            STAGE JOURNAL ENTRY: {stage.JournalEntry}
            OBJECTIVE TYPE: {obj.GetType().Name}
            OBJECTIVE NEXT STAGE ID: {obj.NextStageID}
            OBJECTIVE JOURNAL ENTRY: {obj.JournalEntry}
            OBJECTIVE PARTY MEMBERS ALLOWED: {obj.PartyMembersAllowed}
            REWARD XP: {stage.Reward.Xp}
            REWARD GOLD: {stage.Reward.Gold}
            REWARD GE CHANGE: {stage.Reward.GoodEvilChange}
            REWARD LC CHANGE: {stage.Reward.LawChaosChange}
            REWARD ITEMS: {string.Join(',',stage.Reward.Items.Keys)}
            REWARD ITEMS AMOUNTS: {string.Join(',',stage.Reward.Items.Values)}";

            NLog.LogManager.GetCurrentClassLogger().Warn("DEBUGGING QUEST READ FROM PACK:\n"+str);
        }
        public QuestsService(MySQLService mySQL, CharactersRegistryService charactersRegistry, PluginStorageService pluginStorage)
        {
            string questPacksPath = pluginStorage.GetPluginStoragePath(typeof(QuestsService).Assembly);

            //QuestPackManager.GetPacks(questPacksPath);

            _mySQL = mySQL;
            _charactersRegistry = charactersRegistry;

            NwModule.Instance.OnClientEnter += OnClientEnter;
            NwModule.Instance.OnClientLeave += OnClientLeave;
        
            // testing:
            InitializeTestAsync(questPacksPath);
        }

        void OnClientEnter(ModuleEvents.OnClientEnter data)
        {
            LoadQuestsAsync(data.Player);
        }

        async void LoadQuestsAsync(NwPlayer player)
        {
            // todo: read pc quests from database, load current quest stages into memory

            // test:
            var testTag = "test_quest_1";
            var testStage = 2;

            var result = await SetPCOnQuestAsync(player, testTag, testStage);

            if(!result) NLog.LogManager.GetCurrentClassLogger().Error($"Failed to set PC on stage {testStage} of quest {testTag}");
            else NLog.LogManager.GetCurrentClassLogger().Warn($"Successfully set PC on stage {testStage} of quest {testTag}");
        }

        void OnClientLeave(ModuleEvents.OnClientLeave data)
        {
            Quest.ClearPlayer(data.Player);
        }

        static async ValueTask<bool> SetPCOnQuestAsync(NwPlayer player, string questTag, int stageID)
        {
            if(!player.IsValid) return false;

            var questStage = await GetOrLoadStageAsync(questTag, stageID);
            await NwTask.SwitchToMainThread();

            if(questStage == null) return false;

            if (!player.IsValid)
            {
                Quest.ClearPlayer(player);
                return false;
            }

            questStage.TrackProgress(player);

            return true;
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