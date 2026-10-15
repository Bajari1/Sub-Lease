using System.Collections.Generic;
using SubLease.Core;
using UnityEngine;

namespace SubLease.PhysicsSystems
{
    public enum RopeGrade
    {
        HempTwine_800N,
        DynamicClimbing_2200N,
        BraidedSteel_6000N,
        IndustrialBungee_2500N
    }

    /// <summary>
    /// Marks any object (doorknob, radiator pipe, ceiling pulley, furniture, or monster limb)
    /// as a valid anchor point for Ropes, Grappling Hooks, and Pulleys.
    /// </summary>
    public class TetherAnchor : MonoBehaviour
    {
        public string AnchorLabel = "Pipe Anchor";
        public bool IsOverheadPulley = false;
        public int PulleyMechanicalAdvantage = 2; // 2x or 4x force multiplier
        public bool IsRappelShaftTopAnchor = false;

        public Rigidbody GetAttachedRigidbody()
        {
            return GetComponentInParent<Rigidbody>();
        }
    }

    /// <summary>
    /// Represents a live physical rope, steel cable, bungee cord, or tripwire in the world.
    /// Monitors real-time Newton tension, handles pulley mechanical advantage, and trips charging entities.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ActiveTetherRope : MonoBehaviour
    {
        public Transform StartAnchor;
        public Transform EndAnchor;
        public Transform OptionalPulleyMidpoint;

        public RopeGrade Grade = RopeGrade.HempTwine_800N;
        public float MaxRopeLength = 6f;
        public float MaxBreakForceNewtons = 800f;
        public float CurrentTensionNewtons;
        public bool IsTripwireActive;
        public bool IsVerticalRappelLine;

        private LineRenderer _line;
        private ConfigurableJoint _joint;
        private float _lastCreakTime;

        public void InitializeTether(Transform a, Transform b, RopeGrade grade, Transform pulleyMid = null)
        {
            StartAnchor = a;
            EndAnchor = b;
            OptionalPulleyMidpoint = pulleyMid;
            Grade = grade;

            MaxBreakForceNewtons = grade switch
            {
                RopeGrade.HempTwine_800N => 800f,
                RopeGrade.DynamicClimbing_2200N => 2200f,
                RopeGrade.BraidedSteel_6000N => 6000f,
                RopeGrade.IndustrialBungee_2500N => 2500f,
                _ => 1000f
            };

            // Pulley multiplies effective break resistance!
            if (OptionalPulleyMidpoint != null)
            {
                TetherAnchor pulleyAnchor = OptionalPulleyMidpoint.GetComponent<TetherAnchor>();
                int mult = pulleyAnchor != null ? Mathf.Max(2, pulleyAnchor.PulleyMechanicalAdvantage) : 2;
                MaxBreakForceNewtons *= mult;
            }

            float dist = Vector3.Distance(a.position, b.position);
            MaxRopeLength = Mathf.Max(0.4f, dist);

            // Detect if this rope is a vertical rappel line (>4m vertical drop) or a shin-high tripwire
            float heightDiff = Mathf.Abs(a.position.y - b.position.y);
            IsVerticalRappelLine = heightDiff >= 3.5f;
            IsTripwireActive = heightDiff < 0.6f && a.position.y < 1.1f && b.position.y < 1.1f && dist >= 1.0f;

            SetupPhysicsJoint();
            SetupLineRenderer();
        }

        private void SetupPhysicsJoint()
        {
            Rigidbody rbA = StartAnchor != null ? StartAnchor.GetComponentInParent<Rigidbody>() : null;
            Rigidbody rbB = EndAnchor != null ? EndAnchor.GetComponentInParent<Rigidbody>() : null;

            // Attach joint to whichever end has a dynamic Rigidbody
            Rigidbody hostRb = (rbA != null && !rbA.isKinematic) ? rbA : rbB;
            Rigidbody connectedRb = (hostRb == rbA) ? rbB : rbA;

            if (hostRb == null) return;

            _joint = hostRb.gameObject.AddComponent<ConfigurableJoint>();
            _joint.connectedBody = connectedRb;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = Vector3.zero;
            _joint.connectedAnchor = connectedRb != null
                ? Vector3.zero
                : (hostRb == rbA ? EndAnchor.position : StartAnchor.position);

            _joint.xMotion = ConfigurableJointMotion.Limited;
            _joint.yMotion = ConfigurableJointMotion.Limited;
            _joint.zMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit limit = new SoftJointLimit { limit = MaxRopeLength };
            _joint.linearLimit = limit;

            if (Grade == RopeGrade.IndustrialBungee_2500N)
            {
                SoftJointLimitSpring spring = new SoftJointLimitSpring { spring = 450f, damper = 25f };
                _joint.linearLimitSpring = spring;
            }
            else if (Grade == RopeGrade.DynamicClimbing_2200N)
            {
                SoftJointLimitSpring spring = new SoftJointLimitSpring { spring = 1800f, damper = 80f };
                _joint.linearLimitSpring = spring;
            }
        }

        private void SetupLineRenderer()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = OptionalPulleyMidpoint != null ? 3 : 2;
            _line.startWidth = Grade == RopeGrade.BraidedSteel_6000N ? 0.03f : 0.045f;
            _line.endWidth = _line.startWidth;
            _line.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void ShortenOrHoistRope(float deltaMeters)
        {
            MaxRopeLength = Mathf.Max(0.35f, MaxRopeLength - deltaMeters);
            if (_joint != null)
            {
                SoftJointLimit limit = _joint.linearLimit;
                limit.limit = MaxRopeLength;
                _joint.linearLimit = limit;

                if (_joint.GetComponent<Rigidbody>() != null)
                {
                    _joint.GetComponent<Rigidbody>().WakeUp();
                }
            }
        }

        private void FixedUpdate()
        {
            if (StartAnchor == null || EndAnchor == null)
            {
                SnapAndDestroy();
                return;
            }

            // Compute live Newton tension from joint force + suspended mass
            float forceMag = _joint != null ? _joint.currentForce.magnitude : 0f;
            Rigidbody rbA = StartAnchor.GetComponentInParent<Rigidbody>();
            Rigidbody rbB = EndAnchor.GetComponentInParent<Rigidbody>();
            float loadWeightN = 0f;
            if (rbA != null && !rbA.isKinematic) loadWeightN = Mathf.Max(loadWeightN, rbA.mass * 9.81f);
            if (rbB != null && !rbB.isKinematic) loadWeightN = Mathf.Max(loadWeightN, rbB.mass * 9.81f);

            float currentDist = Vector3.Distance(StartAnchor.position, EndAnchor.position);
            bool isTaut = currentDist >= MaxRopeLength * 0.92f;
            CurrentTensionNewtons = isTaut ? Mathf.Max(forceMag, loadWeightN) : forceMag * 0.25f;

            // Emit warning creaks above 75% break force
            float stressRatio = CurrentTensionNewtons / Mathf.Max(1f, MaxBreakForceNewtons);
            if (stressRatio > 0.75f && Time.time > _lastCreakTime + 1.1f)
            {
                _lastCreakTime = Time.time;
                if (AcousticAndPhysicsBus.Instance != null)
                {
                    Vector3 mid = (StartAnchor.position + EndAnchor.position) * 0.5f;
                    AcousticAndPhysicsBus.Instance.EmitSound(mid, 58f, SoundSourceType.RopeCreakOrSnap, gameObject);
                }
            }

            // Check if tension exceeds rope BreakForce!
            if (CurrentTensionNewtons > MaxBreakForceNewtons)
            {
                if (AcousticAndPhysicsBus.Instance != null)
                {
                    Vector3 mid = (StartAnchor.position + EndAnchor.position) * 0.5f;
                    AcousticAndPhysicsBus.Instance.EmitSound(mid, 92f, SoundSourceType.RopeCreakOrSnap, gameObject, CurrentTensionNewtons);
                }
                Debug.Log($"[TetherSystem] Rope SNAPPED under {CurrentTensionNewtons:F0}N load (Limit: {MaxBreakForceNewtons:F0}N)!");
                SnapAndDestroy();
            }
        }

        private void Update()
        {
            if (_line == null || StartAnchor == null || EndAnchor == null) return;

            if (OptionalPulleyMidpoint != null)
            {
                _line.positionCount = 3;
                _line.SetPosition(0, StartAnchor.position);
                _line.SetPosition(1, OptionalPulleyMidpoint.position);
                _line.SetPosition(2, EndAnchor.position);
            }
            else
            {
                _line.positionCount = 2;
                _line.SetPosition(0, StartAnchor.position);
                _line.SetPosition(1, EndAnchor.position);
            }

            // Color shifts from rope color -> bright red as tension nears 100%
            float stress = Mathf.Clamp01(CurrentTensionNewtons / Mathf.Max(1f, MaxBreakForceNewtons));
            Color baseCol = Grade switch
            {
                RopeGrade.HempTwine_800N => new Color(0.82f, 0.70f, 0.45f),
                RopeGrade.DynamicClimbing_2200N => new Color(0.2f, 0.75f, 0.95f),
                RopeGrade.BraidedSteel_6000N => new Color(0.75f, 0.78f, 0.82f),
                RopeGrade.IndustrialBungee_2500N => new Color(0.95f, 0.5f, 0.15f),
                _ => Color.yellow
            };

            Color finalCol = Color.Lerp(baseCol, Color.red, stress * stress);
            _line.startColor = finalCol;
            _line.endColor = finalCol;
        }

        public void SnapAndDestroy()
        {
            if (_joint != null) Destroy(_joint);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Physical 3-Pronged Throwing Grapple / Pneumatic Piton Harpoon / Silent Magnetic Grapple projectile.
    /// Trails a rope line in flight and clamps onto furniture, ceiling hooks, pipes, or monsters on impact!
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GrappleHookProjectile : MonoBehaviour
    {
        public Transform OriginAnchor;
        public RopeGrade AttachedRopeGrade = RopeGrade.DynamicClimbing_2200N;
        public bool IsSilentMagnetic = false;
        public bool IsPitonHarpoon = false;
        public bool HasLatched { get; private set; }

        private LineRenderer _flightLine;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _flightLine = gameObject.AddComponent<LineRenderer>();
            _flightLine.positionCount = 2;
            _flightLine.startWidth = 0.03f;
            _flightLine.endWidth = 0.03f;
            _flightLine.material = new Material(Shader.Find("Sprites/Default"));
            _flightLine.startColor = new Color(0.25f, 0.85f, 0.95f);
            _flightLine.endColor = new Color(0.25f, 0.85f, 0.95f);
        }

        private void Update()
        {
            if (_flightLine != null && OriginAnchor != null)
            {
                _flightLine.SetPosition(0, OriginAnchor.position);
                _flightLine.SetPosition(1, transform.position);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (HasLatched) return;
            if (OriginAnchor != null && collision.transform.IsChildOf(OriginAnchor)) return;

            HasLatched = true;
            _rb.isKinematic = true;
            transform.SetParent(collision.transform, true);

            if (_flightLine != null) Destroy(_flightLine);

            // Emit grapple impact sound (5 dB if Silent Magnetic Grapple, 65 dB if steel claw)
            float db = IsSilentMagnetic ? 5f : 65f;
            if (AcousticAndPhysicsBus.Instance != null && db > 25f)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(transform.position, db, SoundSourceType.PhysicsCollision, gameObject);
            }

            // Automatically spawn an ActiveTetherRope between OriginAnchor and our latched grapple claw!
            if (OriginAnchor != null)
            {
                GameObject ropeObj = new GameObject("GrappleTetherRope");
                ActiveTetherRope rope = ropeObj.AddComponent<ActiveTetherRope>();
                rope.InitializeTether(OriginAnchor, transform, AttachedRopeGrade);
            }
        }
    }
}
