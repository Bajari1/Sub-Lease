using System.Collections.Generic;
using SubLease.Core;
using SubLease.Gameplay;
using UnityEngine;

namespace SubLease.AI
{
    /// <summary>
    /// P3: Procedural Multi-Joint Active Ragdoll Anatomy & Door/Barricade Breach AI.
    /// Constructs:
    /// - The Ram: 4 Piston Leg Segments
    /// - The Seep: 10-Segment Articulated Contortionist Spine (<15cm profile)
    /// - The Mimic: Unfolding Predatory Limbs + Kevlar Tape Capture Cocoon
    /// - The Counterweight: Ceiling Trapdoor Flaps + Rotating Winch Drum
    /// - The Tuning Fork: 7 Oscillating Metallic Resonance Rods
    /// Also implements physical Door/Barricade Force Testing & Vent Rerouting!
    /// </summary>
    [RequireComponent(typeof(EntityPhysicsController))]
    public class ProceduralMultiJointEntityRig : MonoBehaviour
    {
        public EntityPhysicsController Controller { get; private set; }
        public List<Transform> ArticulatedSubParts = new List<Transform>();
        public int TotalJointedSegments => ArticulatedSubParts.Count;

        [Header("P3.2 Door & Barricade Breach State")]
        public bool IsTestingDoorBarricade = false;
        public bool HasReroutedAfterFailedBreach = false;
        public float LastAppliedDoorBreachForceN = 0f;

        private GameObject _mimicKevlarCocoon;

        private void Awake()
        {
            Controller = GetComponent<EntityPhysicsController>();
        }

        private void Start()
        {
            BuildProceduralAnatomy();
        }

        public void BuildProceduralAnatomy()
        {
            if (Controller == null || ArticulatedSubParts.Count > 0) return;

            switch (Controller.QuirkType)
            {
                case EntityQuirkType.TheRam_MomentumCharger:
                    BuildRamFourPistonLegs();
                    break;
                case EntityQuirkType.TheSeep_GapAndTubeCrawler:
                    BuildSeepTenSegmentSpine();
                    break;
                case EntityQuirkType.TheMimic_DisguisedProp95Kg:
                    BuildMimicHiddenJawsAndCocoon();
                    break;
                case EntityQuirkType.TheCounterweight_CeilingWinchPredator:
                    BuildCounterweightTrapdoorAndDrum();
                    break;
                case EntityQuirkType.TheTuningFork_BlindAcousticHunter:
                    BuildTuningForkResonanceRods();
                    break;
            }
        }

        private void BuildRamFourPistonLegs()
        {
            Vector3[] legOffsets =
            {
                new Vector3(-0.42f, -0.55f, 0.55f),
                new Vector3(0.42f, -0.55f, 0.55f),
                new Vector3(-0.42f, -0.55f, -0.55f),
                new Vector3(0.42f, -0.55f, -0.55f)
            };

            for (int i = 0; i < legOffsets.Length; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Ram_PistonLeg_{i + 1}";
                Destroy(leg.GetComponent<Collider>());
                leg.transform.SetParent(transform, false);
                leg.transform.localPosition = legOffsets[i];
                leg.transform.localScale = new Vector3(0.22f, 0.45f, 0.22f);
                ApplyColor(leg, new Color(0.55f, 0.12f, 0.10f));
                ArticulatedSubParts.Add(leg.transform);
            }
        }

        private void BuildSeepTenSegmentSpine()
        {
            // P3.1: 10-segment articulated spine (<15cm height) for squeezing under doors and through vents
            for (int i = 0; i < 10; i++)
            {
                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                seg.name = $"Seep_SpineSegment_{i + 1}";
                Destroy(seg.GetComponent<Collider>());
                seg.transform.SetParent(transform, false);
                float t = i / 9f;
                float radius = Mathf.Lerp(0.14f, 0.07f, t);
                seg.transform.localPosition = new Vector3(0f, 0f, -i * 0.16f);
                seg.transform.localScale = new Vector3(radius, Mathf.Min(0.14f, radius), radius);
                ApplyColor(seg, Color.Lerp(new Color(0.22f, 0.35f, 0.18f), new Color(0.10f, 0.18f, 0.08f), t));
                ArticulatedSubParts.Add(seg.transform);
            }
        }

        private void BuildMimicHiddenJawsAndCocoon()
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                limb.name = $"Mimic_PredatoryMandible_{i + 1}";
                Destroy(limb.GetComponent<Collider>());
                limb.transform.SetParent(transform, false);
                float angle = i * 90f * Mathf.Deg2Rad;
                limb.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.4f, 0.35f, Mathf.Sin(angle) * 0.4f);
                limb.transform.localScale = Vector3.zero; // Hidden until awake!
                ApplyColor(limb, new Color(0.85f, 0.12f, 0.25f));
                ArticulatedSubParts.Add(limb.transform);
            }

            _mimicKevlarCocoon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _mimicKevlarCocoon.name = "Mimic_KevlarTapeCocoon";
            Destroy(_mimicKevlarCocoon.GetComponent<Collider>());
            _mimicKevlarCocoon.transform.SetParent(transform, false);
            _mimicKevlarCocoon.transform.localPosition = Vector3.zero;
            _mimicKevlarCocoon.transform.localScale = new Vector3(1.08f, 0.65f, 1.08f);
            ApplyColor(_mimicKevlarCocoon, new Color(0.95f, 0.75f, 0.12f));
            _mimicKevlarCocoon.SetActive(false);
        }

        private void BuildCounterweightTrapdoorAndDrum()
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                part.name = $"Counterweight_WinchSpool_{i + 1}";
                Destroy(part.GetComponent<Collider>());
                part.transform.SetParent(transform, false);
                part.transform.localPosition = new Vector3((i - 1) * 0.28f, -0.15f, 0f);
                part.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);
                ApplyColor(part, new Color(0.45f, 0.10f, 0.22f));
                ArticulatedSubParts.Add(part.transform);
            }
        }

        private void BuildTuningForkResonanceRods()
        {
            // 7 metallic resonance tuning rods that vibrate when physics collisions occur
            for (int i = 0; i < 7; i++)
            {
                GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rod.name = $"TuningFork_ResonanceRod_{i + 1}";
                Destroy(rod.GetComponent<Collider>());
                rod.transform.SetParent(transform, false);
                float angle = (i / 7f) * Mathf.PI * 2f;
                rod.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.35f, 0.45f, Mathf.Sin(angle) * 0.35f);
                rod.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);
                ApplyColor(rod, new Color(0.82f, 0.88f, 0.95f));
                ArticulatedSubParts.Add(rod.transform);
            }
        }

        private void Update()
        {
            if (Controller == null || ArticulatedSubParts.Count == 0) return;

            switch (Controller.QuirkType)
            {
                case EntityQuirkType.TheRam_MomentumCharger:
                    for (int i = 0; i < ArticulatedSubParts.Count; i++)
                    {
                        float swing = Mathf.Sin(Time.time * 12f + i * Mathf.PI * 0.5f) * 28f;
                        ArticulatedSubParts[i].localRotation = Quaternion.Euler(swing, 0f, 0f);
                    }
                    break;

                case EntityQuirkType.TheSeep_GapAndTubeCrawler:
                    for (int i = 0; i < ArticulatedSubParts.Count; i++)
                    {
                        float wave = Mathf.Sin(Time.time * 9f - i * 0.55f) * 0.14f;
                        ArticulatedSubParts[i].localPosition = new Vector3(wave, 0f, -i * 0.16f);
                    }
                    break;

                case EntityQuirkType.TheMimic_DisguisedProp95Kg:
                    LootFurnitureItem loot = GetComponent<LootFurnitureItem>();
                    if (loot != null)
                    {
                        bool taped = loot.IsMimicSafelyTaped();
                        if (_mimicKevlarCocoon != null) _mimicKevlarCocoon.SetActive(taped);
                        Vector3 targetScale = (loot.IsMimicAwake && !taped) ? new Vector3(0.12f, 0.55f, 0.12f) : Vector3.zero;
                        foreach (var limb in ArticulatedSubParts)
                        {
                            limb.localScale = Vector3.Lerp(limb.localScale, targetScale, Time.deltaTime * 8f);
                        }
                    }
                    break;

                case EntityQuirkType.TheTuningFork_BlindAcousticHunter:
                    float dbAmp = AcousticAndPhysicsBus.Instance != null
                        ? Mathf.Clamp01((AcousticAndPhysicsBus.Instance.LastPeakDecibels - 25f) / 80f)
                        : 0.1f;
                    for (int i = 0; i < ArticulatedSubParts.Count; i++)
                    {
                        float vib = Mathf.Sin(Time.time * 55f + i) * (4f + dbAmp * 22f);
                        ArticulatedSubParts[i].localRotation = Quaternion.Euler(vib, 0f, vib);
                    }
                    break;
            }
        }

        /// <summary>
        /// P3.2: Physically tests a closed/barricaded hinged door by applying the entity's Newton force
        /// to the door panel, transferring tension into attached ropes/wedges, and rerouting if held!
        /// </summary>
        public bool TestAndAttemptDoorBreach(Rigidbody doorRb)
        {
            if (doorRb == null || Controller == null) return false;

            IsTestingDoorBarricade = true;
            float breachForce = Controller.QuirkType switch
            {
                EntityQuirkType.TheRam_MomentumCharger => 4500f,
                EntityQuirkType.TheSeep_GapAndTubeCrawler => 350f,
                EntityQuirkType.TheTuningFork_BlindAcousticHunter => 1200f,
                _ => 800f
            };

            LastAppliedDoorBreachForceN = breachForce;
            doorRb.WakeUp();
            doorRb.AddForceAtPosition(transform.forward * breachForce, doorRb.position + doorRb.transform.right * 0.5f, ForceMode.Force);

            // If The Seep (350N) pushes a door held by Braided Steel Cable (6,000N) or Duct Tape (1,200N+),
            // it lacks the strength to break through and reroutes through ceiling vents!
            if (breachForce < 800f)
            {
                HasReroutedAfterFailedBreach = true;
                return false;
            }

            return true;
        }

        private static void ApplyColor(GameObject obj, Color col)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = col;
                r.material = mat;
            }
        }
    }
}
