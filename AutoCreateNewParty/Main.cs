using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Actions;

namespace AutoCreateNewParty
{
    public class Main : MBSubModuleBase
    {
        private Harmony harmonyKit;
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            try
            {
                this.harmonyKit = new Harmony("AutoCreateNewParty.harmony");
                this.harmonyKit.PatchAll();
                InformationManager.DisplayMessage(new InformationMessage("AutoCreateNewParty loaded"));
            }
            catch (Exception ex)
            {
                FileLog.Log("err:" + ex.ToString());
                FileLog.FlushBuffer();
                InformationManager.DisplayMessage(new InformationMessage("err:" + ex.ToString()));
            }
        }

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            if (!(game.GameType is Campaign))
                return;
            CampaignEvents.DailyTickEvent.AddNonSerializedListener((object)this,
                (Action)(() => DailyTick()));
        }
        public static void DailyTick()
        {
            foreach (Hero hero in Hero.MainHero.Clan.Heroes)
            {
                if (hero.IsActive && !hero.IsChild && hero != Hero.MainHero && hero.CanLeadParty())
                {
                    if (hero.PartyBelongedToAsPrisoner != null)
                        continue;
                    else if (hero.IsReleased)
                        continue;
                    else if (hero.PartyBelongedTo == MobileParty.MainParty)
                        continue;
                    else if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero == hero)
                        continue;
                    else if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero != Hero.MainHero)
                        continue;
                    else if (hero.GovernorOf != null)
                        continue;
                    else if (hero.HeroState == Hero.CharacterStates.Disabled)
                        continue;
                    else if (hero.HeroState == Hero.CharacterStates.Fugitive)
                        continue;
                    MobileParty newParty = Hero.MainHero.Clan.CreateNewMobileParty(hero);
                    newParty.SetPartyObjective(MobileParty.PartyObjective.Aggressive);
                    InformationManager.DisplayMessage(new InformationMessage(hero.Name.ToString() + " lead a party"));
                }
            }
        }
    }
    [HarmonyPatch(typeof(PrisonerReleaseCampaignBehavior), "DailyHeroTick")]
    public class PatchDailyHeroTick
    {
        public static bool Prefix(Hero hero)
        {
            if (!hero.IsPrisoner || hero.PartyBelongedToAsPrisoner == null || hero == Hero.MainHero)
                return false;
            if (hero.Clan == Clan.PlayerClan)
            {
                EndCaptivityAction.ApplyByEscape(hero);
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(DefaultPartyWageModel), "MaxWage", MethodType.Getter)]
    public class PatchMaxWage
    {
        public static void Postfix(ref int __result)
        {
            __result = 10000;
        }
    }
    [HarmonyPatch(typeof(DefaultClanTierModel), "GetPartyLimitForTier")]
    public class PatchGetPartyLimitForTier
    {
        public static void Postfix(ref int __result)
        {
            __result = 10000;
        }
    }
    [HarmonyPatch(typeof(DefaultClanTierModel), "GetCompanionLimitFromTier")]
    public class PatchGetCompanionLimitFromTier
    {
        public static void Postfix(ref int __result)
        {
            __result = 10000;
        }
    }
}
