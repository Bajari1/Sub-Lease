using System.Collections.Generic;
using SubLease.PhysicsSystems;
using UnityEngine;

namespace SubLease.Player
{
    /// <summary>
    /// P1.2: Physical 3D Waist Belt Holster Ring around the player's torso.
    /// Players can look down in VR or Desktop mode to see and grab their physical tools
    /// (Rope Spool, Grapple Claw, Duct Tape Gun, Flashlight, Digital Scale, and Waist Brake Carabiner).
    /// </summary>
    public class WaistBeltHolsterSystem : MonoBehaviour
    {
        [System.Serializable]
        public class HolsterSlotVisual
        {
            public string SlotName;
            public PlayerToolSlot ToolSlot;
            public Vector3 LocalWaistOffset;
            public Color PouchColor;
            public GameObject PouchObject;
        }

        public Transform HeadCameraTransform;
        public List<HolsterSlotVisual> HolsterSlots = new List<HolsterSlotVisual>();

        private Transform _beltRingRoot;

        private void Start()
        {
            BuildPhysicalWaistBelt();
        }

        private void BuildPhysicalWaistBelt()
        {
            _beltRingRoot = new GameObject("PhysicalWaistBeltRing").transform;
            _beltRingRoot.SetParent(transform, false);
            _beltRingRoot.localPosition = new Vector3(0f, -0.05f, 0f);

            HolsterSlots = new List<HolsterSlotVisual>
            {
                new HolsterSlotVisual
                {
                    SlotName = "Holster_1_GrabAndWheels",
                    ToolSlot = PlayerToolSlot.Slot1_PhysicsHandsAndHardware,
                    LocalWaistOffset = new Vector3(-0.22f, 0f, 0.22f),
                    PouchColor = new Color(0.35f, 0.75f, 0.40f)
                },
                new HolsterSlotVisual
                {
                    SlotName = "Holster_2_RopeSpool",
                    ToolSlot = PlayerToolSlot.Slot2_RopeAndPulleyRig,
                    LocalWaistOffset = new Vector3(0.22f, 0f, 0.22f),
                    PouchColor = new Color(0.88f, 0.72f, 0.32f)
                },
                new HolsterSlotVisual
                {
                    SlotName = "Holster_3_GrappleClaw",
                    ToolSlot = PlayerToolSlot.Slot3_GrapplingHookLauncher,
                    LocalWaistOffset = new Vector3(0.32f, 0f, 0.04f),
                    PouchColor = new Color(0.25f, 0.80f, 0.95f)
                },
                new HolsterSlotVisual
                {
                    SlotName = "Holster_4_DuctTapeGun",
                    ToolSlot = PlayerToolSlot.Slot4_DuctTapeDispenser,
                    LocalWaistOffset = new Vector3(-0.32f, 0f, 0.04f),
                    PouchColor = new Color(0.75f, 0.75f, 0.78f)
                },
                new HolsterSlotVisual
                {
                    SlotName = "Holster_5_FlashlightAndUV",
                    ToolSlot = PlayerToolSlot.Slot5_FlashlightAndUVScanner,
                    LocalWaistOffset = new Vector3(-0.20f, 0f, -0.18f),
                    PouchColor = new Color(0.75f, 0.30f, 0.95f)
                },
                new HolsterSlotVisual
                {
                    SlotName = "Holster_6_DigitalScale",
                    ToolSlot = PlayerToolSlot.Slot6_DigitalScaleAndAudio,
                    LocalWaistOffset = new Vector3(0.20f, 0f, -0.18f),
                    PouchColor = new Color(0.95f, 0.45f, 0.20f)
                }
            };

            foreach (var slot in HolsterSlots)
            {
                GameObject pouch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pouch.name = slot.SlotName;
                pouch.transform.SetParent(_beltRingRoot, false);
                pouch.transform.localPosition = slot.LocalWaistOffset;
                pouch.transform.localScale = new Vector3(0.09f, 0.12f, 0.09f);

                // Make trigger collider so it never collides with player CharacterController
                Collider col = pouch.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                Renderer r = pouch.GetComponent<Renderer>();
                if (r != null)
                {
                    Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                    mat.color = slot.PouchColor;
                    r.material = mat;
                }

                HolsterPouchInteractable tag = pouch.AddComponent<HolsterPouchInteractable>();
                tag.AssignedSlot = slot.ToolSlot;
                slot.PouchObject = pouch;
            }
        }

        private void LateUpdate()
        {
            if (_beltRingRoot == null || HeadCameraTransform == null) return;

            // Keep belt positioned beneath head and aligned with horizontal yaw
            Vector3 flatFwd = Vector3.ProjectOnPlane(HeadCameraTransform.forward, Vector3.up);
            if (flatFwd.sqrMagnitude > 0.01f)
            {
                _beltRingRoot.rotation = Quaternion.LookRotation(flatFwd.normalized, Vector3.up);
            }
        }

        public void HighlightActiveSlot(PlayerToolSlot active)
        {
            foreach (var slot in HolsterSlots)
            {
                if (slot.PouchObject == null) continue;
                float scale = (slot.ToolSlot == active) ? 0.125f : 0.085f;
                slot.PouchObject.transform.localScale = new Vector3(scale, scale * 1.25f, scale);
            }
        }
    }

    public class HolsterPouchInteractable : MonoBehaviour
    {
        public PlayerToolSlot AssignedSlot;
    }

    /// <summary>
    /// P1.3: Ratchet One-Way Shaft Cleat (Item B6).
    /// Mounted at the top of a vertical rappel shaft. When active, any rope passing through the shaft pulley
    /// automatically ratchets shorter as furniture is hoisted upward—preventing the load from falling back down!
    /// </summary>
    public class RatchetOneWayCleat : MonoBehaviour
    {
        public static RatchetOneWayCleat Instance { get; private set; }

        public bool IsRatchetEngaged = true;
        public float TotalHoistedMeters = 0f;

        private void Awake()
        {
            Instance = this;
        }

        private void FixedUpdate()
        {
            if (!IsRatchetEngaged) return;

            foreach (var rope in FindObjectsByType<ActiveTetherRope>(FindObjectsSortMode.None))
            {
                if (rope == null || rope.StartAnchor == null || rope.EndAnchor == null) continue;
                if (!rope.IsVerticalRappelLine && rope.OptionalPulleyMidpoint == null) continue;

                float currentSpan = Vector3.Distance(rope.StartAnchor.position, rope.EndAnchor.position);
                // If the load was lifted closer to the anchor/pulley, ratchet the max length so it cannot slip back!
                if (currentSpan + 0.12f < rope.MaxRopeLength)
                {
                    float delta = rope.MaxRopeLength - (currentSpan + 0.12f);
                    rope.ShortenOrHoistRope(delta);
                    TotalHoistedMeters += delta;
                }
            }
        }
    }
}
