using System;
using HarmonyLib;
using Verse;

namespace HSKVFEPiratesCompat.Patches
{
    /// <summary>
    /// Removes VFE Pirates' strip-blocking Harmony patches so colonists
    /// can strip warcasket-wearing corpses like any other pawn.
    ///
    /// Upstream VFE Pirates defines three strip/drop blockers on warcasket-
    /// wearing pawns:
    ///   [HarmonyPatch(typeof(Pawn), "AnythingToStrip")]  → blocks Strip menu
    ///   [HarmonyPatch(typeof(Pawn_ApparelTracker), "Unlock")] → keeps pieces
    ///       specifically locked even if a strip job runs
    ///   [HarmonyPatch(typeof(Pawn), "Strip")] → caches pawn for ButcherProducts
    ///       (benign; left alone)
    ///
    /// Current design goal: let the player Strip warcasket corpses for
    /// weapons + any non-warcasket apparel, but leave the warcasket pieces
    /// LOCKED to the corpse (so they can't be directly worn by colonists
    /// without the VFEP_WarcasketTrait). To recover warcasket materials,
    /// the player hauls the corpse to the ElectricCrematorium and runs
    /// the HSK_SalvageWarcasketCorpse recipe — burns the body, emits ~50%
    /// of the original craft cost as SteelBar/Plasteel/ComponentIndustrial.
    ///
    /// To achieve that:
    ///   - AnythingToStrip Prefix is UNPATCHED → "Strip" menu appears and
    ///     the job runs. Vanilla strip iterates apparel + equipment.
    ///   - ApparelTracker.Unlock Prefix is LEFT IN PLACE (not unpatched)
    ///     → warcasket-class apparel remains "locked" during the strip
    ///     loop, so weapons + non-warcasket apparel drop but warcasket
    ///     pieces stay attached to the corpse for later cremator salvage.
    ///
    /// Earlier versions of this compat DLL also unpatched Unlock, which
    /// made warcasket pieces drop intact. That worked, but the player
    /// could then queue TableMachining recycle bills for the pieces,
    /// effectively bypassing the "body-burning" flavor the design wants.
    /// Restoring the Unlock block routes all warcasket recovery through
    /// the cremator + corpse path instead.
    ///
    /// RestrictWarcasketWear still runs as a safety net — even if a
    /// warcasket piece somehow reaches a colonist's inventory, only a
    /// VFEP_WarcasketTrait pawn can wear it.
    /// </summary>
    public static class UnpatchStripBlocker
    {
        public const string VFEPiratesHarmonyId = "VFEPirates.Mod";

        public static void Apply(Harmony harmony)
        {
            // Unpatch ONLY the AnythingToStrip prefix so the menu appears
            // and vanilla strip runs. Leave the Pawn_ApparelTracker.Unlock
            // prefix in place — that keeps warcasket pieces attached to
            // the corpse, so weapons + normal apparel drop but the
            // warcasket itself must be burned at the cremator to recover
            // materials (see HSK_SalvageWarcasketCorpse recipe).
            UnpatchOne(harmony,
                AccessTools.Method(typeof(Verse.Pawn), "AnythingToStrip"),
                "Pawn.AnythingToStrip",
                "Warcasket corpses now show the Strip menu; weapons + non-warcasket apparel drop. Warcasket pieces remain attached for Cremator salvage.");
        }

        private static void UnpatchOne(Harmony harmony, System.Reflection.MethodBase target, string name, string effect)
        {
            if (target == null)
            {
                Log.Warning("[HSK VFE Pirates Compat] Could not find " + name + "; unpatch skipped.");
                return;
            }
            try
            {
                harmony.Unpatch(target, HarmonyPatchType.Prefix, VFEPiratesHarmonyId);
                Log.Message("[HSK VFE Pirates Compat] Unpatched VFE Pirates prefix on " + name + ". " + effect);
            }
            catch (Exception e)
            {
                Log.Warning("[HSK VFE Pirates Compat] Unpatch of " + name + " failed: " + e);
            }
        }
    }
}
