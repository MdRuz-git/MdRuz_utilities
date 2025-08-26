using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

/* compiled for rimworld 1.5, will see if it survives 1.6
 * 
 * 
 * This class remembers all bills inside any player production building per map
 * ?if that building ever gets destroyed (not deconstructed)
 * ?then if its ever rebuilt
 *
 * =the bills will still be there
 * 
 * purposefully made so that it never actually scans the whole map or anything like that (except when first loading savefile) you wont notice
 * should not have any impact of TPS 
 *
 *when player building is destroyed
 *if its production building
 *save it as hash and copy of its recipes
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

        //writes and reads savegame file
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref savedBills, "savedBills", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref destroyedBuildings, "destroyedBuildings", LookMode.Value);

            if (savedBills == null) savedBills = new Dictionary<string, List<Bill>>();
            if (destroyedBuildings == null) destroyedBuildings = new HashSet<string>();
        }
    }

    public static class BillManager
    {
        // Initialize and capture all bills on this map (per active map) runs only once
        public static void InitializeBills(BillManagerMapComponent component)
        {
            // Run culling once GC - this shouldn't even create loop in an extreme scenario since even if the number exceeds the treshold
            // it only runs once per map init
            // so the only loop that would be there is if you have carefully made a dev debugging map for the purpose of breaking it
            // where is the only thing that would happen is the list would just have to be repopulated thats it

            if (component.savedBills.Count > 1000 || component.destroyedBuildings.Count > 1000)
            {
                component.savedBills.Clear();
                component.destroyedBuildings.Clear();
            }

            Map map = component.map;

            foreach (Building building in map.listerBuildings.allBuildingsColonist) //only slow thing in this class, but since it only runs once i dont care. and its not even that slow
            {
                if (IsProductionBuilding(building))
                {
                    SaveAllBillsForBuilding(building, component);
                }
            }

            //Log.Message($"BillManager initialized: Saved bills for {component.savedBills.Count} building instances");
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