using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MDutility
{
    class RelationHigherLimit
    {
      public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == 100)
                {
                    Log.Message("Patched upper relation bound from 100 to 180");
                    codes[i].opcode = OpCodes.Ldc_I4;
                    codes[i].operand = 180;
                }
            }
            return codes.AsEnumerable();
        }
    }
}
