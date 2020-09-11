﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;


namespace CustomSpawns.Data
{
    public class SpawnDataManager
    {

        static SpawnDataManager _instance;

        public static SpawnDataManager Instance
        {
            get
            {
                return _instance ?? new SpawnDataManager();
            }
            private set
            {
                _instance = value;
                
            }
        }

        public static void ClearInstance(Main caller)
        {
            if (caller == null)
                return;
            _instance = null;
        }

        private List<SpawnData> data = new List<SpawnData>();
        private Dictionary<string, SpawnData> partyIDtoData = new Dictionary<string, SpawnData>();

        public IList<SpawnData> Data
        {
            get
            {
                return data.AsReadOnly();
            }
        }

        public MBReadOnlyDictionary<string, SpawnData> PartyIDToData
        {
            get
            {
                return partyIDtoData.GetReadOnlyDictionary();
            }
        }

        private SpawnDataManager()
        {
            if (!Main.isAPIMode)
            {
                string path = Path.Combine(BasePath.Name, "Modules", "CustomSpawns", "ModuleData", "Data", "CustomDailySpawn.xml");
                if (!File.Exists(path))
                {
                    path = Path.Combine(BasePath.Name, "Modules", "CustomSpawns", "ModuleData", "Data", "RegularBanditDailySpawn.xml");
                }
                ConstructListFromXML(path);
            }
            foreach (var subMod in ModIntegration.SubModManager.dependentModsArray)
            {
                string path = Path.Combine(subMod.CustomSpawnsDirectoryPath, "CustomDailySpawn.xml");
                if (!File.Exists(path))
                {
                    path = Path.Combine(subMod.CustomSpawnsDirectoryPath, "RegularBanditDailySpawn.xml");
                }
                if (File.Exists(path))
                    ConstructListFromXML(path);
            }
        }

        private void ConstructListFromXML(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            try
            {
                foreach (XmlNode node in doc.DocumentElement)
                {
                    if (node.NodeType == XmlNodeType.Comment)
                        continue;

                    SpawnData dat = new SpawnData();

                    dat.PartyTemplate = (PartyTemplateObject)MBObjectManager.Instance.ReadObjectReferenceFromXml("party_template", typeof(PartyTemplateObject), node);
                    if (node.Attributes["spawn_clan"] == null)
                        dat.SpawnClan = (Clan)MBObjectManager.Instance.ReadObjectReferenceFromXml("bandit_clan", typeof(Clan), node);
                    else
                        dat.SpawnClan = (Clan)MBObjectManager.Instance.ReadObjectReferenceFromXml("spawn_clan", typeof(Clan), node);

                    //have bannerlord read attributes

                    int i = 0;
                    string s = "overriden_spawn_clan";
                    while (true)
                    {
                        string s1 = s + "_" + i.ToString();
                        if (node.Attributes[s1] == null || node.Attributes[s1].InnerText == "")
                        {
                            break;
                        }
                        else
                        {
                            dat.OverridenSpawnClan.Add((Clan)MBObjectManager.Instance.ReadObjectReferenceFromXml(s1, typeof(Clan), node));
                        }
                        i++;
                    }

                    int j = 0;
                    string st = "overriden_spawn_culture";
                    while (true)
                    {
                        string s1 = st + "_" + j.ToString();
                        if (node.Attributes[s1] == null || node.Attributes[s1].InnerText == "")
                        {
                            break;
                        }
                        else
                        {
                            dat.OverridenSpawnCultures.Add(((CultureObject)MBObjectManager.Instance.ReadObjectReferenceFromXml(s1, typeof(CultureObject), node)).GetCultureCode());
                        }
                        j++;
                    }

                    j = 0;
                    st = "overriden_spawn_settlement";
                    while (true)
                    {
                        string s1 = st + "_" + j.ToString();
                        if (node.Attributes[s1] == null || node.Attributes[s1].InnerText == "")
                        {
                            break;
                        }
                        else
                        {
                            dat.OverridenSpawnSettlements.Add(((Settlement)MBObjectManager.Instance.ReadObjectReferenceFromXml(s1, typeof(Settlement), node)));
                        }
                        j++;
                    }

                    //get elements
                    dat.MaximumOnMap = node["MaximumOnMap"] == null? 0 : int.Parse(node["MaximumOnMap"].InnerText);
                    if (dat.MaximumOnMap < 1)
                    {
                        throw new Exception("the node 'MaximumOnMap' cannot be less than 1!");
                    }
                }
                dat.InheritClanFromSettlement = node["GetClanFromSettlement"] == null ? false : bool.Parse(node["GetClanFromSettlement"].InnerText); t);
                dat.PartyType = node["PartyType"] == null ? MobileParty.PartyTypeEnum.Bandit : StringToPartyTypeEnumIfInvalidBandit(node["PartyType"].InnerText);
                dat.ChanceOfSpawn = node["ChanceOfSpawn"] == null ? 1 : float.Parse(node["ChanceOfSpawn"].InnerText);
                dat.Name = node["Name"] == null ? "Unnamed" : node["Name"].InnerText;
                dat.ChanceInverseConstant = node["ChanceInverseConstant"] == null? 0 : float.Parse(node["ChanceInverseConstant"].InnerText);
                    dat.RepeatSpawnRolls = node["RepeatSpawnRolls"] == null? 1 : int.Parse(node["RepeatSpawnRolls"].InnerText);

                    dat.PatrolAroundSpawn = node["PatrolAroundSpawn"] == null ? false : bool.Parse(node["PatrolAroundSpawn"].InnerText);
                    dat.MinimumNumberOfDaysUntilSpawn = node["MinimumNumberOfDaysUntilSpawn"] == null ? -1 : int.Parse(node["MinimumNumberOfDaysUntilSpawn"].InnerText);

                    dat.AttackClosestIfIdleForADay = node["AttackClosestIfIdleForADay"] == null ? true : bool.Parse(node["AttackClosestIfIdleForADay"].InnerText);

                    //try spawn at list creation
                    if (node["TrySpawnAt"] != null && node["TrySpawnAt"].InnerText != "")
                    {
                        dat.TrySpawnAtList = ConstructTrySettlementList(node["TrySpawnAt"].InnerText);
                    }

                    //message
                    string msg = node["SpawnMessage"] == null? "" : node["SpawnMessage"].InnerText;
                    string color = node["SpawnMessageColor"] == null ? "" : node["SpawnMessageColor"].InnerText;

                    if(msg != "")
                    {
                        if(color == "")
                        {
                            dat.spawnMessage = new InformationMessage(msg, Color.Black);
                        }
                        else
                        {
                            Color c = UX.GetMessageColour(color) == "" ? (color[0] == '#'? Color.ConvertStringToColor(color) : Color.Black) : Color.ConvertStringToColor(UX.GetMessageColour(color));
                            dat.spawnMessage = new InformationMessage(msg, c);
                        }
                    }

                    //death message
                    string deathMsg = node["DeathMessage"] == null ? "" : node["DeathMessage"].InnerText;
                    string deathColor = node["DeathMessage"] == null ? "" : node["DeathMessageColor"].InnerText;

                    if (deathMsg != "")
                    {
                        if (deathColor == "")
                        {
                            dat.deathMessage = new InformationMessage(deathMsg, Color.Black);
                        }
                        else
                        {
                            Color c = UX.GetMessageColour(deathColor) == "" ? (deathColor[0] == '#' ? Color.ConvertStringToColor(deathColor) : Color.Black) : Color.ConvertStringToColor(UX.GetMessageColour(deathColor));
                            dat.deathMessage = new InformationMessage(deathMsg, c);
                        }
                    }

                    //handle extra linear speed.
                    float extraSpeed = float.MinValue;
                    if (node["ExtraLinearSpeed"] != null)
                    {
                        if (!float.TryParse(node["ExtraLinearSpeed"].InnerText, out extraSpeed)) { 
                            throw new Exception("ExtraLinearSpeed must be a float value! ");
                        }
                        Main.customSpeedModel.RegisterPartyExtraSpeed(dat.PartyTemplate.StringId, extraSpeed);
                    }

                    //handle base speed override
                    float baseSpeedOverride = float.MinValue;
                    if (node["BaseSpeedOverride"] != null)
                    {
                        if (!float.TryParse(node["BaseSpeedOverride"].InnerText, out baseSpeedOverride))
                        {
                            throw new Exception("BaseSpeedOverride must be a float value! ");
                        }
                        Main.customSpeedModel.RegisterPartyBaseSpeed(dat.PartyTemplate.StringId, baseSpeedOverride);
                    }
                    else
                    {
                        Main.customSpeedModel.RegisterPartyBaseSpeed(dat.PartyTemplate.StringId, float.MinValue);
                    }

                    //patrol around closest lest interrupted and switch 
                    if (node["PatrolAroundClosestLestInterruptedAndSwitch"] != null)
                    {
                        bool val = false;
                        if (!bool.TryParse(node["PatrolAroundClosestLestInterruptedAndSwitch"].InnerText, out val))
                        {
                            break;
                        }
                        if (!val)
                            break;
                        XmlNode innerNode = node["PatrolAroundClosestLestInterruptedAndSwitch"];
                        float minDays = 0;
                        float maxDays = 10;
                        List<SpawnSettlementType> TryPatrolAround = new List<SpawnSettlementType>();
                        try
                        {
                            if (!float.TryParse(innerNode.Attributes["min_stable_days"].InnerText, out minDays))
                                throw new Exception("min_stable_days must be a float value!");
                            if (!float.TryParse(innerNode.Attributes["max_stable_days"].InnerText, out maxDays))
                                throw new Exception("max_stable_days must be a float value!");
                            if (innerNode.Attributes["try_patrol_around"] != null && innerNode.Attributes["try_patrol_around"].InnerText != "")
                            {
                                TryPatrolAround = ConstructTrySettlementList(innerNode.Attributes["try_patrol_around"].InnerText);
                            }
                        }catch
                        {
                            throw new Exception("not all attributes in PatrolAroundClosestLestInterruptedAndSwitch were filled properly!");
                        }
                        dat.PatrolAroundClosestLestInterruptedAndSwitch =
                            new AI.PatrolAroundClosestLestInterruptedAndSwitchBehaviour.PatrolAroundClosestLestInterruptedAndSwitchBehaviourData(null, minDays, maxDays, TryPatrolAround);
                    }

                    //min max party speed modifiers
                    float minSpeed = float.MinValue;
                    if (node["MinimumFinalSpeed"] != null)
                    {
                        if (!float.TryParse(node["MinimumFinalSpeed"].InnerText, out minSpeed))
                        {
                            throw new Exception("MinimumFinalSpeed must be a float value! ");
                        }
                        Main.customSpeedModel.RegisterPartyMinimumSpeed(dat.PartyTemplate.StringId, minSpeed);
                    }

                    float maxSpeed = float.MinValue;
                    if (node["MaximumFinalSpeed"] != null)
                    {
                        if (!float.TryParse(node["MaximumFinalSpeed"].InnerText, out maxSpeed))
                        {
                            throw new Exception("MaximumFinalSpeed must be a float value! ");
                        }
                        Main.customSpeedModel.RegisterPartyMaximumSpeed(dat.PartyTemplate.StringId, maxSpeed);
                    }

                    //Spawn along with
                    int k = 0;
                    string str = "spawn_along_with";
                    while (true)
                    {
                        string s1 = str + "_" + k.ToString();
                        if (node.Attributes[s1] == null || node.Attributes[s1].InnerText == "")
                        {
                            break;
                        }
                        else
                        {
                            PartyTemplateObject pt = (PartyTemplateObject)MBObjectManager.Instance.ReadObjectReferenceFromXml(s1, typeof(PartyTemplateObject), node);
                            dat.SpawnAlongWith.Add(new AccompanyingParty(pt, NameSignifierData.Instance.GetPartyNameFromID(pt.StringId),
                                NameSignifierData.Instance.GetPartyFollowBehaviourFlagFromID(pt.StringId)));
                            Main.customSpeedModel.RegisterPartyExtraSpeed(pt.StringId, NameSignifierData.Instance.GetSpeedModifierFromID(pt.StringId));
                            Main.customSpeedModel.RegisterPartyBaseSpeed(pt.StringId, NameSignifierData.Instance.GetBaseSpeedModifierOverrideFromID(pt.StringId));
                            if (minSpeed != float.MinValue)
                                Main.customSpeedModel.RegisterPartyMinimumSpeed(pt.StringId, minSpeed);
                            if (maxSpeed != float.MinValue)
                                Main.customSpeedModel.RegisterPartyMaximumSpeed(pt.StringId, maxSpeed);
                        }
                        k++;
                    }

                    data.Add(dat);
                    if(!partyIDtoData.ContainsKey(dat.PartyTemplate.StringId)) //TODO add way to alert modder that he should use one party template for one AI 
                        partyIDtoData.Add(dat.PartyTemplate.StringId, dat);
                }
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(e, "Spawn Data Parsing of " + filePath);
            }
        }

        private MobileParty.PartyTypeEnum StringToPartyTypeEnumIfInvalidBandit(string s)
        {
            switch (s)
            {
                case "Default":
                    return MobileParty.PartyTypeEnum.Default;
                case "Bandit":
                    return MobileParty.PartyTypeEnum.Bandit;
                case "Caravan":
                    return MobileParty.PartyTypeEnum.Caravan;
                case "GarrisonParty":
                    return MobileParty.PartyTypeEnum.GarrisonParty;
                case "Lord":
                    return MobileParty.PartyTypeEnum.Lord;
                case "Villager":
                    return MobileParty.PartyTypeEnum.Villager;
                default:
                    return MobileParty.PartyTypeEnum.Bandit;
            }
        }

        public static List<SpawnSettlementType> ConstructTrySettlementList(string input)
        {
            string[] trySpawnAtArray = input.Split('|');
            List<SpawnSettlementType> returned = new List<SpawnSettlementType>();
            foreach (var place in trySpawnAtArray)
            {
                switch (place)
                {
                    case "Village":
                        returned.Add(SpawnSettlementType.Village);
                        break;
                    case "Town":
                        returned.Add(SpawnSettlementType.Town);
                        break;
                    case "Castle":
                        returned.Add(SpawnSettlementType.Castle);
                        break;
                }
            }
            return returned;
        }

    }
    public class SpawnData
    {
        public MobileParty.PartyTypeEnum PartyType { get; set; }
        public Clan SpawnClan { get; set; }
        public List<SpawnSettlementType> TrySpawnAtList = new List<SpawnSettlementType>();
        public List<Clan> OverridenSpawnClan = new List<Clan>();
        public List<Settlement> OverridenSpawnSettlements = new List<Settlement>();
        public List<CultureCode> OverridenSpawnCultures = new List<CultureCode>();
        public List<AccompanyingParty> SpawnAlongWith = new List<AccompanyingParty>();
        public int MaximumOnMap { get; set; }
        private float chanceOfSpawn;
        public int MinimumNumberOfDaysUntilSpawn { get; set; }
        public bool AttackClosestIfIdleForADay { get; set; }
        public AI.PatrolAroundClosestLestInterruptedAndSwitchBehaviour.PatrolAroundClosestLestInterruptedAndSwitchBehaviourData PatrolAroundClosestLestInterruptedAndSwitch { get; set; }
        public float ChanceOfSpawn
        {
            get
            {
                return chanceOfSpawn + ChanceInverseConstant * (float)(MaximumOnMap - numberSpawned) / (float)(MaximumOnMap);
            }
            set
            {
                chanceOfSpawn = value;
            }
        }
        public float ChanceInverseConstant { private get; set; }
        public PartyTemplateObject PartyTemplate { get; set; }
        public string Name { get; set; }
        public int RepeatSpawnRolls { get; set; }
        public InformationMessage spawnMessage { get; set; }
        public InformationMessage deathMessage { get; set; }
        public bool PatrolAroundSpawn { get; set; }
        public bool InheritClanFromSettlement { get; set; }
    private int numberSpawned = 0;

        public void IncrementNumberSpawned()
    {
            numberSpawned++;
        }

        public void DecrementNumberSpawned()
        {
            numberSpawned--;
        }

        public void SetNumberSpawned(int num)
        {
            numberSpawned = num;
        }

        public int GetNumberSpawned()
        {
            return numberSpawned;
        }

        public bool CanSpawn()
        {
            return numberSpawned < MaximumOnMap;
        }



    }

    public struct AccompanyingParty
    {
        public PartyTemplateObject templateObject;
        public string name;
        public bool accompanyMainParty; //TODO implement this!!

        public AccompanyingParty(PartyTemplateObject pt, string n, bool accompanyMainParty)
        {
            templateObject = pt;
            name = n;
            this.accompanyMainParty = accompanyMainParty; 
        }
    }

    public enum SpawnSettlementType
    {
        Village, Castle, Town
    }

}
