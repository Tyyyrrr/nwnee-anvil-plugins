using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anvil.API;
using QuestSystem.Nodes;

namespace QuestSystem.Wrappers.Nodes
{
    internal sealed class VisibilityNodeWrapper : NodeWrapper<VisibilityNode>
    {
        public VisibilityNodeWrapper(VisibilityNode node) : base(node){}

        protected override void ProtectedDispose()
        {
            // noting to dispose atm
        }

        protected override bool ProtectedEvaluate(NwPlayer player, out int nextId)
        {
            nextId = NextID;

            var currentArea = player.ControlledCreature?.Area;

            if(currentArea != null) // update current area first for immediate effects
            {
                foreach(var kvp in Node.Objects)
                {
                    var splitKey = kvp.Key.Split(':');
                    var resRef = splitKey[0];
                    var tag = splitKey.Length > 1 ? splitKey[1] : string.Empty;

                    var mode = kvp.Value ? Anvil.Services.VisibilityMode.Visible : Anvil.Services.VisibilityMode.Hidden;

                    foreach(var obj in currentArea.Objects)
                    {
                        if(obj.ResRef == resRef && (tag == string.Empty || obj.Tag == tag))
                        {
                            player.SetPersonalVisibilityOverride(obj,mode);
                        }
                    }
                }
            }

            // Update other areas asynchronously
            // this one is truly fire-forget because VisibilityNode will be disposed immediately
            _ = NwTask.Run(async () =>
            {
                try { await UpdateAreasAsyncTask(player, Node.Objects.ToDictionary(), currentArea?.Tag); }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error("Exception from async task: " + ex.Message + "\n" + ex.StackTrace);
                }
            });
            
            return true;
        }

        private Task UpdateAreasAsyncTask(NwPlayer player, Dictionary<string, bool> visibility, string? skipAreaTag = null)
        {
            // var sw = new Stopwatch();
            // sw.Start();

            // int testedObjects = 0;
            // int visitedAreas = 0;
            // int updatedObjects = 0;

            foreach(var area in NwModule.Instance.Areas)
            {
                if(skipAreaTag != null && skipAreaTag == area.Tag) continue;

                if(!player.IsValid) return Task.CompletedTask;

                // visitedAreas++;

                foreach(var kvp in visibility)
                {
                    var splitKey = kvp.Key.Split(':');
                    var resRef = splitKey[0];
                    var tag = splitKey.Length > 1 ? splitKey[1] : string.Empty;

                    var mode = kvp.Value ? Anvil.Services.VisibilityMode.Visible : Anvil.Services.VisibilityMode.Hidden;

                    foreach(var obj in area.Objects)
                    {
                        // testedObjects++;
                        if(obj.ResRef == resRef && (tag == string.Empty || obj.Tag == tag))
                        {
                            player.SetPersonalVisibilityOverride(obj,mode);
                            // updatedObjects++;
                        }
                    }
                }
            }

            // sw.Stop();

            // NLog.LogManager.GetCurrentClassLogger().Info(@$"Objects visibility upadted in {sw.Elapsed.TotalSeconds} seconds.
            // Visited areas: {visitedAreas},
            // TestedObjects: {testedObjects},
            // UpdatedObjects: {updatedObjects}");

            return Task.CompletedTask;
        }
    }
}