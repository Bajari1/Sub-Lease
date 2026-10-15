using SubLease.Core;
using SubLease.Gameplay;
using SubLease.PhysicsSystems;
using UnityEngine;

namespace SubLease.AI
{
    public enum EntityQuirkType
    {
        TheRam_MomentumCharger,
        TheSeep_GapAndTubeCrawler,
        TheMimic_DisguisedProp95Kg,
        TheCounterweight_CeilingWinchPredator,
        TheTuningFork_BlindAcousticHunter
    }

    /// <summary>
    /// Physics-driven AI controller for the 5 Core Entities.
    /// Every entity obeys PhysX forces, rope tension, flashlight beams, acoustic impulses, and infighting collisions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EntityPhysicsController : MonoBehaviour
    {
        public EntityQuirkType QuirkType = EntityQuirkType.TheRam_MomentumCharger;
        public float StunnedTimer = 0f;
        public Vector3 TargetHuntPosition;
        public bool HasTargetPosition = false;

        [Header("Counterweight Ceiling Winch State")]
        public Transform CeilingTongueTip;
        public Rigidbody SnaggedPreyOrLoot;
        private LineRenderer _tongueLine;

        private Rigidbody _rb;
        private float _chargeCooldown;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (GetComponent<ProceduralMultiJointEntityRig>() == null)
            {
                gameObject.AddComponent<ProceduralMultiJointEntityRig>();
            }
        }

        private void Start()
        {
            if (QuirkType == EntityQuirkType.TheTuningFork_BlindAcousticHunter && AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.OnAcousticImpulse += HandleAcousticImpulse;
            }

            if (QuirkType == EntityQuirkType.TheCounterweight_CeilingWinchPredator)
            {
                _rb.isKinematic = true;
                _tongueLine = gameObject.AddComponent<LineRenderer>();
                _tongueLine.positionCount = 2;
                _tongueLine.startWidth = 0.05f;
                _tongueLine.endWidth = 0.03f;
                _tongueLine.material = new Material(Shader.Find("Sprites/Default"));
                _tongueLine.startColor = new Color(0.55f, 0.15f, 0.25f);
                _tongueLine.endColor = new Color(0.75f, 0.25f, 0.35f);
            }
        }

        private void OnDestroy()
        {
            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.OnAcousticImpulse -= HandleAcousticImpulse;
            }
        }

        private void HandleAcousticImpulse(AcousticEvent evt)
        {
            if (StunnedTimer > 0f) return;

            // Air-Horn Overload (>125 dB within 8m) stuns The Tuning Fork for 6 seconds!
            if (evt.Decibels >= 125f && Vector3.Distance(transform.position, evt.WorldPosition) < 8.5f)
            {
                TriggerRagdollStun(6.0f, "135 dB Air-Horn Acoustic Overload");
                return;
            }

            if (evt.Decibels >= 42f && evt.Instigator != gameObject)
            {
                TargetHuntPosition = evt.WorldPosition;
                HasTargetPosition = true;
            }
        }

        public void ReactToHighLumenLight(Vector3 lightOrigin)
        {
            if (QuirkType == EntityQuirkType.TheCounterweight_CeilingWinchPredator)
            {
                // Shining High-Lumen Halogen Floodlight into The Counterweight's ceiling hole makes it drop stolen loot!
                if (SnaggedPreyOrLoot != null)
                {
                    Debug.Log("[TheCounterweight] Blinded by High-Lumen Halogen Floodlight! Dropping snagged furniture!");
                    SnaggedPreyOrLoot = null;
                }
                StunnedTimer = 3.5f;
            }
            else if (QuirkType == EntityQuirkType.TheSeep_GapAndTubeCrawler)
            {
                // Repel The Seep away from the light beam
                Vector3 fleeDir = (transform.position - lightOrigin).normalized;
                _rb.AddForce(fleeDir * 320f, ForceMode.Force);
            }
        }

        public void TriggerRagdollStun(float seconds, string reason)
        {
            StunnedTimer = Mathf.Max(StunnedTimer, seconds);
            _rb.freezeRotation = false;
            Debug.Log($"[{QuirkType}] RAGDOLL STUNNED for {seconds:F1}s! Reason: {reason}");
        }

        private void FixedUpdate()
        {
            if (StunnedTimer > 0f)
            {
                StunnedTimer -= Time.fixedDeltaTime;
                if (StunnedTimer <= 0f && QuirkType != EntityQuirkType.TheCounterweight_CeilingWinchPredator)
                {
                    // Recover upright posture
                    transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                    _rb.freezeRotation = true;
                }
                return;
            }

            switch (QuirkType)
            {
                case EntityQuirkType.TheRam_MomentumCharger:
                    UpdateRamBehavior();
                    break;
                case EntityQuirkType.TheSeep_GapAndTubeCrawler:
                    UpdateSeepBehavior();
                    break;
                case EntityQuirkType.TheMimic_DisguisedProp95Kg:
                    UpdateMimicBehavior();
                    break;
                case EntityQuirkType.TheCounterweight_CeilingWinchPredator:
                    UpdateCounterweightBehavior();
                    break;
                case EntityQuirkType.TheTuningFork_BlindAcousticHunter:
                    UpdateTuningForkBehavior();
                    break;
            }
        }

        private void UpdateRamBehavior()
        {
            // Patrols or bull-charges in straight lines with huge linear momentum (180kg) and low braking friction
            _chargeCooldown -= Time.fixedDeltaTime;
            if (_chargeCooldown <= 0f)
            {
                _chargeCooldown = 4.5f;
                Vector3 chargeDir = transform.forward;
                if (Camera.main != null && Vector3.Distance(transform.position, Camera.main.transform.position) < 16f)
                {
                    Vector3 toPlayer = Camera.main.transform.position - transform.position;
                    toPlayer.y = 0f;
                    if (toPlayer.sqrMagnitude > 0.5f)
                    {
                        chargeDir = toPlayer.normalized;
                        transform.rotation = Quaternion.LookRotation(chargeDir);
                    }
                }
                _rb.AddForce(chargeDir * 3800f, ForceMode.Impulse);
            }

            // Check if The Ram runs into an active shin-height Tripwire!
            foreach (var rope in FindObjectsByType<ActiveTetherRope>(FindObjectsSortMode.None))
            {
                if (!rope.IsTripwireActive || rope.StartAnchor == null || rope.EndAnchor == null) continue;
                float distToLine = DistancePointToSegment(transform.position, rope.StartAnchor.position, rope.EndAnchor.position);
                if (distToLine < 0.75f && _rb.linearVelocity.magnitude > 3.0f)
                {
                    // Tripwire flips The Ram head-over-heels using its own momentum!
                    _rb.freezeRotation = false;
                    _rb.AddForce(Vector3.up * 1200f, ForceMode.Impulse);
                    _rb.AddTorque(transform.right * 2400f, ForceMode.Impulse);
                    TriggerRagdollStun(5.5f, "Tripped over taut shin-height Rope Tripwire!");
                    break;
                }
            }
        }

        private void UpdateSeepBehavior()
        {
            // Weak pulling force (350N max) — if tethered to a 50kg+ furniture anchor, it can't drag it!
            if (Camera.main != null && Vector3.Distance(transform.position, Camera.main.transform.position) < 15f)
            {
                Vector3 dir = (Camera.main.transform.position - transform.position).normalized;
                _rb.AddForce(dir * 260f, ForceMode.Force);
            }

            // Chew through Hemp Twine (800N) if touching it, but CANNOT chew Braided Steel Cable!
            foreach (var rope in FindObjectsByType<ActiveTetherRope>(FindObjectsSortMode.None))
            {
                if (rope.Grade == RopeGrade.HempTwine_800N && rope.StartAnchor != null && rope.EndAnchor != null)
                {
                    float d = DistancePointToSegment(transform.position, rope.StartAnchor.position, rope.EndAnchor.position);
                    if (d < 0.65f)
                    {
                        Debug.Log("[TheSeep] Chewed through Hemp Twine! (Use Braided Steel Cable to prevent chewing)");
                        rope.SnapAndDestroy();
                        break;
                    }
                }
            }
        }

        private void UpdateMimicBehavior()
        {
            LootFurnitureItem loot = GetComponent<LootFurnitureItem>();
            if (loot == null || !loot.IsMimicAwake || loot.IsMimicSafelyTaped()) return;

            // Awake Mimic hops aggressively toward nearest player unless taped down!
            if (Camera.main != null && Time.frameCount % 50 == 0)
            {
                Vector3 dir = (Camera.main.transform.position - transform.position).normalized;
                _rb.AddForce((dir + Vector3.up * 0.45f) * 950f, ForceMode.Impulse);
            }
        }

        private void UpdateCounterweightBehavior()
        {
            if (_tongueLine == null) return;

            Vector3 ceilingOrigin = transform.position;
            if (SnaggedPreyOrLoot == null)
            {
                // Scan directly below the shaft/ceiling hole for furniture or items to hoist!
                if (Physics.SphereCast(ceilingOrigin, 1.2f, Vector3.down, out RaycastHit hit, 22f))
                {
                    Rigidbody hitRb = hit.rigidbody;
                    if (hitRb != null && !hitRb.isKinematic)
                    {
                        SnaggedPreyOrLoot = hitRb;
                    }
                }
                _tongueLine.SetPosition(0, ceilingOrigin);
                _tongueLine.SetPosition(1, ceilingOrigin + Vector3.down * 4.5f);
            }
            else
            {
                _tongueLine.SetPosition(0, ceilingOrigin);
                _tongueLine.SetPosition(1, SnaggedPreyOrLoot.position);

                // If the player attached a >180kg object (like the 220kg Marble Statue!), the excess weight RIPS The Counterweight out of the ceiling!
                if (SnaggedPreyOrLoot.mass >= 180f)
                {
                    _rb.isKinematic = false;
                    _rb.mass = 85f;
                    SnaggedPreyOrLoot = null;
                    TriggerRagdollStun(8.0f, "Overloaded by >180kg counterweight! Ripped out of the ceiling!");
                    return;
                }

                // Otherwise, reel the furniture upward at 1,600N (Players can exploit this as a FREE elevator winch!)
                Vector3 pullUp = (ceilingOrigin - SnaggedPreyOrLoot.position).normalized;
                SnaggedPreyOrLoot.AddForce(pullUp * 1600f, ForceMode.Force);
            }
        }

        private void UpdateTuningForkBehavior()
        {
            if (!HasTargetPosition) return;

            Vector3 toSound = TargetHuntPosition - transform.position;
            toSound.y = 0f;
            if (toSound.magnitude < 0.8f)
            {
                HasTargetPosition = false;
                return;
            }

            Vector3 moveDir = toSound.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.fixedDeltaTime * 10f);
            _rb.MovePosition(_rb.position + moveDir * (10.5f * Time.fixedDeltaTime));
        }

        private void OnCollisionEnter(Collision collision)
        {
            float impulse = collision.impulse.magnitude;

            // Ecosystem Infighting: If two entities collide at high momentum, or a heavy counterweight drops on an entity (>450 N*s), stun!
            EntityPhysicsController otherEntity = collision.gameObject.GetComponentInParent<EntityPhysicsController>();
            if (otherEntity != null && otherEntity != this && impulse > 180f)
            {
                TriggerRagdollStun(6.0f, $"Ecosystem Infighting collision with {otherEntity.QuirkType}!");
                otherEntity.TriggerRagdollStun(6.0f, $"Ecosystem Infighting collision with {QuirkType}!");
                return;
            }

            if (impulse > 420f && collision.rigidbody != null && collision.rigidbody.mass >= 35f)
            {
                TriggerRagdollStun(6.5f, $"Crushed by heavy falling counterweight ({collision.gameObject.name}, Impulse: {impulse:F0} N*s)!");
            }
        }

        private static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Mathf.Max(0.0001f, Vector3.Dot(ab, ab));
            t = Mathf.Clamp01(t);
            return Vector3.Distance(p, a + ab * t);
        }
    }
}
