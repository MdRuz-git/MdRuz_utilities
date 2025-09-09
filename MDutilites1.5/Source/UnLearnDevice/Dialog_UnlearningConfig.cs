using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;


/*
 * might still be buggy
 * might remove Pre-computed pawnLabels
 * 
 */
namespace UnlearnDevice
{
    public class Dialog_UnlearningConfig : Window
    {
        private readonly Building_UnlearningDevice device;
        private Vector2 scrollSkills;
        private Vector2 scrollPawns;
        private List<SkillDef> allSkills;
        private List<Pawn> allPawns;
        private List<string> pawnLabels; // Pre-computed labels
        private string pawnSearchText = ""; // Search filter

        public override Vector2 InitialSize => new Vector2(760f, 600f);

        public Dialog_UnlearningConfig(Building_UnlearningDevice device)
        {
            this.device = device;

            forcePause = false;
            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;

            allSkills = DefDatabase<SkillDef>.AllDefsListForReading.OrderBy(s => s.skillLabel).ToList();

            if (device?.Map != null)
            {
                var bar = Find.ColonistBar;
                if (bar != null)
                {
                    allPawns = bar.GetColonistsInOrder()
                        .Where(p => p != null && p.Map == device.Map)
                        .ToList();
                }
                else
                {
                    allPawns = device.Map.mapPawns.FreeColonists.ToList();
                }

                // Pre-compute pawn labels when dialog opens
                PrecomputePawnLabels();
            }
            else
            {
                allPawns = new List<Pawn>();
                pawnLabels = new List<string>();
            }
        }

        private void PrecomputePawnLabels()
        {
            pawnLabels = new List<string>();
            foreach (var pawn in allPawns)
            {
                string label = pawn.LabelShortCap ?? "Unknown";
                if (pawn.IsSlaveOfColony)
                {
                    label += " (Slave)";
                }
                pawnLabels.Add(label);
            }
        }


        public override void DoWindowContents(Rect inRect)
        {
            var headerHeight = 30f;
            var gap = 10f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width / 2f - gap, headerHeight), "Skills");
            Widgets.Label(new Rect(inRect.x + inRect.width / 2f + gap, inRect.y, inRect.width / 2f - gap, headerHeight), "Pawns");
            Text.Font = GameFont.Small;

            var contentRect = new Rect(inRect.x, inRect.y + headerHeight + 4f, inRect.width, inRect.height - headerHeight - 44f);
            var leftRect = new Rect(contentRect.x, contentRect.y, contentRect.width / 2f - gap, contentRect.height);
            var rightRect = new Rect(contentRect.x + contentRect.width / 2f + gap, contentRect.y, contentRect.width / 2f - gap, contentRect.height);

            Widgets.DrawMenuSection(leftRect);
            Widgets.DrawMenuSection(rightRect);

            // Skills panel (unchanged)
            var viewHeightSkills = (allSkills.Count * 26f) + 40f;
            var viewRectL = new Rect(0f, 0f, leftRect.width - 16f, viewHeightSkills);
            Widgets.BeginScrollView(leftRect, ref scrollSkills, viewRectL);
            var cur = new Vector2(0f, 0f);

            if (Widgets.ButtonText(new Rect(cur.x, cur.y, 100f, 24f), "Select all"))
            {
                device.selectedSkills = allSkills.ToList();
            }
            if (Widgets.ButtonText(new Rect(cur.x + 105f, cur.y, 100f, 24f), "Clear all"))
            {
                device.selectedSkills.Clear();
            }
            cur.y += 30f;

            foreach (var sd in allSkills)
            {
                bool on = device.selectedSkills.Contains(sd);
                bool before = on;
                Widgets.CheckboxLabeled(new Rect(cur.x, cur.y, viewRectL.width, 22f), sd.skillLabel.CapitalizeFirst(), ref on);
                if (on != before)
                {
                    if (on)
                    {
                        if (!device.selectedSkills.Contains(sd))
                            device.selectedSkills.Add(sd);
                    }
                    else
                    {
                        device.selectedSkills.Remove(sd);
                    }
                }
                cur.y += 24f;
            }
            Widgets.EndScrollView();

            // Enhanced Pawns panel with search
            var viewHeightPawns = (allPawns.Count * 26f) + 90f; // +90f for search box and extra buttons
            var viewRectR = new Rect(0f, 0f, rightRect.width - 16f, viewHeightPawns);
            Widgets.BeginScrollView(rightRect, ref scrollPawns, viewRectR);
            var curR = new Vector2(0f, 0f);

            // Search box
            Widgets.Label(new Rect(curR.x, curR.y, 80f, 24f), "Search:");
            pawnSearchText = Widgets.TextField(new Rect(curR.x + 85f, curR.y, viewRectR.width - 85f, 24f), pawnSearchText);
            curR.y += 30f;

            // Select/Clear buttons
            if (Widgets.ButtonText(new Rect(curR.x, curR.y, 100f, 24f), "Select all"))
            {
                device.listedPawns.Clear();
                device.listedPawns.AddRange(allPawns);
            }
            if (Widgets.ButtonText(new Rect(curR.x + 105f, curR.y, 100f, 24f), "Clear all"))
            {
                device.listedPawns.Clear();
            }
            curR.y += 30f;

            // Status line
            Widgets.Label(new Rect(curR.x, curR.y, viewRectR.width, 24f),
                $"Mode: {device.allowedMode}");
            curR.y += 26f;

            // Filtered pawn list
            var filteredPawns = GetFilteredPawns();

            for (int i = 0; i < filteredPawns.Count; i++)
            {
                var pawn = filteredPawns[i];
                int originalIndex = allPawns.IndexOf(pawn);
                if (originalIndex >= 0 && originalIndex < pawnLabels.Count)
                {
                    bool listed = device.listedPawns.Contains(pawn);
                    bool before = listed;
                    string displayLabel = pawnLabels[originalIndex];

                    // Draw with color if slave
                    Rect labelRect = new Rect(curR.x, curR.y, viewRectR.width, 22f);
                    Widgets.CheckboxLabeled(labelRect, "", ref listed); // Checkbox only

                    // Custom label drawing for color
                    Rect textRect = new Rect(labelRect.x + 24f, labelRect.y, labelRect.width - 24f, labelRect.height);
                    if (pawn.IsSlaveOfColony)
                    {
                        GUI.color = Color.red;
                        Widgets.Label(textRect, displayLabel);
                        GUI.color = Color.white;
                    }
                    else
                    {
                        Widgets.Label(textRect, displayLabel);
                    }

                    if (listed != before)
                    {
                        if (listed)
                        {
                            if (!device.listedPawns.Contains(pawn))
                                device.listedPawns.Add(pawn);
                        }
                        else
                        {
                            device.listedPawns.Remove(pawn);
                        }
                    }
                    curR.y += 24f;
                }
            }

            Widgets.EndScrollView();

            // Bottom buttons
            var bottomRect = new Rect(inRect.x, inRect.yMax - 32f, inRect.width, 32f);
            if (Widgets.ButtonText(bottomRect.RightPartPixels(160f), "Close"))
            {
                Close();
            }
        }

        private List<Pawn> GetFilteredPawns()
        {
            if (string.IsNullOrEmpty(pawnSearchText))
                return allPawns;

            string searchLower = pawnSearchText.ToLower();
            var filtered = new List<Pawn>();

            for (int i = 0; i < allPawns.Count; i++)
            {
                if (i < pawnLabels.Count)
                {
                    string label = pawnLabels[i];
                    if (label.ToLower().Contains(searchLower))
                    {
                        filtered.Add(allPawns[i]);
                    }
                }
            }

            return filtered;
        }
    }
}