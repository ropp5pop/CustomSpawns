﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using Helpers;
using TaleWorlds.ObjectSystem;

namespace CustomSpawns.Spawn
{
    public static class Spawner
    {


        public static MobileParty SpawnParty(Settlement spawnedSettlement, Clan clan, PartyTemplateObject templateObject, MobileParty.PartyTypeEnum partyType, TextObject partyName = null, bool IsInheritClan = false)
        {
            //get name and show message.
            TextObject textObject = partyName ?? clan.Name;
            ModDebug.ShowMessage("CustomSpawns: Spawning " + textObject.ToString() + " at " + spawnedSettlement.GatePosition + " in settlement " + spawnedSettlement.Name.ToString(), DebugMessageType.Spawn);

            //create.
            MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(templateObject.StringId + "_" + 1);
            mobileParty.InitializeMobileParty(textObject, ConstructTroopRoster(templateObject), new TroopRoster(), spawnedSettlement.GatePosition, 0);

            //initialize
            Clan settlementClan = spawnedSettlement.OwnerClan;
            if (IsInheritClan == true)
            {
                Spawner.InitParty(mobileParty, textObject, settlementClan, spawnedSettlement);
            }
            else
            {
                Spawner.InitParty(mobileParty, textObject, clan, spawnedSettlement);
            }

            return mobileParty;
        }
        private static void InitParty(MobileParty banditParty, TextObject name, Clan faction, Settlement homeSettlement)
        {
            banditParty.Name = name;
            if (faction.Leader == null)
            {
                banditParty.Party.Owner = faction.Heroes.ToList().Count == 0? null : faction.Heroes.First();
            }
            else
            {
                banditParty.Party.Owner = faction.Leader;
            }
            banditParty.Party.Visuals.SetMapIconAsDirty();
            if (faction.Leader.HomeSettlement == null)
            {
                faction.UpdateHomeSettlement(homeSettlement);
            }
            banditParty.HomeSettlement = homeSettlement;
            TaleWorldsCode.BanditsCampaignBehaviour.CreatePartyTrade(banditParty);
            foreach (ItemObject itemObject in ItemObject.All)
            {
                if (itemObject.IsFood)
                {
                    int num = TaleWorldsCode.BanditsCampaignBehaviour.IsLooterFaction(banditParty.MapFaction) ? 8 : 16;
                    int num2 = MBRandom.RoundRandomized((float)banditParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * (float)num * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
                    if (num2 > 0)
                    {
                        banditParty.ItemRoster.AddToCounts(itemObject, num2, true);
                    }
                }
            }
        }

        private static TroopRoster ConstructTroopRoster(PartyTemplateObject pt, int troopNumberLimit = -1) //TODO implement troop number limit.
        {
            TroopRoster returned = new TroopRoster();
            float gameProcess = MiscHelper.GetGameProcess();
            float num = 0.25f + 0.75f * gameProcess;
            int num2 = MBRandom.RandomInt(2);
            float num3 = (num2 == 0) ? MBRandom.RandomFloat : (MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * 4f);
            float num4 = (num2 == 0) ? (num3 * 0.8f + 0.2f) : (1f + num3);
            float randomFloat = MBRandom.RandomFloat;
            float randomFloat2 = MBRandom.RandomFloat;
            float randomFloat3 = MBRandom.RandomFloat;
            for (int i = 0; i < pt.Stacks.Count; i++)
            {
                float f = (pt.Stacks.Count > 0) ? ((float)pt.Stacks[i].MinValue + num * num4 * randomFloat * (float)(pt.Stacks[i].MaxValue - pt.Stacks[i].MinValue)) : 0f;
                returned.AddToCounts(pt.Stacks[i].Character, MBRandom.RoundRandomized(f), false);
            }
            return returned;
        }

        public static void HandleAIChecks(MobileParty mb, Data.SpawnData data, Settlement spawnedSettlement) //TODO handle sub parties being reconstructed!
        {
            try
            {
                bool invalid = false;
                Dictionary<string, bool> aiRegistrations = new Dictionary<string, bool>();
                if (data.PatrolAroundSpawn)
                {
                    bool success = AI.AIManager.HourlyPatrolAroundSpawn.RegisterParty(mb, spawnedSettlement);
                    aiRegistrations.Add("Patrol around spawn behaviour: ", success);
                    invalid = invalid ? true : !success;
                }
                if (data.AttackClosestIfIdleForADay)
                {
                    bool success = AI.AIManager.AttackClosestIfIdleForADayBehaviour.RegisterParty(mb);
                    aiRegistrations.Add("Attack Closest Settlement If Idle for A Day Behaviour: ", success);
                    invalid = invalid ? true : !success;
                }
                if (data.PatrolAroundClosestLestInterruptedAndSwitch.isValidData)
                {
                    bool success = AI.AIManager.PatrolAroundClosestLestInterruptedAndSwitchBehaviour.RegisterParty(mb, 
                        new AI.PatrolAroundClosestLestInterruptedAndSwitchBehaviour.PatrolAroundClosestLestInterruptedAndSwitchBehaviourData(mb, data.PatrolAroundClosestLestInterruptedAndSwitch));
                    aiRegistrations.Add("Patrol Around Closest Lest Interrupted And Switch Behaviour: ", success);
                    invalid = invalid ? true : !success;
                }
                if (invalid && ConfigLoader.Instance.Config.IsDebugMode)
                {
                    ErrorHandler.ShowPureErrorMessage("Custom Spawns AI XML registration error has occured. The party being registered was: " + mb.StringId +
                        "\n Here is more info about the behaviours being registered: \n" + aiRegistrations.ToString());
                }
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(e);
            }
        }

        public static Settlement GetSpawnSettlement(Data.SpawnData data, Random rand = null)
        {
            if(rand == null)
                rand = new Random();
            Clan spawnClan = data.SpawnClan;
            //deal with override of spawn clan.
            if (data.OverridenSpawnClan.Count != 0)
            {
                spawnClan = data.OverridenSpawnClan[rand.Next(0, data.OverridenSpawnClan.Count)];
            }
            //check for one hideout
            Settlement firstHideout = null;
            if (ConfigLoader.Instance.Config.SpawnAtOneHideout)
            {
                foreach (Settlement s in Settlement.All)
                {
                    if (s.IsHideout())
                    {
                        firstHideout = s;
                        break;
                    }
                }
            }
            //deal with town spawn
            Settlement spawnOverride = null;
            if (data.OverridenSpawnSettlements.Count != 0)
            {
                spawnOverride = CampaignUtils.PickRandomSettlementAmong(data.OverridenSpawnSettlements, data.TrySpawnAtList, rand);
            }
            if (spawnOverride == null && data.OverridenSpawnCultures.Count != 0)
            {
                //spawn at overriden spawn instead!
                spawnOverride = CampaignUtils.PickRandomSettlementOfCulture(data.OverridenSpawnCultures, data.TrySpawnAtList);
            }
            //get settlement
            Settlement spawnSettlement = ConfigLoader.Instance.Config.SpawnAtOneHideout ? firstHideout : (spawnOverride == null ? CampaignUtils.GetPreferableHideout(spawnClan) : spawnOverride);
            return spawnSettlement;
        }

    }
}
