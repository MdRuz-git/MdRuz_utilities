using HarmonyLib;
using Verse.AI;
using Verse;
using System.Collections.Generic;


//allows ignoring pathcost of whatever is on top of negative pathcost flooring.
namespace MDutility
{

    public static class PathGrid_CalculatedCostAt
    {
        private static AccessTools.FieldRef<PathGrid, Map> mapField =
        AccessTools.FieldRefAccess<PathGrid, Map>("map");

        /*
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instrs)
        {
            instrs = instrs.MethodReplacer(typeof(SnowUtility).GetMethod("MovementTicksAddOn"), typeof(PathGrid_CalculatedCostAt).GetMethod("MovementTicksAddOnIgnoreZero"));
            return instrs;
        }
        */

        //allows ignoring pathcost of whatever is on top of negative pathcost flooring.
        public static void Postfix(IntVec3 c, ref int __result, PathGrid __instance)
        {
            Map map = mapField(__instance);
            if (map != null)
            {
                TerrainDef terrain = map.terrainGrid.TerrainAt(c);
                if (terrain != null && terrain.pathCost < 0)
                {
                    if (__result < 10000)
                    {
                        __result = -1;
                    }
                }
            }
        }

       

    }
}