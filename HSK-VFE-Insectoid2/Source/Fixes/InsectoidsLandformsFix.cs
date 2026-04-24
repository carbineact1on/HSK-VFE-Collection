using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace HSKVFEInsectoidsCompat.Fixes
{
    /// <summary>
    /// Fix: VFE Insectoids 2 × Geological Landforms conflict on
    /// <c>RimWorld.Planet.WITab_Terrain.FillTab</c>.
    ///
    /// Both mods apply a Harmony transpiler to FillTab:
    ///   - VFE Insectoids uses the default priority (400). Its transpiler
    ///     scans for the operand string "AverageDiseaseFrequency" and injects
    ///     three IL instructions (Ldloc_S 5, Ldloc_S 3, Call AddInfestationLabel)
    ///     after that anchor to draw the "Infestation: Present" line.
    ///   - Geological Landforms uses priority 200 and runs a chained
    ///     <c>TranspilerPattern.OnlyMatchAfter</c> pipeline that replaces the
    ///     vanilla "Special features" LabelDouble call with its own method
    ///     (DoSpecialFeatures), which draws the landform name, bordering
    ///     biomes, caves, and — crucially — the "Search for landform nearby"
    ///     button via <c>GeologicalLandformsAPI.TerrainTabUI.Apply</c>.
    ///
    /// In Harmony, <b>higher priority runs first</b>. Default (400) beats 200,
    /// so Insectoids' transpiler mutates the IL before GL sees it. GL's
    /// sequential pattern matches (BiomeLabel → BiomeDescription →
    /// SpecialFeaturesCond → SpecialFeaturesExt) then fail silently, and the
    /// landform UI + Search button never render.
    ///
    /// Fix: at startup, when both mods are present, unpatch Insectoids'
    /// transpiler (owner ID "VFEInsectoidsMod") and re-apply an identical
    /// replacement transpiler at priority 100 — below GL's 200. GL now runs
    /// first on pristine IL and succeeds; our replacement then runs on the
    /// GL-modified IL and injects the infestation label. Both features work.
    ///
    /// If either mod is missing, or the unpatch fails, this fix no-ops and
    /// logs a warning — behavior falls back to whatever was loaded.
    /// </summary>
    public static class InsectoidsLandformsFix
    {
        public const string InsectoidsHarmonyId = "VFEInsectoidsMod";
        public const string GeologicalLandformsPackageId = "m00nl1ght.GeologicalLandforms";

        // Cached reflection handle for VFEInsectoids.Utils.IsInfestedTile(int).
        // Resolved once on first call to AddInfestationLabel so we don't require
        // a compile-time reference to VFEInsectoids.dll.
        private static MethodInfo _isInfestedTile;
        private static bool _isInfestedTileResolved;

        public static void Apply(Harmony harmony)
        {
            // Only fire when Geological Landforms is also present — otherwise
            // there's nothing to fix and Insectoids' original transpiler works
            // fine on its own.
            if (!ModsConfig.IsActive(GeologicalLandformsPackageId))
            {
                return;
            }

            var target = AccessTools.Method(typeof(WITab_Terrain), "FillTab");
            if (target == null)
            {
                Log.Warning("[HSK VFE Insectoids Compat] Could not find WITab_Terrain.FillTab");
                return;
            }

            try
            {
                // 1. Strip VFE Insectoids' transpiler off WITab_Terrain.FillTab.
                harmony.Unpatch(target, HarmonyPatchType.Transpiler, InsectoidsHarmonyId);

                // 2. Re-apply our identical replacement at priority 100 so
                //    Geological Landforms (priority 200) runs first.
                var transpiler = AccessTools.Method(
                    typeof(InsectoidsLandformsFix),
                    nameof(ReplacementTranspiler));

                harmony.Patch(target,
                    transpiler: new HarmonyMethod(transpiler) { priority = 100 });

                Log.Message(
                    "[HSK VFE Insectoids Compat] Applied Geological Landforms compatibility fix " +
                    "(unpatched Insectoids transpiler, re-applied at priority 100).");
            }
            catch (Exception e)
            {
                Log.Warning("[HSK VFE Insectoids Compat] Geological Landforms fix failed: " + e);
            }
        }

        /// <summary>
        /// Reimplementation of <c>VFEInsectoids.WITab_Terrain_FillTab_Patch.Transpiler</c>.
        /// IL injection is identical; only difference is the Harmony priority
        /// set by Apply() — this runs AFTER Geological Landforms instead of before.
        /// </summary>
        public static IEnumerable<CodeInstruction> ReplacementTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            foreach (var instruction in codeInstructions)
            {
                yield return instruction;
                if (instruction.OperandIs("AverageDiseaseFrequency"))
                {
                    // Local slot 5 = tileInt, slot 3 = listing_Standard
                    // (matches the indices VFE Insectoids uses in vanilla FillTab IL).
                    yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)5);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)3);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(
                            typeof(InsectoidsLandformsFix),
                            nameof(AddInfestationLabel)));
                }
            }
        }

        /// <summary>
        /// Identical to <c>VFEInsectoids.WITab_Terrain_FillTab_Patch.AddInfestationLabel</c>.
        /// Resolved late-bound so we don't need a compile-time reference to VFEInsectoids.dll.
        /// </summary>
        public static void AddInfestationLabel(int tileInt, Listing_Standard listingStandard)
        {
            if (!_isInfestedTileResolved)
            {
                _isInfestedTileResolved = true;
                var utilsType = AccessTools.TypeByName("VFEInsectoids.Utils");
                if (utilsType != null)
                {
                    _isInfestedTile = AccessTools.Method(utilsType, "IsInfestedTile", new[] { typeof(int) });
                }
                if (_isInfestedTile == null)
                {
                    Log.Warning("[HSK VFE Insectoids Compat] VFEInsectoids.Utils.IsInfestedTile not found; infestation labels disabled.");
                }
            }

            if (_isInfestedTile == null) return;

            bool infested;
            try
            {
                infested = (bool)_isInfestedTile.Invoke(null, new object[] { tileInt });
            }
            catch
            {
                return;
            }

            if (infested)
            {
                listingStandard.LabelDouble(
                    "VFEI_Infestation".Translate(),
                    "VFEI_Present".Translate(),
                    "VFEI_InfestationTooltip".Translate());
            }
        }
    }
}
