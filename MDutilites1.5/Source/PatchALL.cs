using HarmonyLib;
using PawnShortcutCycle;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using static MDutility.BillManager;

namespace MDutility
{

    [StaticConstructorOnStartup]
    public class MDutility
    {
        private const string MY_MOD_ID = "MdRuz.utilities";

        public static Harmony Harm;
        static MDutility()
        {
            Harm = new Harmony(MY_MOD_ID);
            //Harmony.DEBUG = true;
            //Harm.PatchAll(); 

           //had to separate all patches just for this one, since tweaking cellfinder just seems not safe
           //so at least I'm not gonna touch it if any other mod tries to do the same
           //CellFinderLoose_TryFindRandomNotEdgeCellWith.cs
           MethodBase originalmethod = AccessTools.Method(typeof(CellFinderLoose), nameof(CellFinderLoose.TryFindRandomNotEdgeCellWith));
           var patchInfo = Harmony.GetPatchInfo(originalmethod);
           if (patchInfo != null && patchInfo.Owners.Count > 0)
            {
                var owners = string.Join(", ", patchInfo.Owners);
                Log.Message($"[MDutility] Method already patched by: {owners}. Skipping. (deepscanner ore finder patch) {originalmethod}");

            }
            else //no other patch detected, applying my own
           {
                Harm.Patch(originalmethod,
                                prefix: new HarmonyMethod(AccessTools.Method(typeof(CellFinderLoose_TryFindRandomNotEdgeCellWith_Patch), nameof(CellFinderLoose_TryFindRandomNotEdgeCellWith_Patch.Prefix)))
                            );
                Log.Message($"[MDutility] TryFindRandomNotEdgeCellWith patched successfully. (deepscanner ore finder patch)");
            }

            
           //WealthWatcher_ForceRecount.cs
           Harm.Patch(
           original: typeof(WealthWatcher).Method("ForceRecount"),
           postfix: new HarmonyMethod(typeof(VanillaGamePatches), nameof(VanillaGamePatches.NoWealth))
           );

            //RelationHigherLimit.cs
            Harm.Patch(
            original: AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf)),
            transpiler: new HarmonyMethod(typeof(RelationHigherLimit), nameof(RelationHigherLimit.Transpiler))
            );
            
            //autoZONEswitch.cs
            Harm.Patch(
            original: typeof(MapComponentUtility).Method("MapComponentUpdate"),
            postfix: new HarmonyMethod(typeof(MapComponentUtility_MapComponentUpdate_Patch), nameof(MapComponentUtility_MapComponentUpdate_Patch.Postfix))
             );
            Harm.Patch(
            original: typeof(Dialog_ManageAreas).Method("DoAreaRow"),
            postfix: new HarmonyMethod(typeof(Dialog_ManageAreas_DoAreaRow_Patch), nameof(Dialog_ManageAreas_DoAreaRow_Patch.Postfix))
             );

            //autoRestoreBills.cs
            /* this was wrong and unnecessary
            Harm.Patch(
                original: AccessTools.Method(typeof(Map), nameof(Map.FinalizeInit)),
                postfix: new HarmonyMethod(typeof(Map_FinalizeInit_Patch), nameof(Map_FinalizeInit_Patch.Postfix))
            );
            */
            Harm.Patch(
                original: AccessTools.Method(typeof(Building), nameof(Building.SpawnSetup)),
                postfix: new HarmonyMethod(typeof(Building_SpawnSetup_Patch), nameof(Building_SpawnSetup_Patch.Postfix))
            );
            Harm.Patch(
                original: AccessTools.Method(typeof(Building), nameof(Building.Destroy)),
                prefix: new HarmonyMethod(typeof(Building_Destroy_Patch), nameof(Building_Destroy_Patch.Prefix))
            );
            
            //PathGrid_CalculatedCostAt.cs
            Harm.Patch(
                original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt)),
                postfix: new HarmonyMethod(typeof(PathGrid_CalculatedCostAt), nameof(PathGrid_CalculatedCostAt.Postfix))
            );

            //improveShortcutAssigment.cs
            /*
            Harm.Patch(
                AccessTools.Method(typeof(Command_Ability), "ProcessInput"),
                prefix: new HarmonyMethod(typeof(GizmoShortcutCycle), nameof(GizmoShortcutCycle.Command_Ability_ProcessInput_Prefix))
            );
            */
            Harm.Patch(
                AccessTools.Method(typeof(Command_Psycast), "DisabledCheck"),
                prefix: new HarmonyMethod(typeof(GizmoShortcutCycle), nameof(GizmoShortcutCycle.Postfix))
            );

            /*
            Harm.Patch(
            original: AccessTools.Method(typeof(SnowUtility), nameof(SnowUtility.MovementTicksAddOn)),
            transpiler: new HarmonyMethod(typeof(PathGrid_CalculatedCostAt), nameof(PathGrid_CalculatedCostAt.MovementTicksAddOnIgnoreZero))
            );
            */
            /*
            //improveShortcutAssigment.cs
            Harm.Patch(
                AccessTools.Method(typeof(Command_Ability), "ProcessInput"),
                prefix: new HarmonyMethod(typeof(PawnShortcutCycleMod), nameof(PawnShortcutCycleMod.Command_Ability_ProcessInput_Prefix))
            );

            
            //autoDrugpolicy_kid
            Harm.Patch(
                original: typeof(Pawn_AgeTracker).Method("BirthdayBiological"),
                postfix: new HarmonyMethod(typeof(Patch_Pawn_AgeTracker_BirthdayBiological), nameof(Patch_Pawn_AgeTracker_BirthdayBiological.Postfix))
            );
            Harm.Patch(
                original: AccessTools.Method(typeof(Pawn), nameof(Pawn.SetFaction)),
                postfix: new HarmonyMethod(typeof(Patch_Pawn_SetFaction), nameof(Patch_Pawn_SetFaction.Postfix))
            );
            */

            // Patch all methods with [HarmonyPatch] in this assembly
            //can no longer do that since im trying to check if other mod patched something already
            //Harm.PatchAll(Assembly.GetExecutingAssembly());

            //old
            //VanillaPatches.Patches();
            //var gameComponentType = typeof(MedicineSwitcherComponent);
            Log.Message($"[MDutility]: Harmony patches applied");
            Log.Message($"[MDutility]: github.com/MdRuz-git/MdRuz_utilities");
        }
    }

    /* OLD, included in MDforcecollision
    [HarmonyPatch(typeof(PawnUtility))]
    [HarmonyPatch("ShouldCollideWithPawns")]
    class PawnUtility_ShouldCollideWithPawns
    {
        public static void Postfix(ref Pawn p, ref bool __result)
        {
            if (p.IsColonistPlayerControlled)   //should be more performant i think. if (p.Faction == Faction.OfPlayer)
            {
                __result = false;
                return;
            }
            if (p.HostileTo(Faction.OfPlayer) && !p.Downed)
            {
                __result = true;
                return;
            }
            if (p.RaceProps.Animal)
            {
                __result = false;
                return;
            }
            return;
        }
    }
    */
}
