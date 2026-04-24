using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKVFEPiratesCompat.Patches
{
    /// <summary>
    /// Keeps warcasket apparel attached to a corpse during Strip so the
    /// only path to recovering warcasket materials is burning the whole
    /// corpse at the ElectricCrematorium.
    ///
    /// Without this patch, vanilla <c>Pawn_ApparelTracker.DropAll</c> on a
    /// corpse passes <c>dropLocked=true</c> (because <c>base.Destroyed</c>
    /// is true for a dead pawn inside a corpse container), which ignores
    /// the "locked" flag VFE Pirates sets on warcasket pieces. Every piece
    /// gets passed to <c>TryDrop</c> → <c>GenDrop.TryDropSpawn</c>, and
    /// since warcasket defs ship with <c>destroyOnDrop=true</c>, they get
    /// <c>Destroy()</c>'d rather than spawned on the ground. Visual +
    /// data-level disappearance, with nothing the player can recover.
    ///
    /// Fix: Harmony prefix on <c>Pawn_ApparelTracker.TryDrop</c>. When the
    /// apparel being dropped is a warcasket piece (thingClass is
    /// VFEPirates.Apparel_Warcasket OR defName starts VFEP_Warcasket), we
    /// short-circuit the drop — set resultingAp=null, __result=false, and
    /// skip the original method. The apparel stays in the pawn's worn
    /// list and therefore visible on the corpse.
    ///
    /// The Warcasket Foundry's "Remove Warcasket" surgery uses
    /// <c>pawn.apparel.Remove(item)</c>, NOT TryDrop, so this patch
    /// doesn't interfere with voluntary removal of a colonist's own
    /// warcasket — that still destroys pieces + spawns a slag chunk per
    /// VE's upstream behaviour.
    ///
    /// We ALSO keep the destroyOnDrop clear at startup as a belt-and-
    /// suspenders measure: if anything other than TryDrop ever ends up
    /// trying to drop a warcasket piece (e.g. via a Foundry rebuild, a
    /// caravan transfer, a mod interaction), the item will spawn normally
    /// instead of being destroyed silently.
    /// </summary>
    public static class ForceWarcasketDroppable
    {
        public static void Apply(Harmony harmony)
        {
            // Clear destroyOnDrop so any non-strip drop path (Foundry rebuild,
            // caravan transfer, mod hooks) spawns pieces rather than destroying
            // them.
            ClearDestroyOnDropFlags();

            // Prefix the strip drop path so warcasket pieces stay on corpse.
            InstallTryDropPrefix(harmony);
        }

        private static void ClearDestroyOnDropFlags()
        {
            int cleared = 0;
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!def.destroyOnDrop) continue;
                if (!IsOurTarget(def)) continue;

                def.destroyOnDrop = false;
                cleared++;
            }
            if (cleared > 0)
            {
                Log.Message("[HSK VFE Pirates Compat] Cleared destroyOnDrop on " + cleared +
                            " warcasket / Wardrone defs.");
            }
        }

        private static void InstallTryDropPrefix(Harmony harmony)
        {
            try
            {
                // The overload that actually does the work:
                // public bool TryDrop(Apparel ap, out Apparel resultingAp, IntVec3 pos, bool forbid = true)
                var target = AccessTools.Method(
                    typeof(Pawn_ApparelTracker),
                    "TryDrop",
                    new[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) });

                if (target == null)
                {
                    Log.Warning("[HSK VFE Pirates Compat] Could not find Pawn_ApparelTracker.TryDrop(Apparel, out Apparel, IntVec3, bool); strip-skip disabled.");
                    return;
                }

                var prefix = AccessTools.Method(typeof(ForceWarcasketDroppable), nameof(TryDrop_Prefix));
                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Log.Message("[HSK VFE Pirates Compat] Installed strip-skip prefix on Pawn_ApparelTracker.TryDrop — warcasket pieces remain attached to the corpse for Cremator salvage.");
            }
            catch (Exception e)
            {
                Log.Warning("[HSK VFE Pirates Compat] Strip-skip prefix install failed: " + e);
            }
        }

        /// <summary>
        /// Harmony prefix on <c>Pawn_ApparelTracker.TryDrop(Apparel, out Apparel, IntVec3, bool)</c>.
        /// Skips the drop entirely for warcasket pieces so they stay
        /// attached to the wearer (typically a corpse being stripped).
        /// </summary>
        public static bool TryDrop_Prefix(Apparel ap, out Apparel resultingAp, ref bool __result)
        {
            if (ap?.def != null && SpecialThingFilterWorker_WarcasketCorpse.IsWarcasketApparelDef(ap.def))
            {
                resultingAp = null;
                __result = false;
                return false; // skip original — apparel stays in wornApparel
            }
            resultingAp = null;
            return true; // fall through to vanilla for non-warcasket
        }

        private static bool IsOurTarget(ThingDef def)
        {
            if (SpecialThingFilterWorker_WarcasketCorpse.IsWarcasketApparelDef(def)) return true;
            if (def.defName == "VFEPGun_MiniBlaster") return true;
            return false;
        }
    }
}
