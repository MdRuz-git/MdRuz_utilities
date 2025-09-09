using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

/* 
 * remembers all bills inside any player production building per map
 * ?if that building ever gets destroyed (not deconstructed)
 * ?then if its ever rebuilt
 *
 * =the bills will still be there
 * 
 * purposefully made so that it never actually scans the whole map or anything like that
 * should not have any impact of TPS 
 *
 *when player building is destroyed
 *if its production building
 *save it as hash and copy of its recipes (objects)
 *
 *when building is spawned (which only gets called on map initialization and when building something)
 *
 *
 */



namespace MDutility
{

    //MapComponent
    public class BillManagerMapComponent : MapComponent
    {
        //C# 7.3: Explicit type required
        //any variable here will have its own instance per map

        //automatically removed with the map removal
        public Dictionary<string, List<Bill>> savedBills = new Dictionary<string, List<Bill>>();
        public HashSet<string> destroyedBuildings = new HashSet<string>();

        //initialize this MapComponent
        public BillManagerMapComponent(Map map) : base(map)
        {
        }
        //runs this MapComponent on each active map
        public override void FinalizeInit()
        {
            BillManager.InitializeBills(this);
        }

        /* 
         * had to remove the ability of deepsaving since I just can't figure out how to assign unique ID to copied bill objects
        //writes and reads savegame file
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref savedBills, "savedBills", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref destroyedBuildings, "destroyedBuildings", LookMode.Value);

            if (savedBills == null) savedBills = new Dictionary<string, List<Bill>>();
            if (destroyedBuildings == null) destroyedBuildings = new HashSet<string>();
        }
        */
    }

    public static class BillManager
    {
        public static void InitializeBills(BillManagerMapComponent component)
        {

            //if (component.savedBills.Count > 1000 || component.destroyedBuildings.Count > 1000)
            //{
                component.savedBills.Clear();
                component.destroyedBuildings.Clear();
            //}


        }

        public static void CheckAndRestoreBills(Building newBuilding)
        {
            //function would not even be called if the building was null
            if (!IsProductionBuilding(newBuilding)) return; 
            string newBuildingHash = GenerateBuildingHash(newBuilding);

            BillManagerMapComponent component = newBuilding.Map.GetComponent<BillManagerMapComponent>();
            if (component == null) return; //can it ever be null? spawnsetup already ran, so map had to be initialized. might be redundant

            
            if (component.destroyedBuildings.Contains(newBuildingHash)) // Early exit if not in destroyed buildings hashset
            {
                LoadBillsToBuilding(newBuilding, component);
                Log.Message($"♻️ Restored bills for rebuilt building: {newBuildingHash}");
                component.destroyedBuildings.Remove(newBuildingHash);
            }
        }

        public static void SaveAllBillsForBuilding(Building building, BillManagerMapComponent component)
        {
            if (!IsProductionBuilding(building)) return;

            string buildingKey = GenerateBuildingHash(building);
            var billGiver = building as IBillGiver;

            // Clear existing entry for this specific building
            component.savedBills[buildingKey] = new List<Bill>();

            // Clone and save all current bills
            foreach (Bill bill in billGiver.BillStack)
            {
                if (bill is Bill_Production prodBill)
                {
                    Bill cloned = prodBill.Clone();
                    component.savedBills[buildingKey].Add(cloned);
                }
            }

            //Log.Message($"Updated bills for {buildingKey}: {component.savedBills[buildingKey].Count} bills saved");
        }


        // Load all saved bills to a rebuilt building
        public static void LoadBillsToBuilding(Building building, BillManagerMapComponent component)
        {
            if (!IsProductionBuilding(building)) return;
            string buildingKey = GenerateBuildingHash(building);
            IBillGiver billGiver = building as IBillGiver;

            //Log.Message($"LoadBillsToBuilding: {buildingKey} at {building.Position}");

            if (component.savedBills.TryGetValue(buildingKey, out List<Bill> bills))
            {
                billGiver.BillStack.Clear();
                foreach (Bill savedBill in bills)
                {
                    billGiver.BillStack.AddBill(savedBill.Clone());
                }
                Log.Message($"Loaded {bills.Count} bills for {building.ThingID}");
            }
            else
            {
                //Log.Message($"ERROR: Tried loading bills for non existing buildingID {building.ThingID}");
            }

            //Log.Message($"Cleanup: Removing {buildingKey} from saved bills & destroyedBuildings cache");
            component.savedBills.Remove(buildingKey); //extra small cleanup, although we prob gonna save them again but whatever
            component.destroyedBuildings.Remove(buildingKey);//no longer needed, removing. -restored bills, building got rebuilt
        }

        private static bool IsProductionBuilding(Building building)
        {
            return building.Faction == Faction.OfPlayer && building is IBillGiver;
        }

        private static string GenerateBuildingHash(Building building)
        {
            // Format: "DefName_X_Y_Z_MapID" (e.g., "ElectricSmelter_12_0_45_Map3445")
            return $"{building.def.defName}_{building.Position.x}_{building.Position.y}_{building.Position.z}_{building.Map.uniqueID}";
        }

        // ================================
        // 🧩 HARMONY PATCHES - moved to PatchALL
        // ================================
        /*
        public static class Map_FinalizeInit_Patch
        {
            public static void Postfix(Map __instance)
            {
                if (__instance == Find.CurrentMap)
                {
                    __instance.GetComponent<BillManagerMapComponent>();
                }
            }
        }
        */

        public static class Building_SpawnSetup_Patch
        {
            public static void Postfix(Building __instance, ref Map map, ref bool respawningAfterLoad) //might actually be (Building __instance, ref Map map, ref bool respawningAfterLoad)
            {
                if (respawningAfterLoad) return;
                if (map == null) return;
                if (__instance.Faction != Faction.OfPlayer) return;
                if (!(__instance is IBillGiver)) return;

                //Log.Message($"SpawnSetup detected the following building __instance: {__instance}");
                CheckAndRestoreBills(__instance);
            }
        }
        public static class Building_Destroy_Patch
        {
            public static void Prefix(Building __instance, DestroyMode mode)
            {
                if (Find.PlaySettings.autoRebuild) //if the autorebuild is turned off don't even bother
                {
                    if (mode == DestroyMode.KillFinalize || mode == DestroyMode.KillFinalizeLeavingsOnly)
                    {
                        //Log.Message($"detected building Destruction at {__instance}");
                        if (IsProductionBuilding(__instance))
                        {
                            string buildingHash = GenerateBuildingHash(__instance);
                            BillManagerMapComponent component = __instance.Map.GetComponent<BillManagerMapComponent>();
                            if (component == null) return;

                            SaveAllBillsForBuilding(__instance, component);
                            component.destroyedBuildings.Add(buildingHash);

                            Log.Message($"🗑️ Remembering destroyed production building ID {buildingHash} type: {__instance.def.defName}");
                        }
                    }

                }
            }
        }
    }
}