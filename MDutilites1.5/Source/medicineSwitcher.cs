using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

/*
if any non prisoner colonist has an infection
switch that pawns default medical care to industrial
after immunity is developed
switch back to previous setting or doctor care no medicine
*/

namespace MDutility
{
    [StaticConstructorOnStartup]

    public static class MedicineSwitcher
    {
        private static HashSet<Pawn> trackedPawns = new HashSet<Pawn>();
        private static Dictionary<Pawn, MedicalCareCategory> originalMedicines = new Dictionary<Pawn, MedicalCareCategory>();
        private static WorkTypeDef patientWorkTypeDef = null;

        static MedicineSwitcher()
        {
            patientWorkTypeDef = DefDatabase<WorkTypeDef>.GetNamed("PatientBedRest", false);
            var harmony = new Harmony("MdRuz.medicineswitcher");

            // Hook into AddHediff with exact signature
            var addHediffMethod = AccessTools.Method(typeof(Pawn_HealthTracker), "AddHediff",
                new System.Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) });

            if (addHediffMethod != null)
            {
                harmony.Patch(
                    original: addHediffMethod,
                    postfix: new HarmonyMethod(typeof(MedicineSwitcher), nameof(OnHediffAdded))
                );
            }
            else
            {
                Log.Error("MDutility: Could not find Pawn_HealthTracker.AddHediff method");
            }
        }

        public static void OnHediffAdded(Pawn_HealthTracker __instance, Hediff hediff, Pawn ___pawn)
        {
            var pawn = ___pawn; // Access private pawn field

            // First check: colonist and not prisoner
            if (!pawn.IsColonist || pawn.IsPrisoner || hediff.def == null)
                return;

            // Check if disease can develop immunity
            if (CanDevelopImmunity(hediff.def))
            {
                // Save original medicine setting
                if (!originalMedicines.ContainsKey(pawn) && pawn.playerSettings != null)
                {
                    originalMedicines[pawn] = pawn.playerSettings.medCare;
                }

                // Switch to best medicine
                if (pawn.playerSettings != null)
                {
                    pawn.playerSettings.medCare = MedicalCareCategory.NormalOrWorse;
                }
                if (patientWorkTypeDef != null)
                {
                    pawn.workSettings.SetPriority(patientWorkTypeDef, 1);
                }
                trackedPawns.Add(pawn);
            }
        }

        private static bool CanDevelopImmunity(HediffDef hediffDef)
        {
            return hediffDef.comps != null &&
                   hediffDef.comps.Any(comp => comp is HediffCompProperties_Immunizable);
        }

        // This will be called by RimWorld's ticker system
        public static void GameComponentTick()
        {
            // Check every 1800 ticks (30 seconds)
            if (Find.TickManager.TicksGame % 1800 != 0)
                return;

            foreach (var pawn in trackedPawns.ToList())
            {
                if (pawn == null || pawn.Destroyed || !pawn.Spawned)
                {
                    trackedPawns.Remove(pawn);
                    if (originalMedicines.ContainsKey(pawn))
                        originalMedicines.Remove(pawn);
                    continue;
                }

                // Check if pawn is immune to all tracked diseases
                if (IsImmuneToAllDiseases(pawn))
                {
                    // Return to original medicine setting or no meds
                    if (pawn.playerSettings != null)
                    {
                        if (originalMedicines.ContainsKey(pawn))
                        {
                            pawn.playerSettings.medCare = originalMedicines[pawn];
                            originalMedicines.Remove(pawn);
                        }
                        else
                        {
                            pawn.playerSettings.medCare = MedicalCareCategory.NoMeds;
                        }
                    }
                    if (patientWorkTypeDef != null)
                    {
                        pawn.workSettings.SetPriority(patientWorkTypeDef, 0);
                    }
                    trackedPawns.Remove(pawn);
                }
            }
        }

        private static bool IsImmuneToAllDiseases(Pawn pawn)
        {
            if (pawn.health?.hediffSet?.hediffs == null)
                return true;

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (CanDevelopImmunity(hediff.def))
                {
                    // Check if the pawn has developed immunity (even if hediff still exists)
                    var immunityComp = hediff.TryGetComp<HediffComp_Immunizable>();
                    if (immunityComp != null && immunityComp.Immunity >= 0.95f)
                    {
                        // Pawn is immune to this disease - continue checking others
                        continue;
                    }
                    else
                    {
                        // Pawn is not yet immune to this disease
                        return false;
                    }
                }
            }

            // All diseases either don't exist or pawn is immune to them
            return true;
        }
    }

    // Game component to handle the tick method
    public class MedicineSwitcherComponent : GameComponent
    {
        public MedicineSwitcherComponent(Game game) : base() { }

        public override void GameComponentTick()
        {
            MedicineSwitcher.GameComponentTick();
        }
    }
}