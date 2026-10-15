using System;
using System.IO;
using SubLease.Networking;
using UnityEngine;

namespace SubLease.Core
{
    [Serializable]
    public class LiquidatorCareerSaveData
    {
        public ulong SteamId64 = 76561198995000001UL;
        public string PersonaName = "Liquidator";
        public int LifetimeExtractedDollars = 0;
        public int HighestQuotaTierReached = 0;
        public float DeepestSectorReachedMeters = 0f;
        public int TotalLiveMimicsCaptured = 0;
        public int TotalApexBossesDefeated = 0;
        public string UnlockedBeltCertifications = "Rigger,Janitor,ShippingClerk,AcousticTech";
        public string LastSavedIsoTimestamp = "";
    }

    /// <summary>
    /// Priority P10: Persistent Liquidator Career Save System.
    /// Stores lifetime extracted dollars, deepest sector reached, unlocked Work Belt perks,
    /// and Apex Boss kills to a JSON profile isolated per SteamID64 / Sandboxie account.
    /// </summary>
    public class CareerProgressionManager : MonoBehaviour
    {
        public static CareerProgressionManager Instance { get; private set; }

        public LiquidatorCareerSaveData ActiveProfile = new LiquidatorCareerSaveData();
        public string ActiveSaveFilePath = "";

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
            LoadOrInitializeCareerProfile();
        }

        public string GetProfileFilePath()
        {
            ulong steamId = SteamAndRelayTransportBridge.Instance != null
                ? SteamAndRelayTransportBridge.Instance.LocalSteamId64
                : 76561198995000001UL;
            ActiveSaveFilePath = Path.Combine(Application.persistentDataPath, $"SubLease_Career_{steamId}.json");
            return ActiveSaveFilePath;
        }

        public void LoadOrInitializeCareerProfile()
        {
            string path = GetProfileFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    LiquidatorCareerSaveData loaded = JsonUtility.FromJson<LiquidatorCareerSaveData>(json);
                    if (loaded != null)
                    {
                        ActiveProfile = loaded;
                    }
                }
                catch
                {
                    // Fallback to fresh profile if corrupted
                }
            }

            if (SteamAndRelayTransportBridge.Instance != null)
            {
                ActiveProfile.SteamId64 = SteamAndRelayTransportBridge.Instance.LocalSteamId64;
                ActiveProfile.PersonaName = SteamAndRelayTransportBridge.Instance.LocalSteamPersonaName;
            }
        }

        public bool RecordExtractionAndSave(int extractedDollars, float deepestMeters, int quotaTier, int bossesDefeated)
        {
            ActiveProfile.LifetimeExtractedDollars += Mathf.Max(0, extractedDollars);
            if (deepestMeters < ActiveProfile.DeepestSectorReachedMeters)
            {
                ActiveProfile.DeepestSectorReachedMeters = deepestMeters;
            }
            if (quotaTier > ActiveProfile.HighestQuotaTierReached)
            {
                ActiveProfile.HighestQuotaTierReached = quotaTier;
            }
            ActiveProfile.TotalApexBossesDefeated = Mathf.Max(ActiveProfile.TotalApexBossesDefeated, bossesDefeated);
            ActiveProfile.LastSavedIsoTimestamp = DateTime.UtcNow.ToString("o");

            string path = GetProfileFilePath();
            try
            {
                string json = JsonUtility.ToJson(ActiveProfile, true);
                File.WriteAllText(path, json);
                return File.Exists(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CareerProgression] Save warning: {e.Message}");
                return false;
            }
        }
    }
}
