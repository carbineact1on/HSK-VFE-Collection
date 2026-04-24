using System.Collections.Generic;
using Verse;

namespace HSKVFEPiratesCompat.Patches
{
    /// <summary>
    /// Ensures the <c>VFEP_RemoveWarcasket</c> surgery shows up on the
    /// Health → Operations tab for ALL humanlike races, not just vanilla
    /// Human.
    ///
    /// Upstream VFE Pirates ships a PatchOperationAdd that inserts the
    /// recipe into <c>Defs/ThingDef[defName="Human"]/recipes</c> only.
    /// Custom races (Orassan, Nova, Dova, Norbal, alien races, VFE races,
    /// RJW races, etc.) never get the recipe attached, so colonists of
    /// those races can't have their warcasket surgically removed even
    /// though they can wear one.
    ///
    /// This pass runs after def load and appends <c>VFEP_RemoveWarcasket</c>
    /// to the recipes list of every humanlike ThingDef that doesn't
    /// already have it, creating a new <c>recipes</c> list if one didn't
    /// exist. Automatically covers any humanlike race present in the user's
    /// modlist without needing a per-mod XML compat patch.
    /// </summary>
    public static class UniversalRemoveWarcasket
    {
        private const string RemoveWarcasketRecipeDefName = "VFEP_RemoveWarcasket";

        public static void Apply()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(RemoveWarcasketRecipeDefName);
            if (recipe == null)
            {
                Log.Warning("[HSK VFE Pirates Compat] VFEP_RemoveWarcasket RecipeDef not found; universal surgery patch skipped.");
                return;
            }

            int added = 0;
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def?.race == null || !def.race.Humanlike) continue;

                if (def.recipes == null)
                {
                    def.recipes = new List<RecipeDef>();
                }
                if (def.recipes.Contains(recipe)) continue;

                def.recipes.Add(recipe);
                added++;
            }

            // The recipe itself needs to know the race as a user, too. RimWorld
            // caches a reverse lookup (ThingDef <-> recipes) at game start; we
            // already handled the ThingDef side. Some recipes also maintain a
            // recipeUsers list that's consulted independently, so mirror here
            // in case it matters for UI / WorkGiver reservations.
            if (recipe.recipeUsers == null) recipe.recipeUsers = new List<ThingDef>();
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def?.race == null || !def.race.Humanlike) continue;
                if (!recipe.recipeUsers.Contains(def)) recipe.recipeUsers.Add(def);
            }

            if (added > 0)
            {
                Log.Message("[HSK VFE Pirates Compat] Added VFEP_RemoveWarcasket surgery to " + added +
                            " humanlike race(s) — any race wearing a warcasket can now have it surgically removed.");
            }
        }
    }
}
