using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace UnlearnDevice
{
    public class JobDriver_UseUnlearningDevice : JobDriver
    {
        private const int SessionDurationTicks = 2400;      // ~1 in-game hour
        private const int XpTickInterval = 300;             // apply XP loss every ~5 sec
        private const int MeleeAnimInterval = 120;          // swing animation cadence
        private const float XpLossPerEvent = 175f;         // per event, passion ignored

        private Building_UnlearningDevice Device => TargetThingA as Building_UnlearningDevice;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 80/20 chance to proceed; on refusal, apply moodlet and end
            var decide = new Toil
            {
                initAction = () =>
                {
                    if (Rand.Chance(0.20f))
                    {
                        GiveReluctantThought(pawn);
                        EndJobWith(JobCondition.Incompletable);
                    }
                }
            };
            yield return decide;

            var start = new Toil
            {
                initAction = () =>
                {
                   // Device?.Notify_StartedUsing(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return start;

            var work = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = SessionDurationTicks
            };

            work.WithProgressBar(TargetIndex.A, () => (float)work.actor.jobs.curDriver.ticksLeftThisToil / SessionDurationTicks);

            work.AddPreTickAction(() =>
            {
                if (Device == null || Device.Destroyed)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Face the device
                pawn.rotationTracker.FaceCell(Device.Position);

                // Melee swing animation without damage (absorbed by device override)
                if (Find.TickManager.TicksGame % MeleeAnimInterval == 0)
                {
                    pawn.meleeVerbs?.TryMeleeAttack(Device);
                }

                // Apply XP loss
                if (Find.TickManager.TicksGame % XpTickInterval == 0)
                {
                    ApplyXpLoss(pawn, Device);
                }
            });
            yield return work;

            var finish = new Toil
            {
                initAction = () =>
                {
                   // Device?.Notify_StoppedUsing(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finish;
        }

        private void ApplyXpLoss(Pawn p, Building_UnlearningDevice dev)
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
            target.Learn(-XpLossPerEvent, direct: true, true);
        }

        private void GiveReluctantThought(Pawn p)
        {
            if (p?.needs?.mood == null) return;

            var memories = p.needs.mood.thoughts.memories;

            // Get existing thought
            var existingMemory = memories.GetFirstMemoryOfDef(TUDefOf.TU_ForcedAgainstMyWill);
            int currentDegree = existingMemory?.CurStageIndex ?? -1;

            // Remove existing thought if it exists
            if (existingMemory != null)
            {
                memories.RemoveMemoriesOfDef(TUDefOf.TU_ForcedAgainstMyWill);
            }

            // Apply new thought with next degree (stage)
            int newDegree = Mathf.Min(currentDegree + 1, 4); // 0-4 for 5 stages
            if (newDegree < 0) newDegree = 0; // Handle case where there was no existing thought

            Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(TUDefOf.TU_ForcedAgainstMyWill, newDegree);
            memories.TryGainMemory(thought);
        }
    }
}