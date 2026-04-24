using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKVFEPiratesCompat
{
    /// <summary>
    /// Recipe worker for the "Salvage warcasket weapon" bill on the
    /// TableMachining spacer workbench.
    ///
    /// Dynamic yield is produced by the Harmony postfix on
    /// <see cref="GenRecipe.MakeRecipeProducts"/> (see
    /// Patches/MakeRecipeProducts_Patch.cs). That path runs BEFORE
    /// ConsumeIngredients destroys the weapon, so we can read both
    /// <see cref="Thing.HitPoints"/> (for damage scaling) and
    /// <see cref="ThingDef.costList"/>.
    ///
    /// This worker stays attached via RecipeDef.workerClass so the recipe
    /// has a binding target; its methods are intentionally empty.
    /// </summary>
    public class RecipeWorker_SalvageWarcasketWeapon : RecipeWorker
    {
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
        }
    }
}
