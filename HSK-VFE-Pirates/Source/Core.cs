using System;
using HarmonyLib;
using HSKVFEPiratesCompat.Patches;
using Verse;

namespace HSKVFEPiratesCompat
{
    /// <summary>
    /// Entry point for HSK-VFE-Pirates compatibility patches.
    ///
    /// Defers work to <see cref="LongEventHandler.ExecuteWhenFinished"/> so
    /// we run AFTER VFE Pirates' own static constructor has applied its
    /// Harmony patches via <c>new Harmony("VFEPirates.Mod").PatchAll()</c>.
    /// Our Unpatch calls then actually find and remove their patches
    /// instead of being no-ops.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Core
    {
        public const string HarmonyId = "CarbineAction.HSK.VFE.Pirates.Compat";

        static Core()
        {
            LongEventHandler.ExecuteWhenFinished(ApplyPatches);
        }

        private static void ApplyPatches()
        {
            try
            {
                var harmony = new Harmony(HarmonyId);
                UnpatchStripBlocker.Apply(harmony);
                RestrictWarcasketWear.Apply(harmony);
                ForceWarcasketDroppable.Apply(harmony);
                UniversalRemoveWarcasket.Apply();

                // Pick up any [HarmonyPatch]-attributed classes in this assembly
                // (currently: MakeRecipeProducts_Patch for dynamic salvage yield).
                harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
                Log.Message("[HSK VFE Pirates Compat] All compat patches applied.");
            }
            catch (Exception e)
            {
                Log.Error("[HSK VFE Pirates Compat] Failed to apply patches: " + e);
            }
        }
    }
}
