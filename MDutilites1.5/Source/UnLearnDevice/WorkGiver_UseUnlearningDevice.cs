using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace UnlearnDevice
{
    public class WorkGiver_UseUnlearningDevice : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn.Map == null) yield break;
            var list = pawn.Map.listerThings.ThingsOfDef(TUDefOf.TU_UnlearningDevice);
            if (list == null) yield break;
            foreach (var t in list) yield return t;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn.Map == null) return true;

            var devices = pawn.Map.listerThings.ThingsOfDef(TUDefOf.TU_UnlearningDevice)
                .OfType<Building_UnlearningDevice>();

            return !devices.Any(device => device.PawnAllowed(pawn) && device.HasUsableSkillsFor(pawn));
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t == null || t.DestroyedOrNull()) return false;

            var b = t as Building_UnlearningDevice;
            if (b == null) return false;
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;
            if (!pawn.CanReach(t, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn)) return false;

            // Only humanlike colonists with mood and skills
            if (!pawn.RaceProps.Humanlike || pawn.Faction?.IsPlayer != true) return false;

            // Whitelist/blacklist check
            if (!b.PawnAllowed(pawn)) return false;

            // Skills must be selected and relevant to this pawn
            if (!b.HasUsableSkillsFor(pawn)) return false;

            // Fail if interaction cell blocked
            var cell = t.InteractionCell;
            if (!cell.Standable(pawn.Map) || cell.IsForbidden(pawn)) return false;

            if (t.IsBurning()) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(TUDefOf.TU_UseUnlearningDevice, t);
        }
    }
}