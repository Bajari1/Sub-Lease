using System.Collections.Generic;
using SubLease.Core;
using SubLease.Gameplay;
using SubLease.Networking;
using SubLease.PhysicsSystems;
using UnityEngine;

namespace SubLease.World
{
    public enum ApexBossType
    {
        Apex1_ThePendulum_BrutalistArchive,
        Apex2_TheUndertow_DeepPoolrooms,
        Apex3_TheCentrifuge_HvacTurbine
    }

    /// <summary>
    /// P4: Controls the 3 Apex Shaft Bosses (The Pendulum, The Undertow, The Centrifuge)
    /// and evaluates their systemic physics defeat conditions.
    /// </summary>
    public class ApexBossController : MonoBehaviour
    {
        public ApexBossType BossType;
        public bool IsBossDefeated = false;
        public string DefeatReason = "";
        public Transform LoadBearingPillarTarget;

        private Vector3 _initialPos;

        private void Start()
        {
            _initialPos = transform.position;
        }

        private void Update()
        {
            if (IsBossDefeated) return;

            if (BossType == Apex1_ThePendulum_BrutalistArchive)
            {
                // Swings like a colossal metronome across the -38m Brutalist Archive shaft
                float arc = Mathf.Sin(Time.time * 2.4f) * 4.5f;
                transform.position = _initialPos + new Vector3(arc, Mathf.Abs(arc) * 0.25f, 0f);
            }
        }

        public bool TriggerPendulumColumnCollapse(RopeGrade cableGrade)
        {
            if (BossType != ApexBossType.Apex1_ThePendulum_BrutalistArchive) return false;
            if (cableGrade != RopeGrade.BraidedSteel_6000N) return false;

            IsBossDefeated = true;
            DefeatReason = "Tethered at peak swing (v=0) with 6,000N Braided Steel Cable to cracked load-bearing column; downward m*g*h collapsed atrium roof!";
            if (LoadBearingPillarTarget != null)
            {
                LoadBearingPillarTarget.localRotation = Quaternion.Euler(35f, 0f, 25f);
            }
            Debug.Log($"[ApexBoss1_ThePendulum] DEFEATED! {DefeatReason}");
            return true;
        }

        public bool TriggerUndertowBuoyancyBomb(int attachedLiftBags)
        {
            if (BossType != ApexBossType.Apex2_TheUndertow_DeepPoolrooms) return false;
            if (attachedLiftBags < 3) return false;

            IsBossDefeated = true;
            DefeatReason = $"Snagged cabinet rigged with {attachedLiftBags}x CO2 Lift Bags (+2,796N upward buoyancy); rocketed into high-voltage ceiling grid!";
            transform.position += Vector3.up * 6.5f;
            Debug.Log($"[ApexBoss2_TheUndertow] DEFEATED! {DefeatReason}");
            return true;
        }

        public bool TriggerCentrifugeTurbineJam(Rigidbody wheeledRamLocker)
        {
            if (BossType != ApexBossType.Apex3_TheCentrifuge_HvacTurbine || wheeledRamLocker == null) return false;
            NetworkedPhysicsObject net = wheeledRamLocker.GetComponent<NetworkedPhysicsObject>();
            if (net == null || !net.HasCasterWheelsAttached) return false;

            IsBossDefeated = true;
            DefeatReason = "Caster-wheeled steel locker launched into central intake rotor during suction phase; turbine jammed permanently!";
            Debug.Log($"[ApexBoss3_TheCentrifuge] DEFEATED! {DefeatReason}");
            return true;
        }

        private const ApexBossType Apex1_ThePendulum_BrutalistArchive = ApexBossType.Apex1_ThePendulum_BrutalistArchive;
    }

    /// <summary>
    /// P4 Sector 3 (-250m / -56m slice): Archimedes Buoyancy Zone (F_b = rho * V * g).
    /// Wooden/light furniture floats up flooded shafts automatically; heavy marble sinks unless rigged with CO2 Lift Bags!
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ArchimedesBuoyancyZone : MonoBehaviour
    {
        public float WaterDensityEquivalentKgThreshold = 60f;
        public float UpwardBuoyantAcceleration = 15.5f;
        public int FloatingObjectsCount { get; private set; }

        private readonly List<Rigidbody> _submergedBodies = new List<Rigidbody>();

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic && !_submergedBodies.Contains(rb))
            {
                _submergedBodies.Add(rb);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null) _submergedBodies.Remove(rb);
        }

        private void FixedUpdate()
        {
            FloatingObjectsCount = 0;
            for (int i = _submergedBodies.Count - 1; i >= 0; i--)
            {
                Rigidbody rb = _submergedBodies[i];
                if (rb == null)
                {
                    _submergedBodies.RemoveAt(i);
                    continue;
                }

                // Objects lighter than threshold (or with CO2 Lift Bags) experience net positive upward Archimedes buoyancy!
                bool hasLiftBag = rb.GetComponent<InflatableLiftBag>() != null;
                if (rb.mass <= WaterDensityEquivalentKgThreshold || hasLiftBag)
                {
                    rb.AddForce(Vector3.up * (rb.mass * UpwardBuoyantAcceleration), ForceMode.Force);
                    FloatingObjectsCount++;
                }
            }
        }
    }

    /// <summary>
    /// P4 Sector 4: Tilted Hotel Gimbal Gravity Zone & HVAC Aerodynamic Updraft Wind Chute.
    /// </summary>
    public class TiltedHotelAndWindChuteZone : MonoBehaviour
    {
        public float CurrentGimbalTiltDegrees = 0f;
        public float UpdraftWindVelocityMps = 22f;
        public bool IsUpdraftActive = true;

        public void ApplyGimbalTiltAndUpdraftToProp(Rigidbody propRb, float tiltAngleDeg = 45f)
        {
            if (propRb == null) return;
            CurrentGimbalTiltDegrees = tiltAngleDeg;

            // Apply 45-degree sideways gimbal gravity vector + aerodynamic updraft lift
            Vector3 tiltedGravity = Quaternion.Euler(0f, 0f, tiltAngleDeg) * Physics.gravity;
            propRb.WakeUp();
            propRb.AddForce(tiltedGravity * propRb.mass, ForceMode.Force);

            if (IsUpdraftActive)
            {
                // Aerodynamic updraft F_d = 0.5 * rho * v^2 * C_d * A
                float updraftForceN = 0.5f * 1.225f * (UpdraftWindVelocityMps * UpdraftWindVelocityMps) * 2.8f;
                propRb.AddForce(Vector3.up * updraftForceN, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// P4 Procedural Builder for Sector 2 (Brutalist Archive), Sector 3 (Deep Poolrooms),
    /// Sector 4 (Tilted Hotel & HVAC Chutes), all 3 Apex Shaft Bosses, and remaining Tier 3/Tier 4 Anomalous Loot.
    /// </summary>
    public static class DeepMacroPhysicsSectorBuilder
    {
        public static void BuildDeepSectorsAndApexBosses(Transform worldRoot)
        {
            // 1. Sector 2: The Brutalist Archive (-38m) + Apex 1: The Pendulum + Crystal Chandelier & Polybius Cabinet
            GameObject sec2 = new GameObject("Sector2_BrutalistArchive_-38m");
            sec2.transform.SetParent(worldRoot, false);
            BuildFloorSlab(sec2.transform, new Vector3(0f, -38f, 6f), new Vector3(24f, 0.5f, 24f), new Color(0.32f, 0.33f, 0.35f));

            GameObject pillar = BuildFloorSlab(sec2.transform, new Vector3(-3.5f, -35f, 8f), new Vector3(0.8f, 5.5f, 0.8f), new Color(0.45f, 0.45f, 0.48f));
            pillar.name = "Cracked_LoadBearing_Pillar";
            pillar.AddComponent<TetherAnchor>().AnchorLabel = "Cracked Load-Bearing Column";

            GameObject pendulumObj = BuildFloorSlab(sec2.transform, new Vector3(0f, -33.5f, 8f), new Vector3(1.8f, 1.8f, 0.8f), new Color(0.75f, 0.58f, 0.20f));
            pendulumObj.name = "ApexBoss1_ThePendulum";
            ApexBossController boss1 = pendulumObj.AddComponent<ApexBossController>();
            boss1.BossType = ApexBossType.Apex1_ThePendulum_BrutalistArchive;
            boss1.LoadBearingPillarTarget = pillar.transform;

            SpawnDeepLoot(sec2.transform, "Venetian Crystal Chandelier", new Vector3(2.5f, -34.0f, 10f), 3500, 70f, 2.5f, new Color(0.85f, 0.95f, 1.0f));
            SpawnDeepLoot(sec2.transform, "Retro Polybius Arcade Cabinet", new Vector3(-2.5f, -37.0f, 10f), 2900, 135f, 1.4f, new Color(0.15f, 0.12f, 0.35f));

            // 2. Sector 3: The Deep Poolrooms (-56m) + Archimedes Buoyancy Volume + Apex 2: The Undertow + Baroque Mirror
            GameObject sec3 = new GameObject("Sector3_DeepPoolrooms_-56m");
            sec3.transform.SetParent(worldRoot, false);
            BuildFloorSlab(sec3.transform, new Vector3(0f, -56f, 6f), new Vector3(24f, 0.5f, 24f), new Color(0.85f, 0.94f, 0.96f));

            GameObject poolWater = BuildFloorSlab(sec3.transform, new Vector3(0f, -54.2f, 6f), new Vector3(20f, 3.0f, 20f), new Color(0.15f, 0.65f, 0.85f));
            poolWater.name = "Poolrooms_ArchimedesBuoyancyVolume";
            BoxCollider waterCol = poolWater.GetComponent<BoxCollider>();
            waterCol.isTrigger = true;
            poolWater.AddComponent<ArchimedesBuoyancyZone>();

            GameObject undertowObj = BuildFloorSlab(sec3.transform, new Vector3(0f, -55.2f, 8f), new Vector3(2.2f, 0.6f, 2.2f), new Color(0.08f, 0.25f, 0.45f));
            undertowObj.name = "ApexBoss2_TheUndertow";
            ApexBossController boss2 = undertowObj.AddComponent<ApexBossController>();
            boss2.BossType = ApexBossType.Apex2_TheUndertow_DeepPoolrooms;

            SpawnDeepLoot(sec3.transform, "Gilded Baroque Mirror", new Vector3(1.5f, -54.5f, 8f), 3200, 45f, 2.2f, new Color(0.95f, 0.82f, 0.25f));

            // 3. Sector 4: The Tilted Hotel & HVAC Wind Chutes (-74m) + Apex 3: The Centrifuge + Tier 4 Anomalous Loot
            GameObject sec4 = new GameObject("Sector4_TiltedHotel_And_HVAC_-74m");
            sec4.transform.SetParent(worldRoot, false);
            BuildFloorSlab(sec4.transform, new Vector3(0f, -74f, 6f), new Vector3(24f, 0.5f, 24f), new Color(0.48f, 0.18f, 0.18f));
            sec4.AddComponent<TiltedHotelAndWindChuteZone>();

            GameObject centrifugeObj = BuildFloorSlab(sec4.transform, new Vector3(0f, -72.0f, 12f), new Vector3(2.6f, 2.6f, 1.2f), new Color(0.45f, 0.50f, 0.55f));
            centrifugeObj.name = "ApexBoss3_TheCentrifuge";
            ApexBossController boss3 = centrifugeObj.AddComponent<ApexBossController>();
            boss3.BossType = ApexBossType.Apex3_TheCentrifuge_HvacTurbine;

            SpawnDeepLoot(sec4.transform, "Gyroscopic Brass Armillary", new Vector3(-3.0f, -72.8f, 9f), 7200, 80f, 1.2f, new Color(0.88f, 0.68f, 0.22f));
            SpawnDeepLoot(sec4.transform, "Superconducting Server Monolith", new Vector3(3.0f, -72.5f, 9f), 9000, 160f, 0.5f, new Color(0.12f, 0.15f, 0.22f));
        }

        private static GameObject BuildFloorSlab(Transform parent, Vector3 pos, Vector3 scale, Color col)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(parent, false);
            box.transform.position = pos;
            box.transform.localScale = scale;
            Renderer r = box.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = col;
                r.material = mat;
            }
            return box;
        }

        private static GameObject SpawnDeepLoot(Transform parent, string name, Vector3 pos, int dollarVal, float massKg, float fragility, Color col)
        {
            GameObject obj = BuildFloorSlab(parent, pos, new Vector3(0.85f, 1.1f, 0.85f), col);
            obj.name = $"Loot_{name}";
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = massKg;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            NetworkedPhysicsObject net = obj.AddComponent<NetworkedPhysicsObject>();
            net.TrueSignedMassKg = massKg;
            obj.AddComponent<TetherAnchor>().AnchorLabel = name;

            LootFurnitureItem loot = obj.AddComponent<LootFurnitureItem>();
            loot.ItemDisplayName = name;
            loot.BaseDollarValue = dollarVal;
            loot.CurrentDollarValue = dollarVal;
            loot.FragilityMultiplier = fragility;
            loot.UpdatePriceTagText();
            return obj;
        }
    }
}
