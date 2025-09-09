using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MDutility
{
    [StaticConstructorOnStartup]
    public static class autoNoDrugpolicy_kid
    {
        private static void AutoNoDrugsPolicy(Pawn pawn)
        {
            if (!pawn.IsColonistPlayerControlled)
            {
                return;
            }
            if (!IsChild(pawn))
            {
                return;
            }
            if (pawn.drugs == null)
            {
                return;
            }
            DrugPolicy drugPolicy = Current.Game.drugPolicyDatabase.AllPolicies.FirstOrDefault((DrugPolicy p) => p.label == "No drugs");
            if (drugPolicy != null)
            {
                pawn.drugs.CurrentPolicy = drugPolicy;
            }
        }
        private static bool IsChild(Pawn pawn)
        {
            return !pawn.ageTracker.Adult;
        }

        public static class Patch_Pawn_AgeTracker_BirthdayBiological
        {
            public static void Postfix(Pawn __instance)
            {
                if (__instance.ageTracker.AgeBiologicalYears < 18)
                {
                    AutoNoDrugsPolicy(__instance);
                }
            }
        }
        public static class Patch_Pawn_SetFaction
        {
            public static void Postfix(Pawn __instance, Faction newFaction)
            {
                if (newFaction != null && newFaction.IsPlayer)
                {
                    AutoNoDrugsPolicy(__instance);
                }
            }
        }
    }
}
