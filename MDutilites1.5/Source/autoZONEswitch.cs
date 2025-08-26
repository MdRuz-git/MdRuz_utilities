using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MDutility
{
    // Map component to store area pairs per map and track hostile status
    public class AreaPairingMapComponent : MapComponent
    {
        public List<AreaPair> areaPairs = new List<AreaPair>();
        public Dictionary<int, bool> areaPairingEnabled = new Dictionary<int, bool>(); // Key: Area ID

        // Track hostile status and tick per map
        public int tickCheck = 0;

        public AreaPairingMapComponent(Map map) : base(map) { }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref areaPairs, "areaPairs", LookMode.Deep);
            Scribe_Collections.Look(ref areaPairingEnabled, "areaPairingEnabled", LookMode.Value, LookMode.Value, ref areaKeys, ref boolValues);
            Scribe_Values.Look(ref tickCheck, "tickCheck", 0);
            if (areaPairs == null) areaPairs = new List<AreaPair>();
            if (areaPairingEnabled == null) areaPairingEnabled = new Dictionary<int, bool>();
        }

        private List<int> areaKeys;
        private List<bool> boolValues;

        public static AreaPairingMapComponent GetMapComponent(Map map)
        {
            return map.GetComponent<AreaPairingMapComponent>();
        }
    }

    // Area pair data structure
    public class AreaPair : IExposable
    {
        public int areaId1;
        public int areaId2;
        public int safeAreaId;

        public AreaPair() { }

        public AreaPair(int id1, int id2, int safeId)
        {
            areaId1 = id1;
            areaId2 = id2;
            safeAreaId = safeId;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref areaId1, "areaId1");
            Scribe_Values.Look(ref areaId2, "areaId2");
            Scribe_Values.Look(ref safeAreaId, "safeAreaId");
        }

        public bool ContainsArea(int areaId)
        {
            return areaId1 == areaId || areaId2 == areaId;
        }

        public int GetPairedAreaId(int areaId)
        {
            if (areaId == areaId1) return areaId2;
            if (areaId == areaId2) return areaId1;
            return -1;
        }

        public bool IsSafeArea(int areaId)
        {
            return safeAreaId == areaId;
        }
    }

    // Harmony patch to add pairing button to area management - moved to PatchAll
    public static class Dialog_ManageAreas_DoAreaRow_Patch
    {
        public static void Postfix(Rect rect, Area area, int i, Dialog_ManageAreas __instance)
        {
            // This patch applies to all Areas managed in the dialog
            Map map = (Map)AccessTools.Field(typeof(Dialog_ManageAreas), "map").GetValue(__instance);
            if (map == null) return;

            var component = AreaPairingMapComponent.GetMapComponent(map);
            if (component == null) return;

            // Add our pairing button at the end of the row
            Rect buttonRect = new Rect(rect.xMax - 10f, rect.y + (rect.height - 24f) / 2f, 45f, 24f);
            bool isPaired = component.areaPairingEnabled.ContainsKey(area.ID) &&
                           component.areaPairingEnabled[area.ID];
            string buttonText = isPaired ? "-----" : "+++++";

            if (Widgets.ButtonText(buttonRect, buttonText))
            {
                ToggleAreaPairing(area, component, map);
            }
        }

        private static void ToggleAreaPairing(Area area, AreaPairingMapComponent component, Map map)
        {
            if (!component.areaPairingEnabled.ContainsKey(area.ID))
            {
                component.areaPairingEnabled[area.ID] = false;
            }

            if (component.areaPairingEnabled[area.ID])
            {
                // Remove from any pairs
                component.areaPairingEnabled[area.ID] = false;
                component.areaPairs.RemoveAll(p => p.ContainsArea(area.ID));
            }
            else
            {
                // Check if we already have 2 paired areas
                var pairedAreas = component.areaPairingEnabled.Where(kv => kv.Value).ToList();
                if (pairedAreas.Count >= 2)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        "There are already 2 areas in the pair, do you want to create new pair?",
                        "Yes", () => {
                            // Remove existing pairs
                            foreach (var kv in pairedAreas)
                            {
                                component.areaPairingEnabled[kv.Key] = false;
                            }
                            component.areaPairs.Clear();
                            // Add new area
                            component.areaPairingEnabled[area.ID] = true;
                        },
                        "No", () => { }));
                }
                else
                {
                    component.areaPairingEnabled[area.ID] = true;
                    // If we now have exactly 2 paired areas, create a pair
                    var newPairedAreas = component.areaPairingEnabled.Where(kv => kv.Value).ToList();
                    if (newPairedAreas.Count == 2)
                    {
                        var areaIds = newPairedAreas.Select(kv => kv.Key).ToList();
                        Find.WindowStack.Add(new Dialog_ConfirmAreaPair(
                            areaIds[0], areaIds[1], map, (safeAreaId) => {
                                component.areaPairs.Add(new AreaPair(areaIds[0], areaIds[1], safeAreaId));
                            }));
                    }
                }
            }
        }
    }

    // Dialog for confirming area pair and selecting safe area
    public class Dialog_ConfirmAreaPair : Window
    {
        private int areaId1;
        private int areaId2;
        private Map map;
        private Action<int> onConfirm;
        private Area area1;
        private Area area2;

        public Dialog_ConfirmAreaPair(int id1, int id2, Map map, Action<int> confirmAction)
        {
            areaId1 = id1;
            areaId2 = id2;
            this.map = map;
            onConfirm = confirmAction;
            // Get areas by ID from the map's area manager
            area1 = map.areaManager.AllAreas.FirstOrDefault(a => a.ID == areaId1);
            area2 = map.areaManager.AllAreas.FirstOrDefault(a => a.ID == areaId2);
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(500f, 250f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 55f), "Which area should act as a WORK area? (no enemies on the map)");
            Text.Font = GameFont.Small;

            float buttonHeight = 35f;
            float buttonWidth = 200f;

            if (area1 != null && Widgets.ButtonText(
                new Rect((inRect.width - buttonWidth) / 2f, 50f, buttonWidth, buttonHeight),
                area1.Label))
            {
                onConfirm(areaId1);
                Close();
            }

            if (area2 != null && Widgets.ButtonText(
                new Rect((inRect.width - buttonWidth) / 2f, 100f, buttonWidth, buttonHeight),
                area2.Label))
            {
                onConfirm(areaId2);
                Close();
            }
        }
    }

    // Harmony patch for pawn switching based on threat status; patch itself moved to PatchALL.
    public static class MapComponentUtility_MapComponentUpdate_Patch
    {
        // Removed static variables here

        public static void Postfix(Map map)
        {
            var component = AreaPairingMapComponent.GetMapComponent(map);
            if (component == null) return; // Safety check

            // Only check every 300 ticks to avoid performance issues
            if (Find.TickManager.TicksGame - component.tickCheck > 300)
            {
                component.tickCheck = Find.TickManager.TicksGame;
                // Check if there are hostile pawns
                bool hasHostiles = map.mapPawns.AllPawnsSpawned.Any(p =>
                p.HostileTo(Faction.OfPlayer) && !p.Downed);

                ProcessAreaSwitching(map, hasHostiles, component); // Pass component, run it
            }
        }

        private static void ProcessAreaSwitching(Map map, bool hasHostiles, AreaPairingMapComponent component)
        {
            // Use the passed component instead of getting it again
            if (component.areaPairs == null || !component.areaPairs.Any()) return;

            foreach (var pair in component.areaPairs)
            {
                // Determine target area based on threat status
                // FIXED LOGIC:
                // If hostiles -> Use the NON-safe area (combat area)
                // If no hostiles -> Use the safe area
                int targetAreaId = hasHostiles ?
                    pair.GetPairedAreaId(pair.safeAreaId) : // Get the ID of the area paired with the safe one
                    pair.safeAreaId;                       // Use the safe area ID directly

                // Debug.Log($"[MDutility] Map {map.uniqueID}: Hostiles={hasHostiles}, SafeArea={pair.safeAreaId}, TargetArea={targetAreaId}");

                foreach (var pawn in GetAreaAssignablePawns(map))
                {
                    
                    // Get pawn's current assigned area using the correct property
                    Area pawnArea = pawn.playerSettings?.AreaRestrictionInPawnCurrentMap;

                    // If pawn is assigned to a paired area
                    if (pawnArea != null && pair.ContainsArea(pawnArea.ID))
                    {
                        // Skip if already assigned to target area
                        if (pawnArea.ID == targetAreaId) continue;

                        // Switch to the target area
                        var targetArea = map.areaManager.AllAreas
                            .FirstOrDefault(a => a.ID == targetAreaId);

                        if (targetArea != null)
                        {
                            // Debug.Log($"[MDutility] Switching {pawn.LabelShort} from {pawnArea.Label} to {targetArea.Label}");
                            // Assign pawn to the target area using the correct property
                            pawn.playerSettings.AreaRestrictionInPawnCurrentMap = targetArea;
                        }
                    }
                }
            }
        }
        private static IEnumerable<Pawn> GetAreaAssignablePawns(Map map)
        {
            var pawns = new List<Pawn>();
            // Colonists
            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.IsPrisoner) continue;
                pawns.Add(pawn);
                //if (pawn.Faction != Faction.OfPlayer) continue; //already included inside FreeColonistsSpawned

                //yield return pawn; //never again use yield return for IEnumerable - collection modified error
            }

            // Zonable colony animals
            foreach (var animal in map.mapPawns.SpawnedColonyAnimals)
            {
                //not sure if checking for empty tag is good idea, maybe there are some animals with no tags but are zonable?
                if (/* !animal.def.tradeTags.NullOrEmpty<string>() && */ !animal.def.tradeTags.Contains("AnimalFarm"))
                {
                    pawns.Add(animal);
                }
            }
            return pawns;
        }

    }
}