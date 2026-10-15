using SubLease.AI;
using SubLease.Core;
using SubLease.Gameplay;
using SubLease.Networking;
using SubLease.PhysicsSystems;
using UnityEngine;

namespace SubLease.Player
{
    public enum PlayerToolSlot
    {
        Slot1_PhysicsHandsAndHardware = 1,
        Slot2_RopeAndPulleyRig = 2,
        Slot3_GrapplingHookLauncher = 3,
        Slot4_DuctTapeDispenser = 4,
        Slot5_FlashlightAndUVScanner = 5,
        Slot6_DigitalScaleAndAudio = 6
    }

    /// <summary>
    /// Unified OpenXR VR + Desktop First-Person Physics Player Controller (Upgraded for P1).
    /// Features:
    /// - P1.1: Two-Handed Independent PD-Joint Grabbing (Left + Right Hand AddForceAtPosition + 90-Degree Desk Roll [Z]/[X])
    /// - P1.2: Physical 3D Waist Belt Holster Ring (Look down & click pouch or press 1-6)
    /// - P1.3: Hand-Over-Hand Pulley Hoisting (Alternate Left/Right pulls) + Ratchet One-Way Shaft Cleat ([K])
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DualPhysicsPlayerRig : MonoBehaviour
    {
        public static DualPhysicsPlayerRig Instance { get; private set; }

        [Header("Camera & Dual Physics Hand Anchors")]
        public Camera PlayerCamera;
        public Transform LeftToolHandAnchor;
        public Transform RightToolHandAnchor;
        public PhysicalFlashlightTool EquippedFlashlight;
        public WaistBeltHolsterSystem WaistHolster;

        [Header("Movement & Rappel Carabiner")]
        public float WalkSpeed = 5.5f;
        public float SprintSpeed = 8.5f;
        public float Gravity = -18.5f;
        public bool IsCarabinerLockedMidAir = false;

        [Header("Equipped Tool State")]
        public PlayerToolSlot ActiveSlot = PlayerToolSlot.Slot1_PhysicsHandsAndHardware;
        public RopeGrade SelectedRopeGrade = RopeGrade.DynamicClimbing_2200N;
        public TapeVariant SelectedTapeVariant = TapeVariant.StandardGrey_1200N;
        public bool ShowLobbyAndShopWindow = false;

        [Header("P1.1 Two-Handed Independent Physics Grab State")]
        public Rigidbody LeftHandGrabbedRb;
        public Vector3 LeftHandLocalGrabOffset;
        public float LeftGrabDistance = 2.4f;

        public Rigidbody RightHandGrabbedRb;
        public Vector3 RightHandLocalGrabOffset;
        public float RightGrabDistance = 2.4f;

        public bool IsTwoHandedGrabActive => LeftHandGrabbedRb != null && LeftHandGrabbedRb == RightHandGrabbedRb;

        [Header("P1.3 Hand-Over-Hand Hoist State")]
        public bool LastPullWasLeftHand = false;
        public int TotalHandOverHandPulls = 0;

        private CharacterController _cc;
        private Vector3 _velocity;
        private float _pitch;
        private string _joinCodeInput = "SL-";

        // Rope & Tape Multi-Point Selection
        private Transform _pendingRopeAnchorA;
        private Transform _pendingPulleyMidpoint;
        private Collider _pendingTapeColliderA;
        private Vector3 _pendingTapePointA;

        // Live Appraisal Scanner Readout
        private string _hoverScanReadout = "";

        private void Awake()
        {
            Instance = this;
            _cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (PlayerCamera == null)
            {
                GameObject camObj = new GameObject("PlayerHeadCamera");
                camObj.tag = "MainCamera";
                camObj.transform.SetParent(transform, false);
                camObj.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                PlayerCamera = camObj.AddComponent<Camera>();
                PlayerCamera.nearClipPlane = 0.05f;
                camObj.AddComponent<AudioListener>();
            }

            if (LeftToolHandAnchor == null)
            {
                GameObject lHandObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lHandObj.name = "LeftPhysicsHandAnchor";
                Destroy(lHandObj.GetComponent<Collider>());
                lHandObj.transform.SetParent(PlayerCamera.transform, false);
                lHandObj.transform.localPosition = new Vector3(-0.32f, -0.24f, 0.55f);
                lHandObj.transform.localScale = Vector3.one * 0.11f;
                LeftToolHandAnchor = lHandObj.transform;
            }

            if (RightToolHandAnchor == null)
            {
                GameObject rHandObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rHandObj.name = "RightPhysicsHandAnchor";
                Destroy(rHandObj.GetComponent<Collider>());
                rHandObj.transform.SetParent(PlayerCamera.transform, false);
                rHandObj.transform.localPosition = new Vector3(0.32f, -0.24f, 0.55f);
                rHandObj.transform.localScale = Vector3.one * 0.11f;
                RightToolHandAnchor = rHandObj.transform;
            }

            if (EquippedFlashlight == null)
            {
                GameObject lightObj = new GameObject("BeltFlashlightTool");
                lightObj.transform.SetParent(PlayerCamera.transform, false);
                lightObj.transform.localPosition = new Vector3(-0.26f, -0.18f, 0.35f);
                EquippedFlashlight = lightObj.AddComponent<PhysicalFlashlightTool>();
            }

            if (WaistHolster == null)
            {
                WaistHolster = gameObject.AddComponent<WaistBeltHolsterSystem>();
                WaistHolster.HeadCameraTransform = PlayerCamera.transform;
            }
        }

        private void Update()
        {
            HandleCursorAndLobbyToggle();
            if (!ShowLobbyAndShopWindow)
            {
                HandleLookAndMovement();
                HandleToolHotkeys();
                HandlePrimaryToolActions();
            }
            UpdateHoverAppraisalAndFlashlightEffects();
            if (WaistHolster != null)
            {
                WaistHolster.HighlightActiveSlot(ActiveSlot);
            }
        }

        private void FixedUpdate()
        {
            ApplyTwoHandedPDGrabForces();
        }

        /// <summary>
        /// P1.1: Applies independent Left & Right hand PD-Controller forces at exact grab offsets on the Rigidbody.
        /// Grabbing a heavy desk with BOTH hands doubles lift force (2,800N) and enables 90-degree doorway rotation!
        /// </summary>
        private void ApplyTwoHandedPDGrabForces()
        {
            if (PlayerCamera == null) return;

            if (LeftHandGrabbedRb != null)
            {
                Vector3 worldGrabPt = LeftHandGrabbedRb.transform.TransformPoint(LeftHandLocalGrabOffset);
                Vector3 targetPt = PlayerCamera.transform.position
                                   + PlayerCamera.transform.forward * LeftGrabDistance
                                   - PlayerCamera.transform.right * 0.35f;
                ApplySingleHandPDForce(LeftHandGrabbedRb, worldGrabPt, targetPt, 1400f);
            }

            if (RightHandGrabbedRb != null)
            {
                Vector3 worldGrabPt = RightHandGrabbedRb.transform.TransformPoint(RightHandLocalGrabOffset);
                Vector3 targetPt = PlayerCamera.transform.position
                                   + PlayerCamera.transform.forward * RightGrabDistance
                                   + PlayerCamera.transform.right * 0.35f;
                ApplySingleHandPDForce(RightHandGrabbedRb, worldGrabPt, targetPt, 1400f);
            }
        }

        private void ApplySingleHandPDForce(Rigidbody rb, Vector3 worldGrabPt, Vector3 targetPt, float maxHandForceN)
        {
            Vector3 posError = targetPt - worldGrabPt;
            Vector3 pointVel = rb.GetPointVelocity(worldGrabPt);
            Vector3 desiredVel = posError * 11f;
            Vector3 force = Vector3.ClampMagnitude((desiredVel - pointVel) * Mathf.Clamp(rb.mass, 8f, 120f) * 6.5f, maxHandForceN);
            rb.AddForceAtPosition(force, worldGrabPt, ForceMode.Force);
        }

        /// <summary>
        /// Programmatic entry point for P1 Two-Handed Grab & 90-Degree Desk Rotation (used by Player & Automated Co-Op Test).
        /// </summary>
        public void PerformTwoHandedGrabAndRotate90(Rigidbody targetDeskRb)
        {
            if (targetDeskRb == null) return;
            LeftHandGrabbedRb = targetDeskRb;
            RightHandGrabbedRb = targetDeskRb;
            LeftHandLocalGrabOffset = new Vector3(-0.45f, 0f, 0f);
            RightHandLocalGrabOffset = new Vector3(0.45f, 0f, 0f);

            NetworkedPhysicsObject netObj = targetDeskRb.GetComponent<NetworkedPhysicsObject>();
            if (netObj != null)
            {
                netObj.RegisterGrab(SubLeaseNetworkManager.Instance != null ? SubLeaseNetworkManager.Instance.LocalPlayerId : 1);
            }

            // Rotate 90 degrees onto its side so a 1.8m Mahogany Desk fits through narrow doorways!
            targetDeskRb.MoveRotation(targetDeskRb.rotation * Quaternion.Euler(0f, 0f, 90f));
        }

        private void HandleCursorAndLobbyToggle()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ShowLobbyAndShopWindow = !ShowLobbyAndShopWindow;
                Cursor.lockState = ShowLobbyAndShopWindow ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = ShowLobbyAndShopWindow;
            }
        }

        private void HandleLookAndMovement()
        {
            float mouseX = Input.GetAxis("Mouse X") * 2.2f;
            float mouseY = Input.GetAxis("Mouse Y") * 2.2f;

            transform.Rotate(Vector3.up * mouseX);
            _pitch = Mathf.Clamp(_pitch - mouseY, -88f, 88f);
            if (PlayerCamera != null)
            {
                PlayerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            // Toggle Waist Brake Carabiner Lock-Off inside vertical rappel tubes with [R]!
            if (Input.GetKeyDown(KeyCode.R))
            {
                IsCarabinerLockedMidAir = !IsCarabinerLockedMidAir;
                if (IsCarabinerLockedMidAir)
                {
                    _velocity = Vector3.zero;
                }
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 move = (transform.right * h + transform.forward * v).normalized;
            float speed = Input.GetKey(KeyCode.LeftShift) ? SprintSpeed : WalkSpeed;

            if (IsCarabinerLockedMidAir)
            {
                float verticalClimb = 0f;
                if (Input.GetKey(KeyCode.Space)) verticalClimb = 5.5f;
                else if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl)) verticalClimb = -6.5f;

                _cc.Move((move * (speed * 0.65f) + Vector3.up * verticalClimb) * Time.deltaTime);
                return;
            }

            if (_cc.isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }

            if (_cc.isGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                _velocity.y = Mathf.Sqrt(1.6f * -2f * Gravity);
            }

            _velocity.y += Gravity * Time.deltaTime;
            _cc.Move((move * speed + _velocity) * Time.deltaTime);
        }

        private void HandleToolHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ActiveSlot = PlayerToolSlot.Slot1_PhysicsHandsAndHardware;
            if (Input.GetKeyDown(KeyCode.Alpha2)) ActiveSlot = PlayerToolSlot.Slot2_RopeAndPulleyRig;
            if (Input.GetKeyDown(KeyCode.Alpha3)) ActiveSlot = PlayerToolSlot.Slot3_GrapplingHookLauncher;
            if (Input.GetKeyDown(KeyCode.Alpha4)) ActiveSlot = PlayerToolSlot.Slot4_DuctTapeDispenser;
            if (Input.GetKeyDown(KeyCode.Alpha5)) ActiveSlot = PlayerToolSlot.Slot5_FlashlightAndUVScanner;
            if (Input.GetKeyDown(KeyCode.Alpha6)) ActiveSlot = PlayerToolSlot.Slot6_DigitalScaleAndAudio;

            // [F] Cycles Flashlight Mode
            if (Input.GetKeyDown(KeyCode.F) && EquippedFlashlight != null)
            {
                EquippedFlashlight.CycleMode();
            }

            // [K] Toggles Ratchet One-Way Shaft Cleat (P1.3)
            if (Input.GetKeyDown(KeyCode.K) && RatchetOneWayCleat.Instance != null)
            {
                RatchetOneWayCleat.Instance.IsRatchetEngaged = !RatchetOneWayCleat.Instance.IsRatchetEngaged;
            }

            // [T] Cycles Rope Grade or Duct Tape Variant
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (ActiveSlot == PlayerToolSlot.Slot2_RopeAndPulleyRig || ActiveSlot == PlayerToolSlot.Slot3_GrapplingHookLauncher)
                {
                    SelectedRopeGrade = (RopeGrade)(((int)SelectedRopeGrade + 1) % 4);
                }
                else if (ActiveSlot == PlayerToolSlot.Slot4_DuctTapeDispenser)
                {
                    SelectedTapeVariant = (TapeVariant)(((int)SelectedTapeVariant + 1) % 5);
                }
            }

            // [E] Cash out furniture on Cargo Scale!
            if (Input.GetKeyDown(KeyCode.E) && CargoScaleExtractionZone.Instance != null)
            {
                CargoScaleExtractionZone.Instance.CashOutScaleHaul();
            }

            // [H] Blast 135 dB Compressed Air-Horn
            if (Input.GetKeyDown(KeyCode.H) && AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(transform.position, 135f, SoundSourceType.GadgetTimerOrHorn, gameObject);
            }
        }

        private void HandlePrimaryToolActions()
        {
            if (PlayerCamera == null) return;
            Ray ray = new Ray(PlayerCamera.transform.position, PlayerCamera.transform.forward);

            // P1.2: Check if player is looking down at a Physical Waist Belt Holster Pouch and clicks it!
            if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out RaycastHit beltHit, 2.0f))
            {
                HolsterPouchInteractable pouch = beltHit.collider.GetComponent<HolsterPouchInteractable>();
                if (pouch != null)
                {
                    ActiveSlot = pouch.AssignedSlot;
                    return;
                }
            }

            // Adjust grab distance & 90-degree two-handed rotation ([Z]/[X])
            if (LeftHandGrabbedRb != null || RightHandGrabbedRb != null)
            {
                float scroll = Input.mouseScrollDelta.y * 0.4f;
                LeftGrabDistance = Mathf.Clamp(LeftGrabDistance + scroll, 1.2f, 6.0f);
                RightGrabDistance = Mathf.Clamp(RightGrabDistance + scroll, 1.2f, 6.0f);

                // P1.1: When holding with BOTH hands, press [Z] or [X] to physically roll/rotate wide desks 90 degrees!
                if (IsTwoHandedGrabActive)
                {
                    if (Input.GetKeyDown(KeyCode.Z))
                    {
                        LeftHandGrabbedRb.MoveRotation(LeftHandGrabbedRb.rotation * Quaternion.Euler(0f, 0f, 90f));
                    }
                    else if (Input.GetKeyDown(KeyCode.X))
                    {
                        LeftHandGrabbedRb.MoveRotation(LeftHandGrabbedRb.rotation * Quaternion.Euler(0f, 90f, 0f));
                    }
                }
            }

            if (Input.GetMouseButtonUp(0) && LeftHandGrabbedRb != null)
            {
                NetworkedPhysicsObject netObj = LeftHandGrabbedRb.GetComponent<NetworkedPhysicsObject>();
                if (netObj != null && RightHandGrabbedRb != LeftHandGrabbedRb) netObj.UnregisterGrab(1);
                LeftHandGrabbedRb = null;
            }
            if (Input.GetMouseButtonUp(1) && RightHandGrabbedRb != null)
            {
                NetworkedPhysicsObject netObj = RightHandGrabbedRb.GetComponent<NetworkedPhysicsObject>();
                if (netObj != null && LeftHandGrabbedRb != RightHandGrabbedRb) netObj.UnregisterGrab(1);
                RightHandGrabbedRb = null;
            }

            switch (ActiveSlot)
            {
                case PlayerToolSlot.Slot1_PhysicsHandsAndHardware:
                    // Left-Click = Left Hand Grab | Right-Click = Right Hand Grab (Both held = Two-Handed Heavy Lift!)
                    if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out RaycastHit lHit, 4.8f) && lHit.rigidbody != null && !lHit.rigidbody.isKinematic)
                    {
                        LeftHandGrabbedRb = lHit.rigidbody;
                        LeftGrabDistance = lHit.distance;
                        LeftHandLocalGrabOffset = lHit.rigidbody.transform.InverseTransformPoint(lHit.point);
                        RegisterGrabAndCheckMimic(LeftHandGrabbedRb);
                    }
                    if (Input.GetMouseButtonDown(1) && Physics.Raycast(ray, out RaycastHit rHit, 4.8f) && rHit.rigidbody != null && !rHit.rigidbody.isKinematic)
                    {
                        RightHandGrabbedRb = rHit.rigidbody;
                        RightGrabDistance = rHit.distance;
                        RightHandLocalGrabOffset = rHit.rigidbody.transform.InverseTransformPoint(rHit.point);
                        RegisterGrabAndCheckMimic(RightHandGrabbedRb);
                    }

                    // [G] Attach 4x Swivel Caster Wheels
                    if (Input.GetKeyDown(KeyCode.G) && Physics.Raycast(ray, out RaycastHit wheelHit, 4.5f))
                    {
                        NetworkedPhysicsObject net = wheelHit.collider.GetComponentInParent<NetworkedPhysicsObject>();
                        if (net != null)
                        {
                            net.ApplyCasterWheels();
                            LootFurnitureItem loot = net.GetComponent<LootFurnitureItem>();
                            if (loot != null) loot.UpdatePriceTagText();
                        }
                    }
                    // [B] Attach Inflatable CO2 Lift Bag (+95kg upward lift!)
                    if (Input.GetKeyDown(KeyCode.B) && Physics.Raycast(ray, out RaycastHit bagHit, 4.5f) && bagHit.rigidbody != null)
                    {
                        DuctTapeAndGadgetFactory.AttachInflatableLiftBag(bagHit.rigidbody);
                    }
                    break;

                case PlayerToolSlot.Slot2_RopeAndPulleyRig:
                    if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out RaycastHit ropeHit, 18f))
                    {
                        TetherAnchor anchor = ropeHit.collider.GetComponentInParent<TetherAnchor>();
                        Transform hitAnchorTransform = anchor != null ? anchor.transform : CreateSurfaceAnchor(ropeHit.point, ropeHit.collider.transform);

                        if (_pendingRopeAnchorA == null)
                        {
                            _pendingRopeAnchorA = hitAnchorTransform;
                        }
                        else if (anchor != null && anchor.IsOverheadPulley && _pendingPulleyMidpoint == null)
                        {
                            _pendingPulleyMidpoint = anchor.transform;
                        }
                        else
                        {
                            GameObject ropeObj = new GameObject($"Rope_{SelectedRopeGrade}");
                            ActiveTetherRope tether = ropeObj.AddComponent<ActiveTetherRope>();
                            tether.InitializeTether(_pendingRopeAnchorA, hitAnchorTransform, SelectedRopeGrade, _pendingPulleyMidpoint);
                            _pendingRopeAnchorA = null;
                            _pendingPulleyMidpoint = null;
                        }
                    }

                    // P1.3: Hand-Over-Hand Pulley Hoisting (Scroll Wheel or alternating [Q]/[E] hand pulls)
                    if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f || Input.GetKeyDown(KeyCode.Q))
                    {
                        PerformHandOverHandPulleyPull(0.65f);
                    }

                    // Right-click Box Cutter: Instantly sever nearest rope
                    if (Input.GetMouseButtonDown(1))
                    {
                        ActiveTetherRope[] ropes = FindObjectsByType<ActiveTetherRope>(FindObjectsSortMode.None);
                        if (ropes.Length > 0) ropes[ropes.Length - 1].SnapAndDestroy();
                    }
                    break;

                case PlayerToolSlot.Slot3_GrapplingHookLauncher:
                    if (Input.GetMouseButtonDown(0))
                    {
                        FireGrapplingHook();
                    }
                    break;

                case PlayerToolSlot.Slot4_DuctTapeDispenser:
                    if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out RaycastHit tapeHit, 5.0f))
                    {
                        if (_pendingTapeColliderA == null)
                        {
                            _pendingTapeColliderA = tapeHit.collider;
                            _pendingTapePointA = tapeHit.point;
                        }
                        else
                        {
                            DuctTapeAndGadgetFactory.ApplyTapeBetween(_pendingTapeColliderA, _pendingTapePointA, tapeHit.collider, tapeHit.point, SelectedTapeVariant);
                            LootFurnitureItem lootA = _pendingTapeColliderA.GetComponentInParent<LootFurnitureItem>();
                            if (lootA != null) lootA.UpdatePriceTagText();
                            LootFurnitureItem lootB = tapeHit.collider.GetComponentInParent<LootFurnitureItem>();
                            if (lootB != null) lootB.UpdatePriceTagText();
                            _pendingTapeColliderA = null;
                        }
                    }
                    break;

                case PlayerToolSlot.Slot5_FlashlightAndUVScanner:
                    if (Input.GetMouseButtonDown(0) && EquippedFlashlight != null)
                    {
                        EquippedFlashlight.CycleMode();
                    }
                    break;
            }
        }

        public void PerformHandOverHandPulleyPull(float pullMeters)
        {
            LastPullWasLeftHand = !LastPullWasLeftHand;
            TotalHandOverHandPulls++;

            // Multiply pull distance if Co-Op Crewmates are connected!
            int crewCount = (LobbyManager.Instance != null) ? Mathf.Max(1, LobbyManager.Instance.CrewRoster.Count) : 1;
            float effectivePull = pullMeters * (1f + (crewCount - 1) * 0.5f);

            foreach (var r in FindObjectsByType<ActiveTetherRope>(FindObjectsSortMode.None))
            {
                r.ShortenOrHoistRope(effectivePull);
            }

            if (SubLeaseNetworkManager.Instance != null && SubLeaseNetworkManager.Instance.IsConnected)
            {
                SubLeaseNetworkManager.Instance.BroadcastRpc("HOIST_PULL", $"{effectivePull:F2}");
            }
        }

        private void RegisterGrabAndCheckMimic(Rigidbody rb)
        {
            NetworkedPhysicsObject netObj = rb.GetComponent<NetworkedPhysicsObject>();
            if (netObj != null) netObj.RegisterGrab(1);

            LootFurnitureItem loot = rb.GetComponent<LootFurnitureItem>();
            if (loot != null && loot.IsDisguisedMimic && !loot.IsMimicSafelyTaped())
            {
                loot.TriggerMimicWakeUp();
            }
        }

        private void FireGrapplingHook()
        {
            GameObject hook = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hook.name = "GrappleClaw_Projectile";
            hook.transform.position = RightToolHandAnchor.position + PlayerCamera.transform.forward * 0.4f;
            hook.transform.localScale = Vector3.one * 0.18f;

            Rigidbody rb = hook.AddComponent<Rigidbody>();
            rb.mass = 3.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(PlayerCamera.transform.forward * 95f + Vector3.up * 8f, ForceMode.Impulse);

            GrappleHookProjectile proj = hook.AddComponent<GrappleHookProjectile>();
            proj.OriginAnchor = RightToolHandAnchor;
            proj.AttachedRopeGrade = SelectedRopeGrade;
        }

        private Transform CreateSurfaceAnchor(Vector3 worldPt, Transform parent)
        {
            GameObject a = new GameObject("SurfacePitonAnchor");
            a.transform.position = worldPt;
            a.transform.SetParent(parent, true);
            a.AddComponent<TetherAnchor>();
            return a.transform;
        }

        private void UpdateHoverAppraisalAndFlashlightEffects()
        {
            _hoverScanReadout = "";
            if (PlayerCamera == null) return;

            bool isUV = EquippedFlashlight != null && EquippedFlashlight.IsSwitchedOn &&
                        EquippedFlashlight.CurrentMode == PhysicalFlashlightTool.LightMode.UVBlacklightInspector;
            bool isHalogen = EquippedFlashlight != null && EquippedFlashlight.IsSwitchedOn &&
                             EquippedFlashlight.CurrentMode == PhysicalFlashlightTool.LightMode.HighLumenHalogenFlood;

            Ray ray = new Ray(PlayerCamera.transform.position, PlayerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 18f))
            {
                LootFurnitureItem loot = hit.collider.GetComponentInParent<LootFurnitureItem>();
                if (loot != null)
                {
                    loot.SetUVHighlight(isUV);
                    float trueMass = loot.NetPhysics != null ? loot.NetPhysics.TrueSignedMassKg : 15f;
                    string mimicWarn = (ActiveSlot == PlayerToolSlot.Slot6_DigitalScaleAndAudio && loot.IsDisguisedMimic)
                        ? " [WARNING: SCALE READS 95.0 KG - MIMIC DETECTED!]"
                        : "";
                    _hoverScanReadout = $"SCAN: {loot.ItemDisplayName} | Value: ${loot.CurrentDollarValue} | Scale Mass: {trueMass:F1} kg{mimicWarn}";
                }

                if (isHalogen)
                {
                    EntityPhysicsController entity = hit.collider.GetComponentInParent<EntityPhysicsController>();
                    if (entity != null)
                    {
                        entity.ReactToHighLumenLight(PlayerCamera.transform.position);
                    }
                }
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(14, 14, 540, 165), "");
            GUILayout.BeginArea(new Rect(24, 20, 520, 155));
            if (LobbyManager.Instance != null)
            {
                GUILayout.Label($"<b>SUB-LEASE (P1) // ROOM CODE: {LobbyManager.Instance.CurrentRoomCode}</b>  |  Crew: {LobbyManager.Instance.CrewRoster.Count}/4  |  Cash: <b>${LobbyManager.Instance.BankedCash}</b> / Quota: ${LobbyManager.Instance.CurrentQuotaTarget}");
            }
            GUILayout.Label($"<b>Tool [1-6 / Waist Belt]:</b> {ActiveSlot}   |   <b>Two-Hand Grab (L+R Click):</b> {(IsTwoHandedGrabActive ? "ACTIVE (2,800N | [Z]/[X] 90° Desk Roll)" : "Single/Idle")}");
            bool ratchetOn = RatchetOneWayCleat.Instance != null && RatchetOneWayCleat.Instance.IsRatchetEngaged;
            GUILayout.Label($"<b>Flashlight [F]:</b> {(EquippedFlashlight != null ? EquippedFlashlight.CurrentMode.ToString() : "Off")}   |   <b>Waist Rappel [R]:</b> {(IsCarabinerLockedMidAir ? "LOCKED" : "Free")}   |   <b>Ratchet Cleat [K]:</b> {(ratchetOn ? "LOCKED ONE-WAY" : "FREE")}");
            if (AcousticAndPhysicsBus.Instance != null)
            {
                GUILayout.Label($"<b>Acoustic Meter:</b> {AcousticAndPhysicsBus.Instance.LastPeakDecibels:F0} dB   |   <b>[G]</b> Wheels  <b>[B]</b> Lift Bag  <b>[H]</b> Air-Horn  <b>[Tab]</b> Co-Op Lobby");
            }
            if (!string.IsNullOrEmpty(_hoverScanReadout))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(_hoverScanReadout);
                GUI.color = Color.white;
            }
            GUILayout.EndArea();

            if (!ShowLobbyAndShopWindow)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 6f, Screen.height * 0.5f - 10f, 24f, 24f), "+");
                return;
            }

            Rect winRect = new Rect(Screen.width * 0.5f - 290f, Screen.height * 0.5f - 220f, 580f, 440f);
            GUI.Box(winRect, "=== SUB-LEASE: CO-OP LOBBY & DROP HATCH TERMINAL (0m) ===");
            GUILayout.BeginArea(new Rect(winRect.x + 18f, winRect.y + 32f, winRect.width - 36f, winRect.height - 44f));

            if (LobbyManager.Instance != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host New Room Code", GUILayout.Height(28)))
                {
                    LobbyManager.Instance.HostNewLobby("Liquidator-Host");
                }
                _joinCodeInput = GUILayout.TextField(_joinCodeInput, 12, GUILayout.Width(110), GUILayout.Height(28));
                if (GUILayout.Button("Join By Code / LAN", GUILayout.Height(28)))
                {
                    LobbyManager.Instance.JoinLobbyByCodeOrIp(_joinCodeInput);
                }
                if (GUILayout.Button("+ Spawn Co-Op Crewmate Bot", GUILayout.Height(28)))
                {
                    LobbyManager.Instance.SpawnTestCoOpCrewmate();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label($"<b>ACTIVE CREW ROSTER (Room Code: {LobbyManager.Instance.CurrentRoomCode}):</b>");
                foreach (var member in LobbyManager.Instance.CrewRoster)
                {
                    GUILayout.Label($"  • [{member.PlayerId}] {member.DisplayName}  —  Belt: {member.SelectedBelt}  —  {(member.IsReady ? "READY" : "STAGING")}");
                }

                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Cycle Work Belt Role", GUILayout.Height(26)))
                {
                    LobbyManager.Instance.CycleLocalPlayerBelt();
                }
                if (GUILayout.Button("Cash Out Cargo Scale Haul", GUILayout.Height(26)) && CargoScaleExtractionZone.Instance != null)
                {
                    CargoScaleExtractionZone.Instance.CashOutScaleHaul();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label("<b>DROP HATCH EQUIPMENT TERMINAL (Buy Upgrades With Furniture Cash):</b>");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Buy Braided Steel Cable ($650) [6,000N]") && LobbyManager.Instance.BankedCash >= 650)
                {
                    LobbyManager.Instance.BankedCash -= 650;
                    SelectedRopeGrade = RopeGrade.BraidedSteel_6000N;
                }
                if (GUILayout.Button("Buy Kevlar Tape ($280) [Capture Mimics]") && LobbyManager.Instance.BankedCash >= 280)
                {
                    LobbyManager.Instance.BankedCash -= 280;
                    SelectedTapeVariant = TapeVariant.KevlarStrapping_4500N;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(14);
                GUI.backgroundColor = new Color(0.25f, 0.85f, 0.35f);
                if (GUILayout.Button("LOCK CURSOR & ENTER THE DROP HATCH [Tab]", GUILayout.Height(38)))
                {
                    ShowLobbyAndShopWindow = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    LobbyManager.Instance.StartHeistShift();
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndArea();
        }
    }
}
