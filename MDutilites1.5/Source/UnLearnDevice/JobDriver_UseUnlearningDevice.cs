using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;

namespace UnlearnDevice
{
    public class JobDriver_UseUnlearningDevice : JobDriver
    {
        private const int SessionDurationTicks = 2400; // ~1 in-game hour
        private const int TotalXpLoss = 1400;          // = 2400 / 300 * 175 → 8 events * 175 = 1400 XP total
        private Building_UnlearningDevice cachedDevice;
        private int elapsedTicks = 0;
        private const int feedbackInterval = 300; // Every 5 seconds

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);

            // Path to device
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 20% chance to refuse
            var decide = new Toil
            {
                initAction = () =>
                {
                    if (Rand.Chance(0.20f))
                    {
                        MoteMaker.MakeAttachedOverlay(
                            pawn,                  // the pawn to attach to
                            ThingDefOf.Mote_ThoughtBad,  // or any ThingDef of type Mote
                            Vector3.zero,          // offset from pawn
                            scale: 1f              // scale multiplier
                        );
                        GiveReluctantThought(pawn);
                        EndJobWith(JobCondition.Incompletable);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return decide;

            // Start
            var start = new Toil
            {
                initAction = () =>
                {
                    cachedDevice = TargetThingA as Building_UnlearningDevice;

                    pawn.rotationTracker.FaceCell(cachedDevice.Position);

                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return start;

            // Main work phase: Just wait + show progress bar + occasional visual feedback
            var work = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = SessionDurationTicks
            };

            work.WithProgressBar(TargetIndex.A, () => (float)work.actor.jobs.curDriver.ticksLeftThisToil / SessionDurationTicks);        

            work.tickAction = () =>
            {
                elapsedTicks++;

                // Every 5 seconds: 
                if (elapsedTicks >= feedbackInterval)
                {
                    elapsedTicks = 0; // reset
                    /*
                    Effecter eff = EffecterDefOf.ProgressBar.Spawn();
                    eff.Trigger(pawn, cachedDevice);
                    eff.Cleanup();
                    */
                    // MeleeHit_Unarmed
                    SoundStarter.PlayOneShot(SoundDefOf.MeleeHit_Unarmed, new TargetInfo(pawn.Position, pawn.Map));
                }
            };

            yield return work;

            // Apply XP loss ONCE at the end
            var applyXp = new Toil
            {
                initAction = () =>
                {
                    
                   ApplyXpLoss(pawn, cachedDevice, TotalXpLoss);
                    
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return applyXp;
        }

        private void ApplyXpLoss(Pawn p, Building_UnlearningDevice dev, int totalXpLoss)
        {
            if (p.skills == null || dev?.selectedSkills == null || dev.selectedSkills.Count == 0) return;

            var candidates = new List<SkillRecord>();
            foreach (var sd in dev.selectedSkills)
            {
                var rec = p.skills.GetSkill(sd);
                if (rec == null) continue;
                if (rec.Level > 0 || rec.xpSinceLastLevel > 0f)
                    candidates.Add(rec);
            }

            if (candidates.Count == 0) return;

            var target = candidates.RandomElement();

            if (target.Level > 0 || target.XpTotalEarned >= totalXpLoss)
            {
                // Use Learn if we can safely deduct full amount
                target.Learn(-totalXpLoss, direct: true, true);
            }
            else // Not enough XP — zero it out manually
            {
                target.xpSinceLastLevel = 0f;
            }

           // target.Learn(-totalXpLoss, direct: true, true);
        }

        private void GiveReluctantThought(Pawn p)
        {
            if (p?.needs?.mood == null) return;

            var memories = p.needs.mood.thoughts.memories;
            var existingMemory = memories.GetFirstMemoryOfDef(TUDefOf.TU_ForcedAgainstMyWill);
            int currentDegree = existingMemory?.CurStageIndex ?? -1;

            if (existingMemory != null)
            {
                memories.RemoveMemoriesOfDef(TUDefOf.TU_ForcedAgainstMyWill);
            }

            int newDegree = Mathf.Min(currentDegree + 1, 4);
            if (newDegree < 0) newDegree = 0;

            Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(TUDefOf.TU_ForcedAgainstMyWill, newDegree);
            memories.TryGainMemory(thought);
        }
    }
}