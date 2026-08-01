using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HSKMechShipPower
{
    /// <summary>
    /// Wireless ship power for crashed mechanoid vessels.
    ///
    /// WHY (verified against decompiled Assembly-CSharp, not assumed):
    ///  - PowerConnectionMaker.BestTransmitterForConnector searches ExpandedBy(6), so a
    ///    CONSUMER reaches a transmitter up to 6 tiles away. That part is fine: every
    ///    turret has a hull piece within 1-2 tiles.
    ///  - PowerNetMaker.ContiguousPowerBuildings walks GenAdj.CellsAdjacentCardinal, so
    ///    TRANSMITTERS only join other transmitters they physically touch.
    /// In the generated ship layouts the reactor touches no hull piece, so the ship-wide
    /// net never forms: a flood fill from the reactor reaches only the reactor itself in
    /// every layout, despite 45-60 hull tiles being present. Result: only the handful of
    /// turrets sitting within 6 tiles of the reactor get power and the rest read 0 W
    /// (Arjuna_DK report 2026-07).
    ///
    /// Rather than rely on tile adjacency the layouts do not guarantee, feed consumers
    /// straight from any live reactor in range.
    ///
    /// This comp gives a consumer its power directly from any live reactor within radius,
    /// with no wiring. Destroy the reactors and the turrets go dark, which is the point:
    /// the ship powers itself, and the reactor stays a meaningful target.
    /// </summary>
    public class CompProperties_ShipPowerReceiver : CompProperties
    {
        /// <summary>
        /// How far a reactor can feed this thing, in tiles. Default 24: measured across every
        /// generated ship layout, the farthest turret sits 18 tiles from its reactor
        /// (median 8, p90 13), so 24 covers all of them with headroom while staying well
        /// short of reaching a second ship elsewhere on the map.
        /// </summary>
        public float radius = 24f;

        /// <summary>Def names that count as a ship power source.</summary>
        public List<string> sourceDefNames = new List<string> { "VFE_MechanoidReactor" };

        public CompProperties_ShipPowerReceiver()
        {
            compClass = typeof(CompShipPowerReceiver);
        }
    }

    public class CompShipPowerReceiver : ThingComp
    {
        // Re-scan for a reactor only occasionally (reactors rarely appear or die).
        private const int RescanInterval = 250;

        private CompPowerTrader powerTrader;
        private bool sourceLive;

        private CompProperties_ShipPowerReceiver Props => (CompProperties_ShipPowerReceiver)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerTrader = parent.TryGetComp<CompPowerTrader>();
            Rescan();
            Apply();
        }

        public override void CompTick()
        {
            base.CompTick();

            if ((Find.TickManager.TicksGame + parent.thingIDNumber) % RescanInterval == 0)
            {
                Rescan();
            }

            // Must run EVERY tick, not just on rescan: PowerNet.PowerNetTick assigns
            // PowerOn itself for anything registered on a net, so a turret sitting on its
            // own sourceless net gets switched back off. Re-asserting each tick keeps the
            // reactor's verdict authoritative.
            Apply();
        }

        private void Rescan()
        {
            if (parent.Spawned)
            {
                sourceLive = AnyLiveSourceInRange();
            }
        }

        private void Apply()
        {
            if (powerTrader == null || !parent.Spawned)
            {
                return;
            }

            // Feeding PowerOn directly bypasses the power-net graph, which is the point:
            // the generated layouts never wire the reactor to anything (see class comment).
            // Guard on WantsToBeOn because CompPowerTrader.PowerOn logs a warning when set
            // true on something switched off, and respect an explicit off switch.
            bool wanted = sourceLive && FlickUtility.WantsToBeOn(parent);

            if (powerTrader.PowerOn != wanted)
            {
                powerTrader.PowerOn = wanted;
            }
        }

        private bool AnyLiveSourceInRange()
        {
            Map map = parent.Map;
            if (map == null)
            {
                return false;
            }

            float radiusSq = Props.radius * Props.radius;
            IntVec3 here = parent.Position;

            foreach (string defName in Props.sourceDefNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    continue;
                }

                List<Thing> sources = map.listerThings.ThingsOfDef(def);
                for (int i = 0; i < sources.Count; i++)
                {
                    Thing source = sources[i];
                    if (source == null || source.Destroyed || !source.Spawned)
                    {
                        continue;
                    }

                    if ((source.Position - here).LengthHorizontalSquared > radiusSq)
                    {
                        continue;
                    }

                    // A reactor counts as live unless something explicitly switched it off.
                    CompFlickable flick = source.TryGetComp<CompFlickable>();
                    if (flick != null && !flick.SwitchIsOn)
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        public override string CompInspectStringExtra()
        {
            if (powerTrader == null)
            {
                return null;
            }

            return sourceLive
                ? "HSK_ShipPower_Connected".TranslateSimple()
                : "HSK_ShipPower_NoReactor".TranslateSimple();
        }
    }
}
