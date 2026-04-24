using System.Linq;
using RimWorld;
using Verse;

namespace HSKVFEPiratesCompat
{
    /// <summary>
    /// SpecialThingFilter worker: matches VFE Pirates warcasket weapons
    /// (both ranged and melee) so the "Salvage warcasket weapon" Foundry
    /// bill only picks up valid weapons, not every gun on the map.
    ///
    /// Identification strategy (any of):
    ///   1. Weapon has weaponTag "WarcasketAll" or any tag starting with
    ///      "Warcasket" (covers base + all VE sub-mods' extensions).
    ///   2. Weapon has tradeTag "VFEP_WarcasketWeapon".
    ///   3. Weapon def name starts with a known warcasket prefix.
    ///
    /// Kept loose intentionally: if a sub-mod ships new warcasket weapons
    /// we'd rather accept them than reject and strand the player with
    /// unrecyclable loot.
    /// </summary>
    public class SpecialThingFilterWorker_WarcasketWeapon : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return t?.def != null && IsWarcasketWeapon(t.def);
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return IsWarcasketWeapon(def);
        }

        /// <summary>
        /// Recognises a warcasket weapon by weaponTags, tradeTags, or defName.
        /// </summary>
        public static bool IsWarcasketWeapon(ThingDef def)
        {
            if (def == null) return false;

            // Tag-based checks (fastest, most reliable).
            if (def.weaponTags != null)
            {
                foreach (var tag in def.weaponTags)
                {
                    if (tag != null && tag.StartsWith("Warcasket", System.StringComparison.Ordinal))
                        return true;
                }
            }
            if (def.tradeTags != null)
            {
                foreach (var tag in def.tradeTags)
                {
                    if (tag == "VFEP_WarcasketWeapon") return true;
                }
            }

            // DefName prefix fallback (catches any VE sub-mod pieces that
            // forgot to set tags). Guns use VFEP_WarcasketGun_*, VFEP_Warcasket*,
            // plus melee variants.
            var name = def.defName;
            return name != null &&
                (name.StartsWith("VFEP_WarcasketGun", System.StringComparison.Ordinal) ||
                 name.StartsWith("VFEP_WarcasketMelee", System.StringComparison.Ordinal));
        }
    }
}
