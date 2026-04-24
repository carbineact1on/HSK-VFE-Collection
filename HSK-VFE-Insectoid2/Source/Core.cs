using System;
using HarmonyLib;
using HSKVFEInsectoidsCompat.Fixes;
using Verse;

namespace HSKVFEInsectoidsCompat
{
    /// <summary>
    /// Entry point for HSK-VFE-Insectoid2 compatibility patches.
    ///
    /// Timing challenge: our fixes must run AFTER
    /// <c>VFEInsectoids.HarmonyInit</c>'s static ctor (which does
    /// <c>new Harmony("VFEInsectoidsMod").PatchAll()</c>) but BEFORE
    /// <c>GeologicalLandforms.GeologicalLandformsAPI.Init()</c> tries to
    /// apply its transpilers via LunarFramework's PatchGroup. Both happen
    /// during the <c>[StaticConstructorOnStartup]</c> phase, so neither a
    /// static ctor nor <c>LongEventHandler.ExecuteWhenFinished</c> lands
    /// in that window (one runs too early, the other too late).
    ///
    /// Solution: Harmony-prefix <c>GeologicalLandformsAPI.Init</c> itself.
    /// That method is called once per GL init, right before it registers
    /// its patches. Our prefix runs unpatch-and-reapply just-in-time, then
    /// GL's original Init proceeds on pristine IL and succeeds.
    ///
    /// If GL isn't loaded, the target method doesn't exist and the patch
    /// silently does nothing.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Core
    {
        public const string HarmonyId = "CarbineAction.HSK.VFE.Insectoid2.Compat";

        private static Harmony _harmony;
        private static bool _fixesApplied;

        static Core()
        {
            try
            {
                _harmony = new Harmony(HarmonyId);

                // Prefix GeologicalLandformsAPI.Init so our fixes run between
                // Insectoids' PatchAll and GL's patch application.
                var glInit = AccessTools.Method(
                    "GeologicalLandforms.GeologicalLandformsAPI:Init");
                if (glInit == null)
                {
                    // GL not loaded → nothing to fix. Silent no-op.
                    return;
                }

                var prefix = AccessTools.Method(
                    typeof(Core), nameof(GLInit_Prefix));
                _harmony.Patch(glInit, prefix: new HarmonyMethod(prefix));
            }
            catch (Exception e)
            {
                Log.Error("[HSK VFE Insectoids Compat] Failed to install GL init hook: " + e);
            }
        }

        /// <summary>
        /// Runs immediately before <c>GeologicalLandformsAPI.Init()</c>.
        /// At this point VFE Insectoids has finished PatchAll (static ctors
        /// run before GL init), so our Unpatch calls will actually find and
        /// remove Insectoids' transpilers.
        /// </summary>
        public static void GLInit_Prefix()
        {
            if (_fixesApplied) return;
            _fixesApplied = true;

            try
            {
                InsectoidsLandformsFix.Apply(_harmony);
                InsectoidsAnimalSpawnerFix.Apply(_harmony);
            }
            catch (Exception e)
            {
                Log.Error("[HSK VFE Insectoids Compat] Failed to apply compatibility patches: " + e);
            }
        }
    }
}
