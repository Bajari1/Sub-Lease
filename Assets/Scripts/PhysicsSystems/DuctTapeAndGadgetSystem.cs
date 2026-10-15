using SubLease.Core;
using SubLease.Networking;
using UnityEngine;

namespace SubLease.PhysicsSystems
{
    public enum TapeVariant
    {
        StandardGrey_1200N,
        KevlarStrapping_4500N,
        AcousticFoam_Silent,
        RubberElectrical_Insulated,
        BubbleWrap_DentShield
    }

    /// <summary>
    /// Tracks duct tape welds on doors, vents, furniture contraptions, and sleeping Mimics.
    /// </summary>
    public class DuctTapeWeld : MonoBehaviour
    {
        public TapeVariant Variant;
        public float TotalBreakForceNewtons = 1200f;
        public int StripCount = 1;
        public FixedJoint WeldJoint;
        public GameObject VisualStrip;

        public void AddStrip(TapeVariant variant)
        {
            StripCount++;
            float addForce = variant == TapeVariant.KevlarStrapping_4500N ? 4500f : 1200f;
            TotalBreakForceNewtons += addForce;
            if (WeldJoint != null)
            {
                WeldJoint.breakForce = TotalBreakForceNewtons;
                WeldJoint.breakTorque = TotalBreakForceNewtons;
            }
        }

        private void OnJointBreak(float breakForce)
        {
            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(transform.position, 78f, SoundSourceType.PhysicsCollision, gameObject, breakForce);
            }
            if (VisualStrip != null) Destroy(VisualStrip);
            Destroy(this);
        }
    }

    /// <summary>
    /// Helper factory for applying Duct Tape strips, Caster Wheels, CO2 Lift Bags, and Kitchen Timers.
    /// </summary>
    public static class DuctTapeAndGadgetFactory
    {
        public static void ApplyTapeBetween(Collider colA, Vector3 pointA, Collider colB, Vector3 pointB, TapeVariant variant)
        {
            if (colA == null || colB == null) return;

            // Create visual tape strip bridging pointA and pointB
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = $"TapeStrip_{variant}";
            Object.Destroy(strip.GetComponent<Collider>());

            Vector3 mid = (pointA + pointB) * 0.5f;
            float dist = Mathf.Clamp(Vector3.Distance(pointA, pointB), 0.18f, 1.8f);
            strip.transform.position = mid;
            if ((pointB - pointA).sqrMagnitude > 0.001f)
            {
                strip.transform.rotation = Quaternion.LookRotation((pointB - pointA).normalized);
            }
            strip.transform.localScale = new Vector3(0.08f, 0.015f, dist);
            strip.transform.SetParent(colA.transform, true);

            Renderer rend = strip.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = variant switch
                {
                    TapeVariant.StandardGrey_1200N => new Color(0.65f, 0.67f, 0.70f),
                    TapeVariant.KevlarStrapping_4500N => new Color(0.95f, 0.72f, 0.12f),
                    TapeVariant.AcousticFoam_Silent => new Color(0.22f, 0.25f, 0.32f),
                    TapeVariant.RubberElectrical_Insulated => new Color(0.12f, 0.12f, 0.12f),
                    TapeVariant.BubbleWrap_DentShield => new Color(0.75f, 0.92f, 1.0f, 0.7f),
                    _ => Color.gray
                };
                rend.material = mat;
            }

            // Apply special material modifiers to target NetworkedPhysicsObject
            NetworkedPhysicsObject netA = colA.GetComponentInParent<NetworkedPhysicsObject>();
            NetworkedPhysicsObject netB = colB.GetComponentInParent<NetworkedPhysicsObject>();

            ApplyVariantModifier(netA, variant);
            if (netB != netA) ApplyVariantModifier(netB, variant);

            // If welding two distinct physical objects (or a door/prop to a wall), create or reinforce FixedJoint
            Rigidbody rbA = colA.GetComponentInParent<Rigidbody>();
            Rigidbody rbB = colB.GetComponentInParent<Rigidbody>();

            if (rbA != rbB && (rbA != null || rbB != null))
            {
                Rigidbody hostRb = (rbA != null && !rbA.isKinematic) ? rbA : rbB;
                Rigidbody otherRb = (hostRb == rbA) ? rbB : rbA;

                if (hostRb != null)
                {
                    DuctTapeWeld existing = hostRb.GetComponent<DuctTapeWeld>();
                    if (existing != null && existing.WeldJoint != null && existing.WeldJoint.connectedBody == otherRb)
                    {
                        existing.AddStrip(variant);
                    }
                    else
                    {
                        float force = variant == TapeVariant.KevlarStrapping_4500N ? 4500f : 1200f;
                        FixedJoint fj = hostRb.gameObject.AddComponent<FixedJoint>();
                        fj.connectedBody = otherRb;
                        fj.breakForce = force;
                        fj.breakTorque = force;

                        DuctTapeWeld weld = hostRb.gameObject.AddComponent<DuctTapeWeld>();
                        weld.Variant = variant;
                        weld.TotalBreakForceNewtons = force;
                        weld.WeldJoint = fj;
                        weld.VisualStrip = strip;
                    }
                }
            }
        }

        private static void ApplyVariantModifier(NetworkedPhysicsObject netObj, TapeVariant variant)
        {
            if (netObj == null) return;
            switch (variant)
            {
                case TapeVariant.AcousticFoam_Silent:
                    netObj.ApplyAcousticFoamTape();
                    break;
                case TapeVariant.BubbleWrap_DentShield:
                    netObj.ApplyBubbleWrap();
                    break;
                case TapeVariant.RubberElectrical_Insulated:
                    netObj.IsElectricallyInsulated = true;
                    break;
                case TapeVariant.KevlarStrapping_4500N:
                    netObj.KevlarTapeStrapsApplied++;
                    break;
            }
        }

        public static void AttachInflatableLiftBag(Rigidbody targetRb)
        {
            if (targetRb == null) return;
            GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bag.name = "Inflated_CO2_LiftBag";
            Object.Destroy(bag.GetComponent<Collider>());
            bag.transform.SetParent(targetRb.transform, false);
            bag.transform.localPosition = Vector3.up * 1.1f;
            bag.transform.localScale = new Vector3(1.1f, 1.35f, 1.1f);

            Renderer r = bag.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = new Color(0.95f, 0.35f, 0.1f);
                r.material = mat;
            }

            InflatableLiftBag lift = targetRb.gameObject.AddComponent<InflatableLiftBag>();
            lift.UpwardBuoyantForceNewtons = 95f * 9.81f; // +95kg upward lift!
        }
    }

    /// <summary>
    /// Applies +95kg of upward buoyant lift force to float heavy furniture up vertical rappel shafts!
    /// </summary>
    public class InflatableLiftBag : MonoBehaviour
    {
        public float UpwardBuoyantForceNewtons = 932f; // 95kg * 9.81m/s^2
        public float MaxCeilingHeight = 1.5f;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            if (transform.position.y < MaxCeilingHeight)
            {
                _rb.AddForce(Vector3.up * UpwardBuoyantForceNewtons, ForceMode.Force);
            }
        }
    }

    /// <summary>
    /// Physical Flashlight / 10,000-Lumen Halogen Floodlight / 365nm UV Blacklight Inspector.
    /// Can be held, clipped to belt, or duct-taped onto rolling chairs and swinging ropes!
    /// </summary>
    public class PhysicalFlashlightTool : MonoBehaviour
    {
        public enum LightMode
        {
            StandardWorkBeam,
            HighLumenHalogenFlood,
            UVBlacklightInspector
        }

        public LightMode CurrentMode = LightMode.StandardWorkBeam;
        public bool IsSwitchedOn = true;
        public Light SpotlightComponent;

        private void Awake()
        {
            if (SpotlightComponent == null)
            {
                GameObject lightChild = new GameObject("FlashlightBeam");
                lightChild.transform.SetParent(transform, false);
                SpotlightComponent = lightChild.AddComponent<Light>();
                SpotlightComponent.type = LightType.Spot;
                SpotlightComponent.shadows = LightShadows.Soft;
            }
            ApplyLightModeVisuals();
        }

        public void CycleMode()
        {
            int next = ((int)CurrentMode + 1) % 3;
            CurrentMode = (LightMode)next;
            ApplyLightModeVisuals();

            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(transform.position, 35f, SoundSourceType.PhysicsCollision, gameObject);
            }
        }

        private void ApplyLightModeVisuals()
        {
            if (SpotlightComponent == null) return;
            SpotlightComponent.enabled = IsSwitchedOn;

            switch (CurrentMode)
            {
                case LightMode.StandardWorkBeam:
                    SpotlightComponent.color = new Color(1.0f, 0.95f, 0.82f);
                    SpotlightComponent.intensity = 3.5f;
                    SpotlightComponent.range = 24f;
                    SpotlightComponent.spotAngle = 48f;
                    break;
                case LightMode.HighLumenHalogenFlood:
                    SpotlightComponent.color = new Color(0.95f, 0.98f, 1.0f);
                    SpotlightComponent.intensity = 8.5f;
                    SpotlightComponent.range = 38f;
                    SpotlightComponent.spotAngle = 68f;
                    break;
                case LightMode.UVBlacklightInspector:
                    SpotlightComponent.color = new Color(0.65f, 0.15f, 1.0f);
                    SpotlightComponent.intensity = 4.0f;
                    SpotlightComponent.range = 18f;
                    SpotlightComponent.spotAngle = 42f;
                    break;
            }
        }
    }
}
