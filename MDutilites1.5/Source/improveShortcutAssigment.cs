using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnShortcutCycle
{
    [StaticConstructorOnStartup]
    public static class GizmoShortcutCycle
    {
        // rimworld design, whole UI and all their calculations done everytick on mainthread. just
        // [HarmonyPatch(typeof(Command_Psycast), "DisabledCheck")]


            //recursion prevention
            private static readonly AccessTools.FieldRef<Command_Ability, bool> disabledField =
                AccessTools.FieldRefAccess<Command_Ability, bool>("disabled");

            public static void Postfix(Command_Psycast __instance)
            {
                __instance.Order = disabledField(__instance) ? 10000f : 0f;
            }
        
        //no longer needed when Ordering gizmos by status. also only works without the Ordering
        //MAIN SWITCH LOGIC
        /*
        private static bool isProcessingCycle = false;
        public static bool Command_Ability_ProcessInput_Prefix(Command_Psycast __instance, Event ev)
        {
            // Prevent infinite recursion
            if (isProcessingCycle) return true;

            //if (__instance.KeyDownEvent)

            // Only care about KeyBindingDef-based commands (like Misc shortcuts)
            if (__instance.hotKey == null) return true;

            KeyBindingDef keyDef = __instance.hotKey;

            // Ignore Misc1 and Misc2
            if (keyDef == KeyBindingDefOf.Misc1 || keyDef == KeyBindingDefOf.Misc2) return true;

            // Only handle Misc3 to Misc12
            if (!keyDef.defName.StartsWith("Misc") ||
                !Enumerable.Range(3, 12).Any(i => keyDef.defName == "Misc" + i))
            {
                return true;
            }

            // Reuse this list: valid selected, alive pawns
            List<Pawn> selectedPawns = Find.Selector.SelectedPawns.Where(p => !p.Dead).ToList();
            if (selectedPawns.Count <= 1) return true;

            Ability ability = __instance.Ability;
            if (ability == null) return true;

            Pawn currentPawn = __instance.Pawn;
            if (currentPawn == null) return true;

            AbilityDef abilityDef = ability.def;

            // Find all selected pawns that have this ability AND can cast it
            List<Pawn> pawnsWithCastableAbility = selectedPawns
                .Where(p =>
                {
                    Ability pawnAbility = p.abilities?.GetAbility(abilityDef);
                    return pawnAbility != null && pawnAbility.CanCast.Accepted;
                })
                .ToList();

            // If fewer than 2 pawns can currently use the ability, don't cycle
            if (pawnsWithCastableAbility.Count <= 1) return true;

            // Only proceed if the key was just pressed
            if (!keyDef.JustPressed) return true;

            // Sort pawns deterministically (by ID for consistency)
            pawnsWithCastableAbility.SortBy(p => p.thingIDNumber);

            int currentIndex = pawnsWithCastableAbility.IndexOf(currentPawn);

            int nextIndex = (currentIndex + 1) % pawnsWithCastableAbility.Count;
            Pawn nextPawn = pawnsWithCastableAbility[nextIndex];
            // Get the ability on the next pawn
            Ability nextAbility = nextPawn.abilities.GetAbility(abilityDef);
            if (nextAbility == null || !nextAbility.CanCast.Accepted) return true;

            // Retrieve gizmos and find matching Command_Ability with same hotkey
            IEnumerable<Gizmo> gizmos = nextAbility.GetGizmos();
            Command_Psycast nextCommand = gizmos.OfType<Command_Psycast>()
                .FirstOrDefault(c => c.hotKey == keyDef);

            if (nextCommand == null) return true;

            // WIP Mark the next command's ability and pawn for visual feedback


            // Execute on next pawn instead
            isProcessingCycle = true;
            try
            {
                nextCommand.ProcessInput(ev);
                return false; // Block original command
            }
            finally
            {
                isProcessingCycle = false;
            }
        }
        */
        


        /* horrible trash proof of conecept, terrible lag, but it does intercept the keypress
        [HarmonyPatch(typeof(UIRoot_Play), "UIRootOnGUI")]
        public static class Patch_UIRoot_Play_UIRootOnGUI
        {
            readonly private static bool isProcessingCycle = false;

            public static void Postfix()
            {
                if (isProcessingCycle) return;

                // Check for hotkey presses
                CheckAndHandleHotkeys();
            }

            private static void CheckAndHandleHotkeys()
            {
                foreach (KeyBindingDef keyDef in DefDatabase<KeyBindingDef>.AllDefs)
                {
                    if (keyDef == null ||
                        keyDef == KeyBindingDefOf.Misc1 ||
                        keyDef == KeyBindingDefOf.Misc2 ||
                        !keyDef.defName.StartsWith("Misc")
                        )
                    {
                        continue;
                    }

                    // Check if this key is pressed
                    bool keyPressed = keyDef.KeyDownEvent ?
                        Input.GetKeyDown(keyDef.MainKey) :
                        keyDef.JustPressed;

                    if (keyPressed)
                    {
                        Log.Message($"detected keypress {keyDef}");
                        // Trigger gizmo reordering for all selected pawns
                        // nothig yet Dosomethingspecial();
                        break;
                    }
                }
            }
        }
        */
        /* wont work, everything will be at 10000 after some time
        [HarmonyPatch(typeof(Command_Ability), "DisableWithReason")]
        public static class CommandAbilityDisableWithReasonPatch
        {
            public static void Postfix(Command_Ability __instance)
            {
                __instance.Order = 10000f;
            }
        }
        */


        /*
        //incorrect method type
        [HarmonyPatch(typeof(Command_Psycast), nameof(Command_Psycast.Disabled), MethodType.Setter)]
        public static class PsycastDisabledPatch
        {
            public static void Postfix(Command_Psycast __instance, bool value)
            {
                if (value) // if being disabled
                    __instance.Order = 10000f;
                else
                    __instance.Order = 0f;
            }
        }
        */

        /*
        //works, simple, lag of hell.
        [HarmonyPatch(typeof(Pawn), "GetGizmos")]
        public static class GizmoOrderPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref IEnumerable<Gizmo> __result)
            {
                var gizmos = __result?.OfType<Command_Psycast>().ToList();
                if (gizmos == null) return;

                foreach (var g in gizmos)
                    g.Order = g.Disabled ? 10000f : 0f;

                __result = gizmos;
            }
        }
        */

        /*
        //TRASH. modifying gizmo OBJECT order, corrupts the original order and breaks completely 
        // 
        private const float DisabledGizmoOffset = 100000f;

        // Push all disabled gizmos far beyond any enabled ones to ensure they appear at the end
        // even when multiple pawns are selected (avoids cross-pawn ordering issues)
        [HarmonyPatch(typeof(Pawn), "GetGizmos")]
        public static class GizmoOrderPatchthree
        {
            [HarmonyPostfix]
            public static void Postfix(ref IEnumerable<Gizmo> __result)
            {
                List<Gizmo> gizmos = __result?.ToList();
                if (gizmos == null || gizmos.Count == 0) return;

                // 1. Find highest Order among ENABLED gizmos
                float maxEnabledOrder = gizmos
                    .Where(g => g != null && !g.Disabled)
                    .Select(g => g.Order)
                    .DefaultIfEmpty(0f)
                    .Max();


                // 2. Assign DISABLED gizmos to appear AFTER all enabled ones
                float disabledOrder = maxEnabledOrder + DisabledGizmoOffset;
                foreach (Gizmo g in gizmos)
                {
                    if (g != null && g.Disabled)
                    {
                        g.Order = disabledOrder;
                        disabledOrder += DisabledGizmoOffset; // Preserve relative order of disabled gizmos
                    }
                }

                // 3. Let the game sort naturally using the updated Order values
                __result = gizmos;
            }
        }
        */

    }
}