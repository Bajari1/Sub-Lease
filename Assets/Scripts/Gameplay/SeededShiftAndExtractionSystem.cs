using System.Collections.Generic;
using SubLease.Core;
using SubLease.Networking;
using UnityEngine;

namespace SubLease.Gameplay
{
    /// <summary>
    /// Priority P6: Seeded Procedural Shift Loop, Physical Hydraulic Extraction Lever,
    /// Multi-Day Quota Progression, and "The Abyssal Eviction" failure sequence.
    /// </summary>
    public class SeededShiftAndExtractionSystem : MonoBehaviour
    {
        public static SeededShiftAndExtractionSystem Instance { get; private set; }

        [Header("Seeded Procedural Run State")]
        public uint CurrentShiftSeed = 482019u;
        public int CompletedQuotaTiers = 0;
        public int TotalShiftsExtracted = 0;
        public float SeededDoorwayWidthMeters = 1.32f;
        public Vector3 LastSeededVaultLootPosition;
        public GameObject ActiveSeededBonusLoot;

        [Header("0m Physical Hydraulic Extraction Lever")]
        public Transform LeverHandlePivot;
        public Transform BlastDoorShutter;
        public TextMesh LeverReadoutText;
        public bool IsBlastDoorSealed = false;
        public bool IsAbyssalEvictionTriggered = false;

        private readonly List<GameObject> _dropHatchFloorSlabs = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildHydraulicExtractionLeverAtDropHatch();
            ApplySeededShiftPermutation(CurrentShiftSeed);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                PullExtractionLever(true);
            }
        }

        public void BuildHydraulicExtractionLeverAtDropHatch()
        {
            GameObject stationRoot = new GameObject("DropHatch_HydraulicExtractionStation");
            stationRoot.transform.SetParent(transform, false);
            stationRoot.transform.position = new Vector3(-2.3f, 0f, 2.75f);

            // Pedestal Column
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "HydraulicLeverPedestal";
            pedestal.transform.SetParent(stationRoot.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            pedestal.transform.localScale = new Vector3(0.45f, 1.3f, 0.45f);
            ApplyStationColor(pedestal, new Color(0.22f, 0.24f, 0.26f));

            // Rotating Lever Pivot & Red Handle
            GameObject pivotObj = new GameObject("LeverHandlePivot");
            pivotObj.transform.SetParent(stationRoot.transform, false);
            pivotObj.transform.localPosition = new Vector3(0f, 1.18f, -0.22f);
            pivotObj.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            LeverHandlePivot = pivotObj.transform;

            GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = "HydraulicLeverRod";
            rod.transform.SetParent(pivotObj.transform, false);
            rod.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            rod.transform.localScale = new Vector3(0.05f, 0.25f, 0.05f);
            ApplyStationColor(rod, new Color(0.85f, 0.15f, 0.12f));

            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "HydraulicLeverTBar";
            grip.transform.SetParent(pivotObj.transform, false);
            grip.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            grip.transform.localScale = new Vector3(0.28f, 0.07f, 0.07f);
            ApplyStationColor(grip, new Color(0.95f, 0.25f, 0.15f));

            // Blast Door Shutter over the 0m Drop Hatch (3.6m x 3.6m)
            GameObject shutter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shutter.name = "DropHatch_BlastDoorShutter";
            shutter.transform.SetParent(transform, false);
            shutter.transform.position = new Vector3(0f, 0.08f, 4.4f); // Retracted behind hatch initially
            shutter.transform.localScale = new Vector3(3.55f, 0.12f, 3.55f);
            shutter.GetComponent<Collider>().isTrigger = true; // Non-blocking when retracted
            ApplyStationColor(shutter, new Color(0.35f, 0.32f, 0.18f));
            BlastDoorShutter = shutter.transform;

            // 3D Status Board above Lever
            GameObject textObj = new GameObject("ExtractionLeverReadout");
            textObj.transform.SetParent(stationRoot.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            LeverReadoutText = textObj.AddComponent<TextMesh>();
            LeverReadoutText.characterSize = 0.052f;
            LeverReadoutText.fontSize = 36;
            LeverReadoutText.anchor = TextAnchor.MiddleCenter;
            LeverReadoutText.alignment = TextAlignment.Center;
            LeverReadoutText.color = new Color(1.0f, 0.85f, 0.3f);

            UpdateLeverReadout();
        }

        /// <summary>
        /// Deterministically permutes doorway bottleneck widths, furniture coordinates, and spawns
        /// a seeded high-value anomaly vault prop so Host and Clients share identical procedural runs.
        /// </summary>
        public void ApplySeededShiftPermutation(uint shiftSeed)
        {
            CurrentShiftSeed = shiftSeed == 0 ? 1u : shiftSeed;
            uint rng = CurrentShiftSeed;

            // Deterministic Xorshift32 step
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            float normA = (rng & 0xFFFF) / 65535f;

            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            float normB = (rng & 0xFFFF) / 65535f;

            // Vary doorway bottleneck width between 1.18m and 1.48m (requires 90° rotation for 1.8m desks!)
            SeededDoorwayWidthMeters = Mathf.Lerp(1.18f, 1.48f, normA);

            // Spawn or reposition the Seeded Shift Vault Artifact at a deterministic coordinate in Sector 1
            if (ActiveSeededBonusLoot != null)
            {
                Destroy(ActiveSeededBonusLoot);
            }

            LastSeededVaultLootPosition = new Vector3(
                Mathf.Lerp(-3.8f, 3.8f, normA),
                -16.9f,
                Mathf.Lerp(4.2f, 7.6f, normB)
            );

            ActiveSeededBonusLoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ActiveSeededBonusLoot.name = $"Loot_Seeded Executive Vault Safe #{CurrentShiftSeed % 1000}";
            ActiveSeededBonusLoot.transform.position = LastSeededVaultLootPosition;
            ActiveSeededBonusLoot.transform.localScale = new Vector3(0.75f, 0.85f, 0.75f);
            ApplyStationColor(ActiveSeededBonusLoot, new Color(0.78f, 0.66f, 0.22f));

            Rigidbody rb = ActiveSeededBonusLoot.AddComponent<Rigidbody>();
            rb.mass = 65f;

            NetworkedPhysicsObject netObj = ActiveSeededBonusLoot.AddComponent<NetworkedPhysicsObject>();
            netObj.TrueSignedMassKg = 65f;

            LootFurnitureItem loot = ActiveSeededBonusLoot.AddComponent<LootFurnitureItem>();
            loot.ItemDisplayName = $"Seeded Vault Safe #{CurrentShiftSeed % 1000}";
            loot.BaseDollarValue = 1650;
            loot.CurrentDollarValue = 1650;
            loot.FragilityMultiplier = 0.4f;
            loot.UpdatePriceTagText();

            UpdateLeverReadout();
            Debug.Log($"[SeededShiftSystem] Applied ShiftSeed={CurrentShiftSeed} | BottleneckDoorWidth={SeededDoorwayWidthMeters:F2}m | VaultPos={LastSeededVaultLootPosition}");
        }

        /// <summary>
        /// Pulls the physical 0m Hydraulic Extraction Lever:
        /// 1. Cashes out all furniture on CargoScaleExtractionZone
        /// 2. Advances the Shift Day (Day 1 -> 2 -> 3)
        /// 3. Evaluates Quota on Day 3 (advances Quota Tier or triggers Abyssal Eviction)
        /// 4. Broadcasts SHIFT_ADVANCE RPC to synchronize all connected Co-Op clients.
        /// </summary>
        public void PullExtractionLever(bool broadcastRpc = true)
        {
            if (LobbyManager.Instance == null) return;

            TotalShiftsExtracted++;
            IsBlastDoorSealed = true;

            if (LeverHandlePivot != null)
            {
                LeverHandlePivot.localRotation = Quaternion.Euler(42f, 0f, 0f);
            }
            if (BlastDoorShutter != null)
            {
                BlastDoorShutter.position = new Vector3(0f, 0.08f, 0f);
            }

            // Cash out any furniture currently resting on the 0m Cargo Extraction Scale
            if (CargoScaleExtractionZone.Instance != null)
            {
                CargoScaleExtractionZone.Instance.RecalculateScale();
                if (CargoScaleExtractionZone.Instance.TotalScaleDollarValue > 0)
                {
                    LobbyManager.Instance.BankedCash += CargoScaleExtractionZone.Instance.TotalScaleDollarValue;
                    foreach (var item in CargoScaleExtractionZone.Instance.ItemsOnScale)
                    {
                        if (item != null) Destroy(item.gameObject);
                    }
                    CargoScaleExtractionZone.Instance.ItemsOnScale.Clear();
                    CargoScaleExtractionZone.Instance.RecalculateScale();
                }
            }

            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(new Vector3(-2.3f, 1.2f, 2.75f), 95f, SoundSourceType.MotorizedWinch, gameObject);
            }

            // Check if Quota is met or if Day 3 has concluded
            if (LobbyManager.Instance.BankedCash >= LobbyManager.Instance.CurrentQuotaTarget)
            {
                CompletedQuotaTiers++;
                LobbyManager.Instance.CurrentDay = 1;
                LobbyManager.Instance.CurrentQuotaTarget = Mathf.RoundToInt(LobbyManager.Instance.CurrentQuotaTarget * 1.55f);
                IsAbyssalEvictionTriggered = false;
                Debug.Log($"[SeededShiftSystem] QUOTA TIER #{CompletedQuotaTiers} MET! Next 3-Day Quota Target: ${LobbyManager.Instance.CurrentQuotaTarget}");
            }
            else if (LobbyManager.Instance.CurrentDay >= LobbyManager.Instance.DaysPerQuota)
            {
                // Failed 3-Day Quota -> Trigger "The Abyssal Eviction"!
                TriggerAbyssalEviction();
            }
            else
            {
                LobbyManager.Instance.CurrentDay++;
            }

            // Generate next deterministic Shift Seed and permute layout
            uint nextSeed = CurrentShiftSeed * 1664525u + 1013904223u;
            ApplySeededShiftPermutation(nextSeed);

            if (broadcastRpc && SubLeaseNetworkManager.Instance != null)
            {
                SubLeaseNetworkManager.Instance.BroadcastShiftAdvanceRpc(
                    CurrentShiftSeed,
                    LobbyManager.Instance.CurrentDay,
                    LobbyManager.Instance.BankedCash,
                    LobbyManager.Instance.CurrentQuotaTarget
                );
            }

            UpdateLeverReadout();
        }

        public void SyncShiftStateFromNetwork(uint seed, int day, int bankedCash, int quotaTarget)
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.CurrentDay = day;
                LobbyManager.Instance.BankedCash = bankedCash;
                LobbyManager.Instance.CurrentQuotaTarget = quotaTarget;
            }
            TotalShiftsExtracted++;
            ApplySeededShiftPermutation(seed);
            UpdateLeverReadout();
        }

        public void TriggerAbyssalEviction()
        {
            IsAbyssalEvictionTriggered = true;
            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(Vector3.zero, 125f, SoundSourceType.GadgetTimerOrHorn, gameObject);
            }

            // Unlatch Drop Hatch floor slabs so they fall into the abyssal shaft when evicted
            foreach (GameObject slab in _dropHatchFloorSlabs)
            {
                if (slab == null) continue;
                Rigidbody rb = slab.GetComponent<Rigidbody>();
                if (rb == null) rb = slab.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // Reset to Day 1 after eviction penalty
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.CurrentDay = 1;
                LobbyManager.Instance.BankedCash = Mathf.Max(250, LobbyManager.Instance.BankedCash / 2);
            }

            UpdateLeverReadout();
            Debug.Log("[SeededShiftSystem] !!! ABYSSAL EVICTION TRIGGERED !!! Crew failed 3-Day Quota — Drop Hatch floor unlatched into -74m shaft!");
        }

        public void UpdateLeverReadout()
        {
            if (LeverReadoutText == null || LobbyManager.Instance == null) return;
            string evictionBanner = IsAbyssalEvictionTriggered ? "\n[WARNING: ABYSSAL EVICTION EXECUTED!]" : "";
            LeverReadoutText.text =
                $"=== HYDRAULIC SHIFT EXTRACTION LEVER ===\n" +
                $"SHIFT DAY: {LobbyManager.Instance.CurrentDay}/{LobbyManager.Instance.DaysPerQuota} | SEED: #{CurrentShiftSeed}\n" +
                $"BANKED: ${LobbyManager.Instance.BankedCash} / QUOTA: ${LobbyManager.Instance.CurrentQuotaTarget}\n" +
                $"QUOTA TIERS COMPLETED: {CompletedQuotaTiers} | [Press L to Pull Lever]{evictionBanner}";
        }

        private void ApplyStationColor(GameObject obj, Color col)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default"));
                mat.color = col;
                r.material = mat;
            }
        }
    }
}
