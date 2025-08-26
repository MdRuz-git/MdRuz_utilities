using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

//basically it allows deep penetration scanner to discover ores around itself 80x80 grid
//currently not checking for powerstatus but i like it that way
//however if the spot for ore vein is somehow incorrect or already occupied it will fall back to vanilla which has no range limit.
namespace MDutility
{
    public static class CellFinderLoose_TryFindRandomNotEdgeCellWith_Patch
    {
        public static bool Prefix(int minEdgeDistance, Predicate<IntVec3> validator, Map map, out IntVec3 result, ref bool __result)
        {
            result = IntVec3.Invalid;
            __result = false;

            // Get all deep scanners on this map
            List<Thing> scanners = map.listerThings.ThingsOfDef(ThingDefOf.GroundPenetratingScanner);

            // If we have any scanners, search within their areas
            //shouldn't be needed but might aswell
            if (scanners.Count > 0)
            {
                // Pick a random scanner to focus the search
                Thing randomScanner = scanners[Rand.Range(0, scanners.Count)];
                IntVec3 buildingPos = randomScanner.Position;
                // Define search area around this scanner
                CellRect searchArea = CellRect.CenteredOn(buildingPos, 40);
                searchArea.ClipInsideMap(map);

                // Try multiple attempts to find a valid cell
                for (int attempt = 0; attempt < 1000; attempt++)
                {
                    // Generate random candidate within this scanner's area
                    IntVec3 candidate = new IntVec3(
                        Rand.Range(searchArea.minX, searchArea.maxX + 1),
                        0,
                        Rand.Range(searchArea.minZ, searchArea.maxZ + 1)
                    );

                    // Validate the candidate
                    if (candidate.InBounds(map) &&
                        candidate.DistanceToEdge(map) >= minEdgeDistance &&
                        validator(candidate))
                    {
                        result = candidate;
                        __result = true; //harmony way of replacing return value of the original method.
                        return false; // Success! Found valid cell, skip original method
                    }
                }
            }
            
            return true;
        }
        /* fixed. no longer needed
        [HarmonyPatch(typeof(Log), nameof(Log.Error), new Type[] { typeof(string) })]
        public static class Log_Error_Patch
        {
            public static bool Prefix(string text)
            {
                if (text == "Could not find a center cell for deep scanning lump generation!")
                {
                    return false; // Suppress this specific error
                }
                return true; // Allow other errors to log normally
            }
        }
        */
    }
}