using System;
using RimWorld;
using Verse;

namespace MDutility
{
    [StaticConstructorOnStartup]
    public static class LoseTheFaith
    {
        // vanilla: certainty of pawns ideology always increases
        // modded: only if mood above half, below 0.5 mood it will decrease
        // prisoners with low mood will start to doubt their ideology
        static LoseTheFaith()
        {
            ConversionTuning.CertaintyPerDayByMoodCurve.SetPoints(new SimpleCurve
            {
                {
                    new CurvePoint(0f, -0.03f),
                    true
                },
                {
                    new CurvePoint(0.2f, -0.02f),
                    true
                },
                {
                    new CurvePoint(0.4f, -0.01f),
                    true
                },
                {
                    new CurvePoint(0.5f, 0f),
                    true
                },
                {
                    new CurvePoint(0.6f, 0.01f),
                    true
                },
                {
                    new CurvePoint(0.8f, 0.02f),
                    true
                },
                {
                    new CurvePoint(0.9f, 0.03f),
                    true
                }
            });
        }
    }
}
