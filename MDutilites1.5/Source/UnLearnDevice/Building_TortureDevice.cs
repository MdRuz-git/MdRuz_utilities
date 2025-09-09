using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

/*
 * works. might already be overkill for this simple feature
 * allows selected pawns to lose experience in selected skills
 * 
 * incomplete, but when is it ever?
 * the explicitPawnIds still not fully implemented
 * will probably remove explicitPawnIds 
 * 
*/
namespace UnlearnDevice
{
    public enum AllowedMode
    {
        Whitelist,
        Blacklist
    }

    public class Building_UnlearningDevice : Building
    {
        public AllowedMode allowedMode = AllowedMode.Whitelist;

        // One shared list used as Whitelist or Blacklist depending on mode
        public List<Pawn> listedPawns = new List<Pawn>();

        // Selected skills to drain
        public List<SkillDef> selectedSkills = new List<SkillDef>();

        // Track which pawns were explicitly set by the user (by thingIDNumber)
        private HashSet<int> explicitPawnIds = new HashSet<int>();
        private List<int> explicitPawnIds_Scribe; // Scribe helper

        // Clipboard for copy/paste
        private static AllowedMode? clipboardMode;
        private static List<string> clipboardPawnIds;           // listed pawn IDs (ThingID)
        private static List<string> clipboardExplicitPawnIds;   // explicit pawn IDs (ThingID)
        private static List<string> clipboardSkillDefNames;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref allowedMode, "allowedMode", AllowedMode.Blacklist);
            Scribe_Collections.Look(ref listedPawns, "listedPawns", LookMode.Reference);
            Scribe_Collections.Look(ref selectedSkills, "selectedSkills", LookMode.Def);
            Scribe_Collections.Look(ref explicitPawnIds_Scribe, "explicitPawnIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Cleanup nulls/dupes
                listedPawns = listedPawns?.Where(p => p != null).Distinct().ToList() ?? new List<Pawn>();
                selectedSkills = selectedSkills?.Where(s => s != null).Distinct().ToList() ?? new List<SkillDef>();

                explicitPawnIds = new HashSet<int>(explicitPawnIds_Scribe ?? Enumerable.Empty<int>());

                // Back-compat: if we have a list but no explicit flags, assume current list is explicit
                if (explicitPawnIds.Count == 0 && listedPawns.Count > 0)
                {
                    foreach (var p in listedPawns)
                        explicitPawnIds.Add(p.thingIDNumber);
                }
            }
        }

        // Mark a pawn as explicitly set by the user (use this from UI code)
        public void SetPawnListedByUser(Pawn p, bool listed)
        {
            if (p == null) return;

            if (listed)
            {
                if (!listedPawns.Contains(p))
                    listedPawns.Add(p);
                explicitPawnIds.Add(p.thingIDNumber);
            }
            else
            {
                listedPawns.Remove(p);
                explicitPawnIds.Add(p.thingIDNumber);
            }
        }

        // Option C: When toggles change, clear only implicit entries (keep explicit choices)
        private void ClearImplicitListedPawns()
        {
            if (listedPawns == null) return;
            listedPawns.RemoveAll(p => p == null || !explicitPawnIds.Contains(p.thingIDNumber));
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;

            // Toggle whitelist/blacklist mode
            yield return new Command_Action
            {
                defaultLabel = $"Mode: {allowedMode}",
                defaultDesc = "whitelist = any pawn (enabled) will use this. blacklist = all pawns EXCEPT (enabled) will use this.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower"),
                action = () =>
                {
                    allowedMode = allowedMode == AllowedMode.Blacklist ? AllowedMode.Whitelist : AllowedMode.Blacklist;
                    ClearImplicitListedPawns(); // Option C reconciliation
                }
            };

            // Configure dialog
            yield return new Command_Action
            {
                defaultLabel = "Configure pawns & skills",
                defaultDesc = "Open configuration for which pawns and skills this device affects.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Study", true),
                action = () => Find.WindowStack.Add(new Dialog_UnlearningConfig(this))
            };

            // Copy settings
            yield return new Command_Action
            {
                defaultLabel = "Copy settings",
                defaultDesc = "Copy pawn list, mode, default behavior, and selected skills.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings", true),
                action = () =>
                {
                    clipboardMode = allowedMode;

                    clipboardPawnIds = listedPawns?.Select(p => p.ThingID).ToList() ?? new List<string>();
                    clipboardExplicitPawnIds = listedPawns?
                        .Where(p => p != null && explicitPawnIds.Contains(p.thingIDNumber))
                        .Select(p => p.ThingID).ToList() ?? new List<string>();

                    clipboardSkillDefNames = selectedSkills?.Select(s => s.defName).ToList() ?? new List<string>();
                    Messages.Message("Unlearning device settings copied.", MessageTypeDefOf.TaskCompletion, false);
                }
            };

            // Paste settings
            yield return new Command_Action
            {
                defaultLabel = "Paste settings",
                defaultDesc = "Paste pawn list, mode, default behavior, and selected skills.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PasteSettings", true),
                action = () =>
                {
                    if (clipboardMode == null)
                    {
                        Messages.Message("Nothing copied.", MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    allowedMode = clipboardMode.Value;

                    // Resolve pawns on this map by ThingID (best-effort)
                    listedPawns = new List<Pawn>();
                    explicitPawnIds.Clear();

                    if (clipboardPawnIds != null && Map != null)
                    {
                        foreach (var p in Map.mapPawns.FreeColonists)
                        {
                            if (clipboardPawnIds.Contains(p.ThingID))
                            {
                                listedPawns.Add(p);
                                if (clipboardExplicitPawnIds != null && clipboardExplicitPawnIds.Contains(p.ThingID))
                                    explicitPawnIds.Add(p.thingIDNumber);
                            }
                        }
                    }

                    selectedSkills = new List<SkillDef>();
                    if (clipboardSkillDefNames != null)
                    {
                        foreach (var defName in clipboardSkillDefNames)
                        {
                            var sd = DefDatabase<SkillDef>.GetNamedSilentFail(defName);
                            if (sd != null)
                                selectedSkills.Add(sd);
                        }
                    }

                    Messages.Message("Unlearning device settings pasted.", MessageTypeDefOf.TaskCompletion, false);
                }
            };
        }

        public bool PawnAllowed(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.skills == null || pawn.Faction?.IsPlayer != true || !pawn.RaceProps.Humanlike)
                return false;

            bool listed = listedPawns?.Contains(pawn) == true;

            if (allowedMode == AllowedMode.Whitelist)
                return listed;

            // Blacklist
            return !listed;
        }

        public bool HasUsableSkillsFor(Pawn pawn)
        {
            if (selectedSkills == null || selectedSkills.Count == 0) return false;
            foreach (var sd in selectedSkills)
            {
                var rec = pawn.skills?.GetSkill(sd);
                if (rec == null) continue;
                if (rec.Level > 0 || rec.xpSinceLastLevel > 0f)
                    return true;
            }
            return false;
        }

        // Absorb the cosmetic melee animation damage from the worker, without tracking currentUser.
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            var instigatorPawn = dinfo.Instigator as Pawn;
            if (instigatorPawn != null)
            {
                var job = instigatorPawn.CurJob;
                if (job != null && job.def == TUDefOf.TU_UseUnlearningDevice && job.targetA.Thing == this)
                {
                    absorbed = true;
                    return;
                }
            }
            base.PreApplyDamage(ref dinfo, out absorbed);
        }

        public override string GetInspectString()
        {
            var baseStr = base.GetInspectString();
            var modeStr = $"Mode: {allowedMode}";
            var skillsStr = $"Skills: {(selectedSkills.Count == 0 ? "None" : string.Join(", ", selectedSkills.Select(s => s.skillLabel)))}";

            // Compute current user on demand (no per-tick work)
            string userStr = "Idle";
            var active = FindActiveUser();
            if (active != null)
                userStr = $"In use by: {active.LabelShortCap}";

            return string.Join("\n", new[] { baseStr, modeStr, skillsStr, userStr }.Where(s => !string.IsNullOrEmpty(s)));
        }

        private Pawn FindActiveUser()
        {
            if (Map == null) return null;
            // Only spawned colonists; cheap check and avoids despawned stale refs
            var list = Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                var job = p.CurJob;
                if (job != null && job.def == TUDefOf.TU_UseUnlearningDevice && job.targetA.Thing == this)
                    return p;
            }
            return null;
        }
    }
}