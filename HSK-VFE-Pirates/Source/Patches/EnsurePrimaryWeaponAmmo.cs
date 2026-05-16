// Postfix Combat Extended's loadout generator to guarantee warcasket pawns
// (and any other LoadoutPropertiesExtension user) actually have ammunition
// for the weapon they end up holding.
//
// USER REPORT (JekCool 2026-05-16): Warcasket pirate raids consistently
// spawn pawns with 20mm autocannons and other warcasket weapons but ZERO
// ammunition for them. Some warcasket pawns get 200+ rounds normally, but
// many get nothing. Non-warcasket pawns wielding heavy warcasket weapons
// always spawn ammoless.
//
// ROOT CAUSE: CE's LoadoutPropertiesExtension.GenerateLoadoutFor calls
// TryGenerateAmmoFor(pawn.equipment.Primary, ...) which silently no-ops
// when the weapon's CompAmmoUser has issues, when AmmoSet resolution
// fails, or when bulk/weight budgets fill up before ammo can be added.
// For warcasket-tier pawns with large-mag weapons (20mm Mauser mag 30,
// 25mm NATO mag 10, Minigun mag 500) plus heavy apparel, the budget
// often fills with armor + weapon before ammo gets a chance.
//
// FIX: After CE's loadout completes, check if the pawn has a primary CE
// weapon that uses ammo and verify their inventory contains at least one
// magazine worth of compatible ammo. If not, force-add 2 magazines via
// CompInventory.TryAddItemNotForSale, bypassing the budget check (these
// pawns are spawned for combat - having a usable weapon matters more
// than bulk constraints they'd never have to walk to). Only fires when
// inventory truly has zero rounds, so well-equipped pawns are untouched.

using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKVFEPiratesCompat.Patches
{
    [HarmonyPatch]
    public static class EnsurePrimaryWeaponAmmo
    {
        // Resolved at first use; cached to avoid reflection cost per pawn.
        private static Type tCompAmmoUser;
        private static Type tCompInventory;
        private static Type tAmmoSetDef;
        private static Type tAmmoLink;
        private static PropertyInfo pUseAmmo;
        private static PropertyInfo pCurMagCount;
        private static PropertyInfo pCurrentAmmo;
        private static PropertyInfo pAmmoSet;
        private static PropertyInfo pMagSize;
        private static FieldInfo fAmmo;
        private static FieldInfo fAmmoTypes;
        private static MethodInfo mTryAddItemNotForSale;
        private static MethodInfo mUpdateInventory;
        private static bool reflectionAttempted;
        private static bool reflectionOK;

        static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("CombatExtended.LoadoutPropertiesExtension");
            if (t == null) return null;
            return AccessTools.Method(t, "GenerateLoadoutFor");
        }

        static bool Prepare()
        {
            // Don't register the patch if CE isn't loaded.
            return AccessTools.TypeByName("CombatExtended.LoadoutPropertiesExtension") != null;
        }

        static void Postfix(Pawn pawn)
        {
            if (pawn?.equipment?.Primary == null) return;
            if (!EnsureReflection()) return;

            try
            {
                var primary = pawn.equipment.Primary;
                var ammoUser = primary.AllComps?.FirstOrDefault(c => tCompAmmoUser.IsInstanceOfType(c));
                if (ammoUser == null) return;
                if (!(bool)pUseAmmo.GetValue(ammoUser)) return; // weapon doesn't use ammo

                var inventoryComp = pawn.AllComps?.FirstOrDefault(c => tCompInventory.IsInstanceOfType(c));
                if (inventoryComp == null) return;

                // Determine which AmmoDef to top up with.
                // Prefer the AmmoDef matching CompAmmoUser.CurrentAmmo if set;
                // otherwise pick the first link in the weapon's AmmoSet.
                ThingDef ammoDefToAdd = pCurrentAmmo.GetValue(ammoUser) as ThingDef;
                int magSize = Convert.ToInt32(pMagSize.GetValue(ammoUser));
                if (magSize <= 0) magSize = 30; // fallback

                if (ammoDefToAdd == null)
                {
                    var ammoSet = pAmmoSet.GetValue(ammoUser);
                    if (ammoSet == null) return;
                    var types = fAmmoTypes.GetValue(ammoSet) as System.Collections.IList;
                    if (types == null || types.Count == 0) return;
                    ammoDefToAdd = fAmmo.GetValue(types[0]) as ThingDef;
                }
                if (ammoDefToAdd == null) return;

                // Count existing rounds in inventory.
                int existing = 0;
                var innerContainer = pawn.inventory?.innerContainer;
                if (innerContainer != null)
                {
                    foreach (var thing in innerContainer)
                    {
                        if (thing.def == ammoDefToAdd) existing += thing.stackCount;
                    }
                }

                // Also count what's loaded in the magazine.
                int loaded = Convert.ToInt32(pCurMagCount.GetValue(ammoUser));

                if (existing + loaded > 0) return; // pawn already has SOME ammo - leave alone

                // Top up with 2 magazines worth via direct inventory add.
                int toAdd = magSize * 2;
                var ammoThing = ThingMaker.MakeThing(ammoDefToAdd, null);
                ammoThing.stackCount = toAdd;
                bool added = (bool)mTryAddItemNotForSale.Invoke(inventoryComp, new object[] { ammoThing });
                if (added)
                {
                    mUpdateInventory.Invoke(inventoryComp, null);
                }
            }
            catch (Exception e)
            {
                // Don't crash pawn generation on our account - just log once per session.
                if (!loggedError)
                {
                    loggedError = true;
                    Log.Warning("[HSK VFE Pirates Compat] EnsurePrimaryWeaponAmmo postfix threw, will not retry-log: " + e);
                }
            }
        }

        private static bool loggedError;

        private static bool EnsureReflection()
        {
            if (reflectionAttempted) return reflectionOK;
            reflectionAttempted = true;
            try
            {
                tCompAmmoUser = AccessTools.TypeByName("CombatExtended.CompAmmoUser");
                tCompInventory = AccessTools.TypeByName("CombatExtended.CompInventory");
                tAmmoSetDef = AccessTools.TypeByName("CombatExtended.AmmoSetDef");
                tAmmoLink = AccessTools.TypeByName("CombatExtended.AmmoLink");

                if (tCompAmmoUser == null || tCompInventory == null
                    || tAmmoSetDef == null || tAmmoLink == null)
                    return reflectionOK = false;

                pUseAmmo = AccessTools.Property(tCompAmmoUser, "UseAmmo")
                           ?? AccessTools.Property(tCompAmmoUser, "HasAndUsesAmmoOrMagazine");
                pCurMagCount = AccessTools.Property(tCompAmmoUser, "CurMagCount");
                pCurrentAmmo = AccessTools.Property(tCompAmmoUser, "CurrentAmmo");
                pAmmoSet = AccessTools.Property(tCompAmmoUser, "AmmoSet");
                pMagSize = AccessTools.Property(tCompAmmoUser, "MagSize")
                           ?? AccessTools.Property(tCompAmmoUser, "MagazineSize");

                fAmmoTypes = AccessTools.Field(tAmmoSetDef, "ammoTypes");
                fAmmo = AccessTools.Field(tAmmoLink, "ammo");

                mTryAddItemNotForSale = AccessTools.Method(tCompInventory, "TryAddItemNotForSale")
                                        ?? AccessTools.Method(tCompInventory, "TryAdd")
                                        ?? AccessTools.Method(tCompInventory, "container_TryAdd");
                mUpdateInventory = AccessTools.Method(tCompInventory, "UpdateInventory");

                reflectionOK = pUseAmmo != null && pCurMagCount != null && pAmmoSet != null
                               && pMagSize != null && fAmmoTypes != null && fAmmo != null
                               && mTryAddItemNotForSale != null;
                if (!reflectionOK)
                {
                    Log.Warning("[HSK VFE Pirates Compat] EnsurePrimaryWeaponAmmo could not resolve all CE reflection members - patch is inert.");
                }
                return reflectionOK;
            }
            catch (Exception e)
            {
                Log.Warning("[HSK VFE Pirates Compat] EnsurePrimaryWeaponAmmo reflection setup failed: " + e.Message);
                return reflectionOK = false;
            }
        }
    }
}
