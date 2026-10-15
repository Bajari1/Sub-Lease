using SubLease.AI;
using SubLease.Core;
using SubLease.Gameplay;
using SubLease.Networking;
using UnityEngine;

namespace SubLease.PhysicsSystems
{
    /// <summary>
    /// Item F3: Walkie-Talkie Pair.
    /// Relays player voice or triggered audio from Walkie A to Walkie B's exact 3D world position,
    /// allowing players to lure The Tuning Fork and The Ram into a remote room from 100m away!
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class WalkieTalkieGadget : MonoBehaviour
    {
        public string ChannelCode = "CH-1";
        public WalkieTalkieGadget LinkedPartnerWalkie;
        public float LastRelayedDecibels;

        public void TransmitAudioToPartner(float decibels)
        {
            if (LinkedPartnerWalkie == null) return;
            LinkedPartnerWalkie.ReceiveRelayedAudio(decibels);
        }

        public void ReceiveRelayedAudio(float decibels)
        {
            LastRelayedDecibels = decibels;
            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(transform.position, decibels, SoundSourceType.WalkieTalkieRelay, gameObject);
            }
            Debug.Log($"[WalkieTalkie {name}] Emitted relayed acoustic lure ({decibels:F0} dB) at {transform.position}!");
        }
    }

    /// <summary>
    /// Item F1: Wind-Up Kitchen Timer (0-60s delayed 95 dB acoustic lure).
    /// Can be duct-taped to a rolling chair or RC Chassis!
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class KitchenTimerGadget : MonoBehaviour
    {
        public float RemainingSeconds = 0f;
        public bool IsWoundUp = false;
        public bool IsRinging = false;
        private float _ringDurationRemaining = 0f;
        private float _nextRingPulse;

        public void WindUpTimer(float seconds)
        {
            RemainingSeconds = Mathf.Clamp(seconds, 1f, 60f);
            IsWoundUp = true;
            IsRinging = false;
        }

        private void Update()
        {
            if (IsWoundUp && !IsRinging)
            {
                RemainingSeconds -= Time.deltaTime;
                if (RemainingSeconds <= 0f)
                {
                    IsWoundUp = false;
                    IsRinging = true;
                    _ringDurationRemaining = 10f;
                    TriggerRingPulse();
                }
            }
            else if (IsRinging)
            {
                _ringDurationRemaining -= Time.deltaTime;
                if (Time.time >= _nextRingPulse)
                {
                    TriggerRingPulse();
                }
                if (_ringDurationRemaining <= 0f)
                {
                    IsRinging = false;
                }
            }
        }

        public void TriggerRingPulse()
        {
            _nextRingPulse = Time.time + 0.8f;
            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(transform.position, 95f, SoundSourceType.GadgetTimerOrHorn, gameObject);
            }
        }
    }

    /// <summary>
    /// Item C5: Strobe & Siren Decoy Flare (30s duration, 115 dB siren + blinding strobe).
    /// Blinds light-sensitive entities (The Seep / Counterweight) and deafens/lures The Tuning Fork.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class StrobeSirenFlareGadget : MonoBehaviour
    {
        public bool IsIgnited = false;
        public float BurnTimeRemaining = 30f;
        private Light _strobeLight;
        private float _nextSirenEmit;

        public void IgniteFlare()
        {
            if (IsIgnited) return;
            IsIgnited = true;
            BurnTimeRemaining = 30f;

            GameObject lObj = new GameObject("FlareStrobeLight");
            lObj.transform.SetParent(transform, false);
            _strobeLight = lObj.AddComponent<Light>();
            _strobeLight.type = LightType.Point;
            _strobeLight.color = new Color(1f, 0.25f, 0.2f);
            _strobeLight.range = 22f;
            _strobeLight.intensity = 9f;

            EmitFlarePulse();
        }

        private void Update()
        {
            if (!IsIgnited) return;
            BurnTimeRemaining -= Time.deltaTime;
            if (_strobeLight != null)
            {
                _strobeLight.intensity = (Mathf.Sin(Time.time * 35f) > 0f) ? 10f : 1.5f;
            }

            if (Time.time >= _nextSirenEmit)
            {
                EmitFlarePulse();
            }

            if (BurnTimeRemaining <= 0f)
            {
                IsIgnited = false;
                if (_strobeLight != null) _strobeLight.enabled = false;
            }
        }

        private void EmitFlarePulse()
        {
            _nextSirenEmit = Time.time + 0.75f;
            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(transform.position, 115f, SoundSourceType.GadgetTimerOrHorn, gameObject);
            }

            foreach (var ent in FindObjectsByType<EntityPhysicsController>(FindObjectsSortMode.None))
            {
                if (Vector3.Distance(transform.position, ent.transform.position) < 12f)
                {
                    ent.ReactToHighLumenLight(transform.position);
                }
            }
        }
    }

    /// <summary>
    /// Item C6: Laser Trip-Beam Pointer.
    /// Projects a 40m red laser line across doorways or shafts; triggers silent alert when broken by an entity.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserTripBeamGadget : MonoBehaviour
    {
        public float MaxBeamRange = 40f;
        public bool IsBeamBrokenByEntity = false;
        public string LastDetectedEntityName = "";
        private LineRenderer _beamLine;

        private void Awake()
        {
            _beamLine = GetComponent<LineRenderer>();
            _beamLine.positionCount = 2;
            _beamLine.startWidth = 0.015f;
            _beamLine.endWidth = 0.015f;
            _beamLine.material = new Material(Shader.Find("Sprites/Default"));
            _beamLine.startColor = Color.red;
            _beamLine.endColor = Color.red;
        }

        private void Update()
        {
            Vector3 start = transform.position;
            Vector3 end = start + transform.forward * MaxBeamRange;
            IsBeamBrokenByEntity = false;

            if (Physics.Raycast(start, transform.forward, out RaycastHit hit, MaxBeamRange))
            {
                end = hit.point;
                EntityPhysicsController ent = hit.collider.GetComponentInParent<EntityPhysicsController>();
                if (ent != null)
                {
                    IsBeamBrokenByEntity = true;
                    LastDetectedEntityName = ent.QuirkType.ToString();
                }
            }

            _beamLine.SetPosition(0, start);
            _beamLine.SetPosition(1, end);
        }
    }

    /// <summary>
    /// Item F5: Contact Stethoscope.
    /// Amplifies sounds through closed doors (10x gain) and detects a sleeping Mimic's 48 BPM heartbeat!
    /// </summary>
    public class ContactStethoscopeGadget : MonoBehaviour
    {
        public static bool InspectTargetForMimicHeartbeat(GameObject target, out string diagnosisReport)
        {
            if (target == null)
            {
                diagnosisReport = "Stethoscope: No surface contact.";
                return false;
            }

            LootFurnitureItem loot = target.GetComponentInParent<LootFurnitureItem>();
            if (loot != null && loot.IsDisguisedMimic)
            {
                diagnosisReport = $"STETHOSCOPE ALERT: 48 BPM ORGANIC HEARTBEAT inside {loot.ItemDisplayName}! (MIMIC CONFIRMED)";
                return true;
            }

            diagnosisReport = $"Stethoscope: {target.name} is acoustically hollow/inanimate (Safe).";
            return false;
        }
    }

    /// <summary>
    /// Item E4 (Industrial Slip-Detergent Puddle) & Item D6 (Industrial Flypaper Strip).
    /// Modifies floor friction to 0.005 (causing The Ram to wipe out) or applies 900N sticky shear drag to The Seep!
    /// </summary>
    public class SurfaceModifierZone : MonoBehaviour
    {
        public bool IsZeroFrictionSlipDetergent = true;
        public bool IsStickyFlypaper = false;

        private void OnTriggerEnter(Collider other)
        {
            EntityPhysicsController entity = other.GetComponentInParent<EntityPhysicsController>();
            if (entity != null)
            {
                if (IsZeroFrictionSlipDetergent && entity.QuirkType == EntityQuirkType.TheRam_MomentumCharger)
                {
                    entity.TriggerRagdollStun(5.0f, "Slipped on Industrial Slip-Detergent zero-friction puddle (friction=0.005)!");
                }
                else if (IsStickyFlypaper && entity.QuirkType == EntityQuirkType.TheSeep_GapAndTubeCrawler)
                {
                    entity.TriggerRagdollStun(4.5f, "Caught on 900N Industrial Flypaper Strip!");
                }
            }

            NetworkedPhysicsObject netObj = other.GetComponentInParent<NetworkedPhysicsObject>();
            if (netObj != null && IsZeroFrictionSlipDetergent)
            {
                netObj.ApplyCasterWheels(); // Zero-friction slide effect
            }
        }
    }

    /// <summary>
    /// Item E2: Steel Crowbar / Lever Pipe (5x Fulcrum Prying Torque & Overhead Steel Cable Zipline Hook).
    /// </summary>
    public class SteelCrowbarTool : MonoBehaviour
    {
        public float LeverageMultiplier = 5.0f;

        public static float PryHeavyObjectWithFulcrum(Rigidbody targetRb, float handForceNewtons = 550f)
        {
            if (targetRb == null) return 0f;
            float pryImpulse = handForceNewtons * 5.0f; // 5x mechanical leverage = 2,750N!
            targetRb.WakeUp();
            targetRb.AddForce(Vector3.up * pryImpulse, ForceMode.Impulse);
            return pryImpulse;
        }
    }

    /// <summary>
    /// Item E6: Foldable Stair-Climbing Dolly.
    /// Halves perceived mass of tall furniture (Grandfather Clocks, Arcade Cabinets) and locks tilt so clocks never chime!
    /// </summary>
    public class StairClimbingDolly : MonoBehaviour
    {
        public static void StrapFurnitureToDolly(LootFurnitureItem targetLoot)
        {
            if (targetLoot == null || targetLoot.NetPhysics == null) return;
            targetLoot.NetPhysics.ApplyCasterWheels();
            targetLoot.IsClockPendulumTaped = true; // Stabilizes Grandfather Clock tilt angle!
            Rigidbody rb = targetLoot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = Mathf.Max(10f, rb.mass * 0.5f); // Halves effective hauling mass!
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            targetLoot.UpdatePriceTagText();
        }
    }

    /// <summary>
    /// Item F6: RC Motorized Roller Chassis.
    /// Remote-controlled wheeled flatbed (6 m/s, 25kg payload) for driving taped timers, walkie-talkies, or flares down halls.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RCMotorizedChassis : MonoBehaviour
    {
        public float DriveSpeed = 6.0f;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void DriveInDirection(Vector3 worldDir, float durationSeconds = 1.0f)
        {
            if (_rb == null) return;
            _rb.WakeUp();
            _rb.linearVelocity = worldDir.normalized * DriveSpeed;
        }
    }
}
