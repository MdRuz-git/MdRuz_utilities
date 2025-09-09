using RimWorld;
using Verse;

namespace UnlearnDevice
{
    [DefOf]
    public static class TUDefOf
    {
        public static ThingDef TU_UnlearningDevice;
        public static JobDef TU_UseUnlearningDevice;
        public static ThoughtDef TU_ForcedAgainstMyWill;

        static TUDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TUDefOf));
        }
    }
}