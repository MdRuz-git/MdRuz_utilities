using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MDutility
{
    public class CompClearScanner : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "Clear Scanner Data",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ActivateTurret", true),
                defaultDesc = "Clear all scanned underground resources",
                action = () => Find.WindowStack.Add(new Dialog_MessageBox(
                    "This action will permanently clear all scanned progress. Do you wish to continue?",
                    "No".Translate(),
                    null,
                    "Yes".Translate(),
                    ClearUndergroundResources,
                    null,
                    false,
                    null,
                    null
                ))
            };
        }

        private void ClearUndergroundResources()
        {
            Map map = parent.Map;
            if (map == null) return;

            // Clear the underground resource grid properly
            for (int x = 0; x < map.Size.x; x++)
            {
                for (int z = 0; z < map.Size.z; z++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    map.deepResourceGrid.SetAt(cell, null, 0);
                }
            }
        }
    }

    public class CompProperties_ClearScanner : CompProperties
    {
        public CompProperties_ClearScanner()
        {
            compClass = typeof(CompClearScanner);
        }
    }
}