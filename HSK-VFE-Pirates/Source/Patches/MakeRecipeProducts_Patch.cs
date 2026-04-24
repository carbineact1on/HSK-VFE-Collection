using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKVFEPiratesCompat.Patches
{
    /// <summary>
    /// Harmony postfix on <see cref="GenRecipe.MakeRecipeProducts"/>.
    ///
    /// Adds bonus yield when the standard HSK cremation recipes process a
    /// warcasket-wearing corpse, and when the TableMachining salvage
    /// recipe processes a warcasket weapon. In both cases we read the
    /// relevant <see cref="ThingDef.costList"/> BEFORE ConsumeIngredients
    /// destroys the thing — the RecipeWorker.Notify_IterationCompleted
    /// hook runs too late (after destroy) and sees empty data.
    ///
    /// Triggers on these RecipeDefs:
    ///   * CremateCorpse           (HSK ElectricCrematorium)
    ///   * CremateCorpseCampfire   (HSK primitive cremator)
    ///   * HSK_SalvageWarcasketWeapon (TableMachining)
    ///
    /// Cremation: for every corpse ingredient whose InnerPawn still wears
    /// warcasket apparel, yields each piece's full costList scaled by the
    /// apparel's HP ratio. Output is layered ON TOP of the vanilla ashes,
    /// so the player gets both the usual cremation result AND whatever
    /// metals are left in the warcasket. Non-warcasket corpses skip this
    /// branch entirely — normal cremation unchanged.
    ///
    /// Yield formula: (costList entry) × (HitPoints / MaxHitPoints),
    /// rounded UP. A pristine piece refunds 100% of its craft cost. A
    /// half-damaged piece refunds 50%. A nearly-destroyed piece still
    /// returns at least 1 of each material with non-zero cost because of
    /// the round-up.
    /// </summary>
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class MakeRecipeProducts_Patch
    {
        private const string CremateCorpseDefName = "CremateCorpse";
        private const string CremateCorpseCampfireDefName = "CremateCorpseCampfire";
        private const string WeaponRecipeDefName = "HSK_SalvageWarcasketWeapon";
        // Yield scales purely with HP ratio — pristine piece = 100% refund,
        // half-damaged = 50%, etc. (costList entry × hp / maxHp, rounded up).
        private const float BaseYieldFraction = 1f;

        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result,
                                                 RecipeDef recipeDef,
                                                 Pawn worker,
                                                 List<Thing> ingredients,
                                                 Thing dominantIngredient,
                                                 IBillGiver billGiver)
        {
            // Pass through vanilla products first.
            foreach (var t in __result)
            {
                yield return t;
            }

            if (recipeDef == null || ingredients == null) yield break;

            var name = recipeDef.defName;
            if (name == CremateCorpseDefName || name == CremateCorpseCampfireDefName)
            {
                foreach (var product in BuildCorpseYield(ingredients))
                {
                    yield return product;
                }
            }
            else if (name == WeaponRecipeDefName)
            {
                foreach (var product in BuildWeaponYield(ingredients))
                {
                    yield return product;
                }
            }
        }

        /// <summary>
        /// Scans each ingredient corpse for worn warcasket apparel and
        /// emits 50% × HP ratio of each piece's costList.
        /// Non-warcasket corpses contribute nothing — they cremate as
        /// normal with just the vanilla ashes output.
        /// </summary>
        private static IEnumerable<Thing> BuildCorpseYield(List<Thing> ingredients)
        {
            var yields = new Dictionary<ThingDef, int>();

            foreach (var thing in ingredients)
            {
                if (!(thing is Corpse corpse)) continue;
                var pawn = corpse.InnerPawn;
                if (pawn?.apparel == null) continue;

                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    if (apparel?.def == null) continue;
                    if (!SpecialThingFilterWorker_WarcasketCorpse.IsWarcasketApparelDef(apparel.def)) continue;
                    if (apparel.def.costList == null) continue;

                    float hpRatio = CalcHpRatio(apparel);
                    AccumulateCost(apparel.def.costList, hpRatio, yields);
                }
            }

            return EmitStacks(yields);
        }

        private static IEnumerable<Thing> BuildWeaponYield(List<Thing> ingredients)
        {
            var yields = new Dictionary<ThingDef, int>();

            foreach (var thing in ingredients)
            {
                if (thing?.def == null) continue;
                if (!SpecialThingFilterWorker_WarcasketWeapon.IsWarcasketWeapon(thing.def)) continue;
                if (thing.def.costList == null) continue;

                float hpRatio = CalcHpRatio(thing);
                AccumulateCost(thing.def.costList, hpRatio, yields);
            }

            return EmitStacks(yields);
        }

        private static float CalcHpRatio(Thing thing)
        {
            if (thing == null || !thing.def.useHitPoints || thing.MaxHitPoints <= 0) return 1f;
            float ratio = (float)thing.HitPoints / thing.MaxHitPoints;
            if (ratio < 0f) return 0f;
            if (ratio > 1f) return 1f;
            return ratio;
        }

        private static void AccumulateCost(List<ThingDefCountClass> costList,
                                           float hpRatio,
                                           Dictionary<ThingDef, int> yields)
        {
            foreach (var cost in costList)
            {
                if (cost?.thingDef == null || cost.count <= 0) continue;

                float raw = cost.count * BaseYieldFraction * hpRatio;
                int refund = Mathf.CeilToInt(raw);
                if (refund <= 0) continue;

                if (yields.TryGetValue(cost.thingDef, out var existing))
                    yields[cost.thingDef] = existing + refund;
                else
                    yields[cost.thingDef] = refund;
            }
        }

        private static IEnumerable<Thing> EmitStacks(Dictionary<ThingDef, int> yields)
        {
            foreach (var kvp in yields)
            {
                var def = kvp.Key;
                int remaining = kvp.Value;
                if (def == null || remaining <= 0) continue;

                int stackLimit = def.stackLimit > 0 ? def.stackLimit : remaining;
                while (remaining > 0)
                {
                    int chunk = System.Math.Min(remaining, stackLimit);
                    var stack = ThingMaker.MakeThing(def);
                    stack.stackCount = chunk;
                    yield return stack;
                    remaining -= chunk;
                }
            }
        }
    }

    internal static class Mathf
    {
        public static int CeilToInt(float value) => (int)System.Math.Ceiling(value);
    }
}
