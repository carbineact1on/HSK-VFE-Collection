using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKVFEPiratesCompat.Patches
{
    /// <summary>
    /// Forces the HSK_SalvageWarcasketWeapon bill to accept ONLY warcasket
    /// weapons as ingredients.
    ///
    /// The XML-level filter uses <c>&lt;specialFiltersToAllow&gt;</c>, which
    /// in RimWorld controls which toggles the UI shows — it does NOT
    /// actually restrict the ingredient set. As written, the bill would
    /// happily pick up any weapon in the Weapons category, including
    /// vanilla guns, HSK weapons, and modded weapons. That's a footgun —
    /// the player could accidentally dismantle a regular SMG thinking
    /// it's a salvage bill.
    ///
    /// This Harmony postfix on <c>Bill.IsFixedOrAllowedIngredient(ThingDef)</c>
    /// intercepts the check for our specific recipe and hard-overrides
    /// the result: warcasket weapons return true, everything else false.
    /// Applies to both ranged and melee warcasket weapons, including sub-
    /// mod variants (VWENL / VWEQ / VWEC / VWEL / VWEBF / VFEV) via the
    /// shared <see cref="SpecialThingFilterWorker_WarcasketWeapon.IsWarcasketWeapon"/>
    /// helper.
    ///
    /// Non-HSK_SalvageWarcasketWeapon bills pass through unchanged.
    /// </summary>
    [HarmonyPatch(typeof(Bill), "IsFixedOrAllowedIngredient", new[] { typeof(ThingDef) })]
    public static class WeaponSalvageFilter
    {
        private const string RecipeDefName = "HSK_SalvageWarcasketWeapon";

        public static void Postfix(Bill __instance, ThingDef def, ref bool __result)
        {
            if (__instance?.recipe?.defName != RecipeDefName) return;

            // For our salvage bill, the ONLY valid ingredients are warcasket
            // weapons. Hard-override whatever the XML filter said.
            __result = def != null && SpecialThingFilterWorker_WarcasketWeapon.IsWarcasketWeapon(def);
        }
    }
}
