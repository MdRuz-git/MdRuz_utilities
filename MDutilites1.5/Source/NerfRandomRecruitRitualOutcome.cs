using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;

//fixed it up a bit

namespace MDutility
{
    public class NerfRandomRecruitRitualOutcome : RitualAttachableOutcomeEffectWorker
    {
        public override void Apply(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual, RitualOutcomePossibility outcome, out string extraOutcomeDesc, ref LookTargets letterLookTargets)

            {
            extraOutcomeDesc = null;

            if (Rand.Chance(0.6f))
            {
                // Get custom pawn kind
                PawnKindDef mostBASICpawn = DefDatabase<PawnKindDef>.GetNamed("MdRuz_mostBASICpawn");

                // Get hostile humanlike faction
                Faction faction = Find.FactionManager.AllFactions
                    .Where(f => f.def.humanlikeFaction &&
                                !f.def.hidden &&
                                f.HostileTo(Faction.OfPlayer)) // Ensure hostile faction
                    .RandomElementWithFallback();

                if (faction == null)
                {
                    Log.Error("No valid hostile humanlike factions found.");
                    return;
                }

                // Generate x-X pawns
                int pawnCount = Rand.RangeInclusive(3, 9);
                List<Pawn> pawns = new List<Pawn>();
                for (int i = 0; i < pawnCount; i++)
                {
                    // clear unwaveringly loyal flag
                    PawnGenerationRequest request = new PawnGenerationRequest(mostBASICpawn, faction)
                    {
                        ForceNoIdeo = true,
                        ForceRecruitable = true,
                        ForceAddFreeWarmLayerIfNeeded = true
                    };
                    Pawn pawn = PawnGenerator.GeneratePawn(request);
                    
                    pawns.Add(pawn);
                }



                IntVec3 spawnLoc;
                bool success = CellFinder.TryFindRandomEdgeCellWith(
                    validator: cell =>
                        cell.Walkable(jobRitual.Map) &&
                        !cell.Fogged(jobRitual.Map) &&
                        !cell.Roofed(jobRitual.Map),
                    map: jobRitual.Map,
                    roadChance: CellFinder.EdgeRoadChance_Ignore, // or 0f if don't want road bias
                    out spawnLoc
                );
                if (success)
                {
                    // Use 'result' – it's a walkable, unroofed edge cell
                    foreach (Pawn pawn in pawns)
                    {
                        GenSpawn.Spawn(pawn, spawnLoc, jobRitual.Map);
                    }

                    // Create raid behavior
                    //canKidnap = true, bool canTimeoutOrFlee = true
                    LordJob lordJob = new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, sappers: false, useAvoidGridSmart: false, canSteal: false, breachers: false, canPickUpOpportunisticWeapons: false);
                    LordMaker.MakeNewLord(faction, lordJob, jobRitual.Map, pawns);

                    extraOutcomeDesc = this.def.letterInfoText;
                    if (pawns.Count > 0)
                    {
                        // Create letter parameters
                        string letterLabel = extraOutcomeDesc;
                        string letterText = this.def.letterInfoText + "their non-threatening appearance might provide useful in our efforts of increasing our ranks.";
                        LookTargets raidTargets = new LookTargets(pawns);

                        // Create and send the letter
                        ChoiceLetter letter = LetterMaker.MakeLetter(
                            letterLabel,
                            letterText,
                            LetterDefOf.ThreatBig,  // Use appropriate letter type
                            raidTargets
                        );
                        Find.LetterStack.ReceiveLetter(letter);
                    }
                    return;
                }

                
            }
        }
    }
}