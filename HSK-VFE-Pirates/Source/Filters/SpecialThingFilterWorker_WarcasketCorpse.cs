using System.Linq;
using RimWorld;
using Verse;

namespace HSKVFEPiratesCompat
{
    /// <summary>
    /// SpecialThingFilter worker: matches humanlike corpses whose InnerPawn
    /// was wearing at least one VFE Pirates warcasket apparel piece at the
    /// time of death.
    ///
    /// Used by the "Salvage warcasket from corpse" recipe on the Warcasket
    /// Foundry so the bill's ingredient search only picks up corpses that
    /// actually have warcasket armor to recycle — prevents players from
    /// processing unarmored raider corpses for free spacer materials.
    /// </summary>
    public class SpecialThingFilterWorker_WarcasketCorpse : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            if (!(t is Corpse corpse)) return false;
            var pawn = corpse.InnerPawn;
            if (pawn?.apparel == null) return false;
            return pawn.apparel.WornApparel.Any(IsWarcasketApparel);
        }

        public override bool CanEverMatch(ThingDef def)
        {
            // Accept anything corpse-shaped; per-instance check in Matches() does the real work.
            return def != null && typeof(Corpse).IsAssignableFrom(def.thingClass);
        }

        /// <summary>
        /// Recognises a warcasket apparel piece by its thingClass
        /// (<c>VFEPirates.Apparel_Warcasket</c>) or its defName prefix as a
        /// fallback for any VE sub-mods (NL/Q/C/L/BF/V) that extend the
        /// warcasket roster.
        /// </summary>
        public static bool IsWarcasketApparel(Apparel apparel)
        {
            return apparel != null && IsWarcasketApparelDef(apparel.def);
        }

        /// <summary>
        /// ThingDef-level overload for use in Harmony patches that only have
        /// a ThingDef reference (e.g. ApparelUtility.HasPartsToWear).
        /// </summary>
        public static bool IsWarcasketApparelDef(ThingDef def)
        {
            if (def == null) return false;
            var tc = def.thingClass;
            if (tc != null && tc.FullName == "VFEPirates.Apparel_Warcasket") return true;

            var defName = def.defName;
            if (string.IsNullOrEmpty(defName)) return false;
            // Body plates, helmets, shoulders, bodysuit — all canonical VFE Pirates pieces.
            return defName.StartsWith("VFEP_Warcasket", System.StringComparison.Ordinal);
        }
    }
}
