using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using Verse.Noise;
using static HarmonyLib.AccessTools;

namespace MDutility
{

    //no idea
    /*
	public class VanillaGamePatches
    {
        public static void NoWealth(bool allowDuringInit, WealthWatcher __instance)
        {
            float num = __instance.WealthPawns;
            Map map = Find.CurrentMap;
            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (!QuestUtility.IsQuestLodger(pawn))
                {
                    bool flag = (pawn.ParentHolder is Building_CryptosleepCasket || pawn.ParentHolder is CompBiosculpterPod || pawn.ParentHolder is IThingHolderWithDrawnPawn);
                    float num2 = pawn.MarketValue;
                    float num3 = pawn.MarketValue;
                    if  (pawn.IsSlave)
                    {
                        num2 *= -0.25f;
                        num += num2;
                    }
                    if (flag && !(pawn.IsSlave))
                    {

                        num -= num3;
                    }
                    else if (flag) 
                    {
                        num += num2*2f;
                    }
                }
                
            Traverse.Create(__instance).Field("wealthPawns").SetValue(num);
            }
        }
    }
	*/

    internal static class FieldRefs
    {
        public static readonly FieldRef<WealthWatcher, float> WealthPawns =
            FieldRefAccess<WealthWatcher, float>("wealthPawns");

        public static readonly FieldRef<WealthWatcher, Map> Map =
            FieldRefAccess<WealthWatcher, Map>("map");
    }

    public class VanillaGamePatches
    {
        public static void NoWealth(WealthWatcher __instance)
        {


            float num = FieldRefs.WealthPawns(__instance);
            Map map = FieldRefs.Map(__instance);

            foreach (Pawn pawn in map.mapPawns.PawnsInFaction(Faction.OfPlayer))
            {
                if (!QuestUtility.IsQuestLodger(pawn))
                {
                    float num2 = pawn.MarketValue;
                    float num3 = pawn.MarketValue;
                    if (pawn.IsSlave)
                    {
                        num2 *= -0.25f; //OP af? HAHA
                        num += num2;
                    }
                    if ((pawn.ParentHolder is Building_CryptosleepCasket || pawn.ParentHolder is CompBiosculpterPod) && !(pawn.IsSlave))//discard wealth from any non slave pawn which is inside cryptosleep casket
                    {

                        num -= num3;
                    }
                    else if (pawn.ParentHolder is Building_CryptosleepCasket || pawn.ParentHolder is CompBiosculpterPod) //had to have this since subtracting num -= num3 results in a negative wealth loop, meaning every time slave went inside sleepcasket the wealth would get lower and lower.
                    {
                        num += num2 * 2f; //idk how to do this correctly without hardcoding. sigh
                    }
                }
                Traverse.Create(__instance).Field("wealthPawns").SetValue(num);
            }
        }
    }
}
