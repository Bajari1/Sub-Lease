using System.Collections.Generic;
using SubLease.Core;
using SubLease.Networking;
using UnityEngine;

namespace SubLease.Gameplay
{
    /// <summary>
    /// Attached to every stealable piece of vintage furniture or anomalous loot.
    /// Tracks dollar value, dent penalties on collision, Grandfather Clock tilt chimes, and Mimic status.
    /// </summary>
    [RequireComponent(typeof(NetworkedPhysicsObject))]
    public class LootFurnitureItem : MonoBehaviour
    {
        [Header("Loot Appraisal Stats")]
        public string ItemDisplayName = "Executive Mahogany Desk";
        public int BaseDollarValue = 750;
        public int CurrentDollarValue = 750;
        [Range(0f, 100f)] public float ConditionPercent = 100f;
        [Tooltip("Higher fragility means hard impacts dent more dollar value unless wrapped in Bubble-Wrap.")]
        public float FragilityMultiplier = 1.0f;

        [Header("Special Loot Quirks")]
        public bool IsGrandfatherClock = false;
        public bool IsClockPendulumTaped = false;
        public bool IsDisguisedMimic = false;
        public bool IsMimicAwake = false;

        public NetworkedPhysicsObject NetPhysics { get; private set; }
        private float _lastChimeTime;
        private TextMesh _floatingPriceTag;
        private Renderer _primaryRenderer;
        private Color _originalColor;

        private void Awake()
        {
            NetPhysics = GetComponent<NetworkedPhysicsObject>();
            CurrentDollarValue = BaseDollarValue;
            _primaryRenderer = GetComponentInChildren<Renderer>();
            if (_primaryRenderer != null)
            {
                _originalColor = _primaryRenderer.material.color;
            }
            CreateFloatingPriceTag();
        }

        private void CreateFloatingPriceTag()
        {
            GameObject tagObj = new GameObject("FloatingAppraisalTag");
            tagObj.transform.SetParent(transform, false);
            tagObj.transform.localPosition = Vector3.up * 0.85f;
            _floatingPriceTag = tagObj.AddComponent<TextMesh>();
            _floatingPriceTag.characterSize = 0.065f;
            _floatingPriceTag.fontSize = 38;
            _floatingPriceTag.anchor = TextAnchor.LowerCenter;
            _floatingPriceTag.alignment = TextAlignment.Center;
            UpdatePriceTagText();
        }

        private void Update()
        {
            // Keep price tag facing main camera
            if (_floatingPriceTag != null && Camera.main != null)
            {
                Vector3 dir = _floatingPriceTag.transform.position - Camera.main.transform.position;
                if (dir.sqrMagnitude > 0.01f)
                {
                    _floatingPriceTag.transform.rotation = Quaternion.LookRotation(dir);
                }
            }

            // Grandfather Clock Quirk: Chimes at 105 dB if tilted past 25 degrees without taping its pendulum!
            if (IsGrandfatherClock && !IsClockPendulumTaped && NetPhysics.KevlarTapeStrapsApplied == 0)
            {
                float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
                if (tiltAngle > 25f && Time.time > _lastChimeTime + 2.5f)
                {
                    _lastChimeTime = Time.time;
                    if (AcousticAndPhysicsBus.Instance != null)
                    {
                        AcousticAndPhysicsBus.Instance.EmitSound(transform.position, 105f, SoundSourceType.GrandfatherClockChime, gameObject);
                    }
                    Debug.Log("[GrandfatherClock] BONG! Clock tilted past 25° without taped pendulum (105 dB)!");
                }
            }
        }

        public void SetUVHighlight(bool uvActive)
        {
            if (_primaryRenderer == null) return;
            if (uvActive && IsDisguisedMimic)
            {
                // Mimics glow with unmistakable magenta vascular veins under 365nm UV Blacklight!
                float pulse = 0.6f + Mathf.Sin(Time.time * 8f) * 0.4f;
                _primaryRenderer.material.color = Color.Lerp(_originalColor, new Color(0.95f, 0.1f, 0.85f), pulse);
            }
            else
            {
                _primaryRenderer.material.color = _originalColor;
            }
        }

        public bool IsMimicSafelyTaped()
        {
            return IsDisguisedMimic && NetPhysics != null && NetPhysics.KevlarTapeStrapsApplied >= 4;
        }

        public void TriggerMimicWakeUp()
        {
            if (!IsDisguisedMimic || IsMimicSafelyTaped()) return;
            IsMimicAwake = true;
            UpdatePriceTagText();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsDisguisedMimic && IsMimicSafelyTaped())
            {
                CurrentDollarValue = 2500; // Live Taped Mimic Bounty!
                UpdatePriceTagText();
                return;
            }

            float impulse = collision.impulse.magnitude;
            float damageThreshold = 22f / Mathf.Max(0.2f, FragilityMultiplier);

            if (impulse > damageThreshold && ConditionPercent > 10f)
            {
                float protection = NetPhysics != null ? NetPhysics.ImpactDamageProtectionMultiplier : 1f;
                float rawLossPct = (impulse - damageThreshold) * 0.45f * FragilityMultiplier * protection;
                if (rawLossPct >= 1f)
                {
                    ConditionPercent = Mathf.Max(10f, ConditionPercent - rawLossPct);
                    CurrentDollarValue = Mathf.RoundToInt(BaseDollarValue * (ConditionPercent / 100f));
                    UpdatePriceTagText();
                }
            }
        }

        public void UpdatePriceTagText()
        {
            if (_floatingPriceTag == null) return;

            if (IsDisguisedMimic && IsMimicSafelyTaped())
            {
                _floatingPriceTag.text = $"[TAPED LIVE MIMIC]\n$2,500 | 95.0 kg";
                _floatingPriceTag.color = new Color(1f, 0.35f, 0.9f);
                return;
            }

            if (IsDisguisedMimic && IsMimicAwake)
            {
                _floatingPriceTag.text = $"[HOSTILE MIMIC!]\n95.0 kg | TAPE OR RUN!";
                _floatingPriceTag.color = Color.red;
                return;
            }

            string extras = "";
            if (NetPhysics != null)
            {
                if (NetPhysics.HasCasterWheelsAttached) extras += " [WHEELS]";
                if (NetPhysics.ImpactDamageProtectionMultiplier < 0.5f) extras += " [BUBBLE-WRAP]";
                if (NetPhysics.AcousticCollisionMultiplier < 0.5f) extras += " [SILENCED]";
            }

            // Note: Mimics hide their true 95kg weight on visual tags until weighed on the Digital Scale or Cargo Scale!
            float shownMass = IsDisguisedMimic ? 12f : (NetPhysics != null ? NetPhysics.TrueSignedMassKg : 15f);
            _floatingPriceTag.text = $"{ItemDisplayName}{extras}\n${CurrentDollarValue} ({ConditionPercent:F0}%) | {shownMass:F0} kg";
            _floatingPriceTag.color = ConditionPercent > 75f
                ? new Color(0.35f, 1.0f, 0.45f)
                : (ConditionPercent > 40f ? Color.yellow : new Color(1f, 0.4f, 0.2f));
        }
    }

    /// <summary>
    /// Physical Cargo Scale platform at 0m (next to the Drop Hatch).
    /// Weighs all furniture resting on it, exposes exact mass (including 95kg Mimics!), and cashes out toward Quota.
    /// </summary>
    public class CargoScaleExtractionZone : MonoBehaviour
    {
        public static CargoScaleExtractionZone Instance { get; private set; }

        public List<LootFurnitureItem> ItemsOnScale = new List<LootFurnitureItem>();
        public int TotalScaleDollarValue;
        public float TotalScaleMassKg;
        public TextMesh ScaleDisplayBoard;

        private void Awake()
        {
            Instance = this;
        }

        private void OnTriggerEnter(Collider other)
        {
            LootFurnitureItem item = other.GetComponentInParent<LootFurnitureItem>();
            if (item != null && !ItemsOnScale.Contains(item))
            {
                ItemsOnScale.Add(item);
                RecalculateScale();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            LootFurnitureItem item = other.GetComponentInParent<LootFurnitureItem>();
            if (item != null && ItemsOnScale.Contains(item))
            {
                ItemsOnScale.Remove(item);
                RecalculateScale();
            }
        }

        public void RecalculateScale()
        {
            TotalScaleDollarValue = 0;
            TotalScaleMassKg = 0f;

            for (int i = ItemsOnScale.Count - 1; i >= 0; i--)
            {
                LootFurnitureItem item = ItemsOnScale[i];
                if (item == null)
                {
                    ItemsOnScale.RemoveAt(i);
                    continue;
                }

                if (item.IsDisguisedMimic && item.IsMimicSafelyTaped())
                {
                    item.CurrentDollarValue = 2500;
                }

                TotalScaleDollarValue += item.CurrentDollarValue;
                TotalScaleMassKg += item.NetPhysics != null ? Mathf.Abs(item.NetPhysics.TrueSignedMassKg) : 15f;
            }

            if (ScaleDisplayBoard != null && LobbyManager.Instance != null)
            {
                ScaleDisplayBoard.text =
                    $"=== CARGO EXTRACTION SCALE (0m) ===\n" +
                    $"ITEMS ON SCALE: {ItemsOnScale.Count} ({TotalScaleMassKg:F1} kg)\n" +
                    $"SCALE PAYOUT: ${TotalScaleDollarValue}\n" +
                    $"BANKED CASH: ${LobbyManager.Instance.BankedCash} / QUOTA: ${LobbyManager.Instance.CurrentQuotaTarget}\n" +
                    $"[Press E to Cash Out Scale Haul]";
            }
        }

        public void CashOutScaleHaul()
        {
            RecalculateScale();
            if (TotalScaleDollarValue <= 0 || LobbyManager.Instance == null) return;

            int payout = TotalScaleDollarValue;
            LobbyManager.Instance.BankedCash += payout;

            foreach (var item in ItemsOnScale)
            {
                if (item != null) Destroy(item.gameObject);
            }
            ItemsOnScale.Clear();

            // Check if Quota met!
            if (LobbyManager.Instance.BankedCash >= LobbyManager.Instance.CurrentQuotaTarget)
            {
                LobbyManager.Instance.CurrentDay++;
                LobbyManager.Instance.CurrentQuotaTarget = Mathf.RoundToInt(LobbyManager.Instance.CurrentQuotaTarget * 1.45f);
                Debug.Log($"[CargoScale] QUOTA MET! Advancing to Quota Tier ${LobbyManager.Instance.CurrentQuotaTarget}!");
            }

            RecalculateScale();
        }
    }
}
