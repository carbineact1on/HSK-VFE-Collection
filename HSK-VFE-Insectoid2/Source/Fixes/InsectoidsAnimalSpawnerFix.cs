using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKVFEInsectoidsCompat.Fixes
{
    /// <summary>
    /// Fix: VFE Insectoids 2 × Geological Landforms conflict on
    /// <c>RimWorld.WildAnimalSpawner.SpawnRandomWildAnimalAt</c>.
    ///
    /// Same bug class as <see cref="InsectoidsLandformsFix"/>, different method.
    /// Both mods transpile SpawnRandomWildAnimalAt:
    ///   - VFE Insectoids (priority 400, default) injects calls after
    ///     <c>BiomeDef.AllWildAnimals</c> (→ TryOverrideWildAnimals, swapping
    ///     in insects on infested maps) and after <c>GenSpawn.Spawn</c>
    ///     (→ TryAddLordJob, adding spawned pawns to infestation lords).
    ///   - Geological Landforms (priority 200) uses a TranspilerPattern
    ///     matcher called "AdjustPotentialAnimalSpawns" to adjust spawn
    ///     weights per landform.
    ///
    /// Insectoids' higher priority runs first and mutates the IL. GL's
    /// pattern then fails to match (0 matches when ≥1 was expected), which
    /// throws a HarmonyException inside LunarFramework's PatchGroup.TryPatch.
    /// That exception aborts ALL of GL's initialization — including the
    /// WITab_Terrain.FillTab patch that draws the "Search for landform
    /// nearby" button. So the InsectoidsLandformsFix on its own isn't
    /// enough; this sibling fix is also required.
    ///
    /// Fix: unpatch Insectoids' transpiler (owner "VFEInsectoidsMod") and
    /// re-apply an identical replacement at priority 100 (below GL's 200).
    /// GL now runs first on pristine IL → pattern matches → init succeeds
    /// → FillTab button + landform UI render correctly. Our replacement
    /// then runs on GL-modified IL and re-injects the insect spawn
    /// overrides, which still work because GL leaves the anchor call sites
    /// (AllWildAnimals getter, GenSpawn.Spawn) in place.
    /// </summary>
    public static class InsectoidsAnimalSpawnerFix
    {
        public const string InsectoidsHarmonyId = "VFEInsectoidsMod";
        public const string GeologicalLandformsPackageId = "m00nl1ght.GeologicalLandforms";

        // Cached reflection handles for the static helpers inside VFEInsectoids.
        // Resolved once so we don't require a compile-time reference to VFEInsectoids.dll.
        private static MethodInfo _tryOverrideWildAnimals;
        private static MethodInfo _tryAddLordJob;
        private static bool _helpersResolved;

        public static void Apply(Harmony harmony)
        {
            if (!ModsConfig.IsActive(GeologicalLandformsPackageId))
            {
                return;
            }

            var target = AccessTools.Method(typeof(WildAnimalSpawner), "SpawnRandomWildAnimalAt");
            if (target == null)
            {
                Log.Warning("[HSK VFE Insectoids Compat] Could not find WildAnimalSpawner.SpawnRandomWildAnimalAt");
                return;
            }

            try
            {
                harmony.Unpatch(target, HarmonyPatchType.Transpiler, InsectoidsHarmonyId);

                var transpiler = AccessTools.Method(
                    typeof(InsectoidsAnimalSpawnerFix),
                    nameof(ReplacementTranspiler));

                harmony.Patch(target,
                    transpiler: new HarmonyMethod(transpiler) { priority = 100 });

                Log.Message(
                    "[HSK VFE Insectoids Compat] Applied Geological Landforms compatibility fix " +
                    "on WildAnimalSpawner.SpawnRandomWildAnimalAt (unpatched Insectoids transpiler, " +
                    "re-applied at priority 100).");
            }
            catch (Exception e)
            {
                Log.Warning("[HSK VFE Insectoids Compat] WildAnimalSpawner fix failed: " + e);
            }
        }

        /// <summary>
        /// Reimplementation of <c>VFEInsectoids.WildAnimalSpawner_SpawnRandomWildAnimalAt_Patch.Transpiler</c>.
        /// Same anchors, same injections; only the Harmony priority differs.
        /// </summary>
        public static IEnumerable<CodeInstruction> ReplacementTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var allWildAnimals = AccessTools.PropertyGetter(typeof(BiomeDef), "AllWildAnimals");
            var spawnPawn = AccessTools.Method(typeof(GenSpawn), "Spawn", new[]
            {
                typeof(Thing), typeof(IntVec3), typeof(Map), typeof(WipeMode)
            });

            var tryOverride = AccessTools.Method(
                typeof(InsectoidsAnimalSpawnerFix), nameof(TryOverrideWildAnimals_Proxy));
            var tryAddLord = AccessTools.Method(
                typeof(InsectoidsAnimalSpawnerFix), nameof(TryAddLordJob_Proxy));

            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(allWildAnimals))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, tryOverride);
                }
                else if (instruction.Calls(spawnPawn))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, tryAddLord);
                }
            }
        }

        // Proxies that forward to VFEInsectoids' original static helpers via reflection.
        // Signatures must match the originals byte-for-byte so the IL injection is valid.

        public static IEnumerable<PawnKindDef> TryOverrideWildAnimals_Proxy(
            IEnumerable<PawnKindDef> wildAnimals, WildAnimalSpawner spawner)
        {
            EnsureHelpersResolved();
            if (_tryOverrideWildAnimals == null) return wildAnimals;
            try
            {
                return (IEnumerable<PawnKindDef>)_tryOverrideWildAnimals.Invoke(
                    null, new object[] { wildAnimals, spawner });
            }
            catch
            {
                return wildAnimals;
            }
        }

        public static Thing TryAddLordJob_Proxy(Thing thing, WildAnimalSpawner spawner)
        {
            EnsureHelpersResolved();
            if (_tryAddLordJob == null) return thing;
            try
            {
                return (Thing)_tryAddLordJob.Invoke(null, new object[] { thing, spawner });
            }
            catch
            {
                return thing;
            }
        }

        private static void EnsureHelpersResolved()
        {
            if (_helpersResolved) return;
            _helpersResolved = true;

            var patchType = AccessTools.TypeByName(
                "VFEInsectoids.WildAnimalSpawner_SpawnRandomWildAnimalAt_Patch");
            if (patchType == null)
            {
                Log.Warning("[HSK VFE Insectoids Compat] WildAnimalSpawner_SpawnRandomWildAnimalAt_Patch type not found; insect spawn overrides disabled.");
                return;
            }
            _tryOverrideWildAnimals = AccessTools.Method(patchType, "TryOverrideWildAnimals");
            _tryAddLordJob = AccessTools.Method(patchType, "TryAddLordJob");
        }
    }
}
