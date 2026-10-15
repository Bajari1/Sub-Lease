using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace SubLease.Networking
{
    public enum WorkBeltRole
    {
        Rigger_PulleysAndPitons,
        Janitor_FrictionAndSlip,
        ShippingClerk_KevlarTapeAndWheels,
        AcousticTech_TimersAndStealth
    }

    public enum ShiftSessionState
    {
        MainMenuOrOffline,
        StagingInDropHatch,
        ActiveHeistInShafts,
        QuotaEvaluation
    }

    [Serializable]
    public class CrewMemberState
    {
        public int PlayerId;
        public string DisplayName;
        public WorkBeltRole SelectedBelt;
        public bool IsReady;
        public bool IsHost;
        public bool IsBotCrewmate;
        public Vector3 HeadPosition;
        public Quaternion HeadRotation;
        public Vector3 LeftHandPosition;
        public Vector3 RightHandPosition;
        public float CurrentMicDecibels;
        public bool IsRappelling;
    }

    [Serializable]
    public class DiscoveredLanLobby
    {
        public string RoomCode;
        public string HostName;
        public string IpAddress;
        public int Port;
        public int CurrentPlayers;
        public int MaxPlayers;
        public int CurrentQuota;
        public float LastSeenTime;
    }

    /// <summary>
    /// Manages Co-Op Lobby creation, 6-Character Room Codes (e.g., SL-4820),
    /// automatic UDP LAN / Quest 3 discovery, Work Belt loadouts, and AI Co-Op Crewmate spawning.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        public const int DiscoveryPort = 47777;
        public const int GameTrafficPort = 47778;
        public const int MaxCrewSize = 4;

        [Header("Lobby & Session Status")]
        public ShiftSessionState SessionState = ShiftSessionState.StagingInDropHatch;
        public string CurrentRoomCode = "SL-0000";
        public bool IsHost = true;
        public bool IsMultiplayerActive = false;

        [Header("Crew Roster (Up to 4 Liquidators)")]
        public List<CrewMemberState> CrewRoster = new List<CrewMemberState>();
        public List<DiscoveredLanLobby> DiscoveredLobbies = new List<DiscoveredLanLobby>();

        [Header("Quota & Economy Sync")]
        public int CurrentDay = 1;
        public int DaysPerQuota = 3;
        public int CurrentQuotaTarget = 1500;
        public int BankedCash = 450; // Starter budget for ropes, tape, grapples

        public event Action OnLobbyRosterUpdated;
        public event Action<ShiftSessionState> OnSessionStateChanged;

        private UdpClient _discoveryBroadcaster;
        private UdpClient _discoveryListener;
        private float _nextBroadcastTime;

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
            // Automatically initialize a default Host Lobby in the Drop Hatch so the player can immediately play or invite crew
            HostNewLobby("Liquidator-1");
            StartLanDiscoveryListener();
        }

        private void Update()
        {
            if (IsMultiplayerActive && IsHost && Time.time >= _nextBroadcastTime)
            {
                _nextBroadcastTime = Time.time + 1.5f;
                BroadcastLobbyBeacon();
            }

            PollLanDiscovery();
            UpdateBotCrewmates();
        }

        public void HostNewLobby(string hostPlayerName)
        {
            IsHost = true;
            IsMultiplayerActive = true;
            CurrentRoomCode = GenerateRoomCode();
            SessionState = ShiftSessionState.StagingInDropHatch;

            CrewRoster.Clear();
            CrewRoster.Add(new CrewMemberState
            {
                PlayerId = 1,
                DisplayName = hostPlayerName,
                SelectedBelt = WorkBeltRole.Rigger_PulleysAndPitons,
                IsReady = true,
                IsHost = true,
                IsBotCrewmate = false,
                HeadPosition = new Vector3(0f, 1.7f, -3.5f)
            });

            if (SubLeaseNetworkManager.Instance != null)
            {
                SubLeaseNetworkManager.Instance.StartHost(GameTrafficPort);
            }

            OnLobbyRosterUpdated?.Invoke();
            Debug.Log($"[LobbyManager] Hosted Co-Op Lobby | Room Code: {CurrentRoomCode} | Port: {GameTrafficPort}");
        }

        public void JoinLobbyByCodeOrIp(string roomCode, string ipAddress = "127.0.0.1", int port = GameTrafficPort)
        {
            IsHost = false;
            IsMultiplayerActive = true;
            CurrentRoomCode = string.IsNullOrWhiteSpace(roomCode) ? "SL-JOIN" : roomCode.ToUpperInvariant();
            SessionState = ShiftSessionState.StagingInDropHatch;

            // Check if room code matches a discovered LAN lobby
            foreach (var lobby in DiscoveredLobbies)
            {
                if (string.Equals(lobby.RoomCode, CurrentRoomCode, StringComparison.OrdinalIgnoreCase))
                {
                    ipAddress = lobby.IpAddress;
                    port = lobby.Port;
                    break;
                }
            }

            if (SubLeaseNetworkManager.Instance != null)
            {
                SubLeaseNetworkManager.Instance.ConnectToHost(ipAddress, port, CurrentRoomCode);
            }

            OnLobbyRosterUpdated?.Invoke();
            Debug.Log($"[LobbyManager] Joining Lobby {CurrentRoomCode} at {ipAddress}:{port}...");
        }

        public void CycleLocalPlayerBelt()
        {
            if (CrewRoster.Count == 0) return;
            CrewMemberState local = CrewRoster[0];
            int next = ((int)local.SelectedBelt + 1) % Enum.GetValues(typeof(WorkBeltRole)).Length;
            local.SelectedBelt = (WorkBeltRole)next;
            OnLobbyRosterUpdated?.Invoke();
        }

        public void ToggleLocalReady()
        {
            if (CrewRoster.Count == 0) return;
            CrewRoster[0].IsReady = !CrewRoster[0].IsReady;
            OnLobbyRosterUpdated?.Invoke();
        }

        /// <summary>
        /// Spawns a physical AI Co-Op Crewmate in the Drop Hatch so solo developers/players can test
        /// 2-player cooperative pulley hoisting and heavy furniture carrying immediately.
        /// </summary>
        public void SpawnTestCoOpCrewmate()
        {
            if (CrewRoster.Count >= MaxCrewSize) return;

            int newId = CrewRoster.Count + 1;
            WorkBeltRole role = (WorkBeltRole)((newId - 1) % 4);
            Vector3 spawnPos = new Vector3((newId - 2) * 1.4f, 1.0f, -2.5f);

            CrewMemberState botState = new CrewMemberState
            {
                PlayerId = newId,
                DisplayName = $"RepoBot-{newId} ({role.ToString().Split('_')[0]})",
                SelectedBelt = role,
                IsReady = true,
                IsHost = false,
                IsBotCrewmate = true,
                HeadPosition = spawnPos + Vector3.up * 0.7f,
                LeftHandPosition = spawnPos + new Vector3(-0.35f, 0.2f, 0.4f),
                RightHandPosition = spawnPos + new Vector3(0.35f, 0.2f, 0.4f)
            };

            CrewRoster.Add(botState);

            if (SubLeaseNetworkManager.Instance != null)
            {
                SubLeaseNetworkManager.Instance.SpawnOrUpdateRemoteAvatar(botState);
            }

            OnLobbyRosterUpdated?.Invoke();
            Debug.Log($"[LobbyManager] Spawned Co-Op Crewmate: {botState.DisplayName}");
        }

        public void StartHeistShift()
        {
            SessionState = ShiftSessionState.ActiveHeistInShafts;
            OnSessionStateChanged?.Invoke(SessionState);
            Debug.Log($"[LobbyManager] Shift Started! Quota Target: ${CurrentQuotaTarget}");
        }

        private void UpdateBotCrewmates()
        {
            if (CrewRoster.Count <= 1) return;

            Transform playerTransform = Camera.main != null ? Camera.main.transform : null;
            for (int i = 1; i < CrewRoster.Count; i++)
            {
                CrewMemberState member = CrewRoster[i];
                if (!member.IsBotCrewmate) continue;

                // Subtle idle breathing & looking toward local player or nearest heavy loot
                float bob = Mathf.Sin(Time.time * 2.2f + i) * 0.04f;
                if (playerTransform != null)
                {
                    Vector3 targetFollow = playerTransform.position - playerTransform.forward * (1.8f + i * 0.8f) + playerTransform.right * ((i % 2 == 0 ? 1f : -1f) * 1.1f);
                    targetFollow.y = Mathf.Max(playerTransform.position.y, -29.0f);
                    member.HeadPosition = Vector3.Lerp(member.HeadPosition, targetFollow + new Vector3(0f, 0.6f + bob, 0f), Time.deltaTime * 2.5f);

                    Vector3 lookDir = playerTransform.position - member.HeadPosition;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        member.HeadRotation = Quaternion.Slerp(member.HeadRotation, Quaternion.LookRotation(lookDir.normalized), Time.deltaTime * 5f);
                    }
                }

                member.LeftHandPosition = member.HeadPosition + member.HeadRotation * new Vector3(-0.32f, -0.45f, 0.45f);
                member.RightHandPosition = member.HeadPosition + member.HeadRotation * new Vector3(0.32f, -0.45f, 0.45f);

                if (SubLeaseNetworkManager.Instance != null)
                {
                    SubLeaseNetworkManager.Instance.SpawnOrUpdateRemoteAvatar(member);
                }
            }
        }

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            System.Random rng = new System.Random();
            char[] code = new char[4];
            for (int i = 0; i < 4; i++)
            {
                code[i] = chars[rng.Next(chars.Length)];
            }
            return "SL-" + new string(code);
        }

        private void StartLanDiscoveryListener()
        {
            try
            {
                _discoveryBroadcaster = new UdpClient();
                _discoveryBroadcaster.EnableBroadcast = true;

                _discoveryListener = new UdpClient();
                _discoveryListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _discoveryListener.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LobbyManager] LAN Discovery socket bind warning (safe to ignore if multiple instances on same PC): {e.Message}");
            }
        }

        private void BroadcastLobbyBeacon()
        {
            if (_discoveryBroadcaster == null) return;
            try
            {
                string payload = $"SUBLEASE|{CurrentRoomCode}|{CrewRoster[0].DisplayName}|{GameTrafficPort}|{CrewRoster.Count}|{MaxCrewSize}|{CurrentQuotaTarget}";
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                _discoveryBroadcaster.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
            }
            catch
            {
                // Ignore broadcast errors on disconnected adapters
            }
        }

        private void PollLanDiscovery()
        {
            if (_discoveryListener == null) return;
            try
            {
                while (_discoveryListener.Available > 0)
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _discoveryListener.Receive(ref ep);
                    string msg = Encoding.UTF8.GetString(data);
                    if (!msg.StartsWith("SUBLEASE|")) continue;

                    string[] parts = msg.Split('|');
                    if (parts.Length < 7) continue;

                    string code = parts[1];
                    if (code == CurrentRoomCode && IsHost) continue;

                    DiscoveredLanLobby existing = DiscoveredLobbies.Find(l => l.RoomCode == code);
                    if (existing == null)
                    {
                        existing = new DiscoveredLanLobby { RoomCode = code };
                        DiscoveredLobbies.Add(existing);
                    }

                    existing.HostName = parts[2];
                    existing.IpAddress = ep.Address.ToString();
                    existing.Port = int.TryParse(parts[3], out int p) ? p : GameTrafficPort;
                    existing.CurrentPlayers = int.TryParse(parts[4], out int cp) ? cp : 1;
                    existing.MaxPlayers = int.TryParse(parts[5], out int mp) ? mp : 4;
                    existing.CurrentQuota = int.TryParse(parts[6], out int q) ? q : 1500;
                    existing.LastSeenTime = Time.time;
                }
            }
            catch
            {
                // Non-blocking poll
            }
        }

        private void OnDestroy()
        {
            _discoveryBroadcaster?.Close();
            _discoveryListener?.Close();
        }
    }
}
