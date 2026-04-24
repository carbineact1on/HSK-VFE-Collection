using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKVFEPiratesCompat.Patches
{
    /// <summary>
    /// Restricts who can wear VFE Pirates warcasket apparel.
    ///
    /// Upstream design: warcasket pieces can only be put onto a colonist
    /// by welding them in at the Warcasket Foundry — the welding job
    /// grants the pawn the <c>VFEP_WarcasketTrait</c> trait and fuses the
    /// apparel to them. VE enforced this indirectly by blocking all strip
    /// actions on warcasket-wearing pawns, so pieces never reached player
    /// hands in the first place.
    ///
    /// Our HSK patches removed that strip block and set destroyOnDrop=false
    /// so pieces become haulable loot. But without additional gating, any
    /// colonist could now just walk over and equip a looted warcasket
    /// helmet directly — bypassing the Foundry entirely, which breaks VE's
    /// intended progression.
    ///
    /// Fix: Harmony-prefix <see cref="ApparelUtility.HasPartsToWear"/> to
    /// return false when a pawn lacking <c>VFEP_WarcasketTrait</c> tries
    /// to wear a warcasket apparel piece. This is the standard gate
    /// RimWorld consults in every wear-check flow (outfit auto-assign,
    /// bill products, force-equip via gear tab, strip-then-equip), so
    /// setting __result=false here uniformly blocks direct wear while
    /// still letting trait-holding pawns (foundry-installed ones) wear
    /// them normally.
    ///
    /// Salvaging looted pieces at TableMachining via HSK_SalvageWarcasketCorpse
    /// remains the way for non-trait colonists to get value out of them.
    /// </summary>
    public static class RestrictWarcasketWear
    {
        private static TraitDef _warcasketTrait;
        private static bool _warcasketTraitResolved;

        public static void Apply(Harmony harmony)
        {
            try
            {
                var target = AccessTools.Method(typeof(ApparelUtility), "HasPartsToWear");
                if (target == null)
                {
                    Log.Warning("[HSK VFE Pirates Compat] Could not find ApparelUtility.HasPartsToWear; wear restriction skipped.");
                    return;
                }

                var prefix = AccessTools.Method(typeof(RestrictWarcasketWear), nameof(Prefix));
                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Log.Message("[HSK VFE Pirates Compat] Applied warcasket wear restriction — only VFEP_WarcasketTrait pawns may wear warcasket apparel.");
            }
            catch (Exception e)
            {
                Log.Warning("[HSK VFE Pirates Compat] Wear restriction failed: " + e);
            }
        }

        public static bool Prefix(Pawn p, ThingDef apparel, ref bool __result)
        {
            // Only gate warcasket apparel — everything else passes through.
            if (!SpecialThingFilterWorker_WarcasketCorpse.IsWarcasketApparelDef(apparel))
            {
                return true;
            }
            if (p?.story?.traits == null)
            {
                return true;
            }
            if (!_warcasketTraitResolved)
            {
                _warcasketTraitResolved = true;
                _warcasketTrait = DefDatabase<TraitDef>.GetNamedSilentFail("VFEP_WarcasketTrait");
                if (_warcasketTrait == null)
                {
                    Log.Warning("[HSK VFE Pirates Compat] VFEP_WarcasketTrait not found; wear restriction disabled.");
                }
            }
            if (_warcasketTrait == null) return true;

            if (p.story.traits.HasTrait(_warcasketTrait))
            {
                return true;
            }
            __result = false;
            return false;
        }
    }
}
