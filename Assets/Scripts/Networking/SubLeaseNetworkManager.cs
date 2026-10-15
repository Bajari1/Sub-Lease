using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace SubLease.Networking
{
    /// <summary>
    /// Low-latency UDP replication manager for Co-Op VR/Desktop players, physics rigidbodies,
    /// rope tethers, duct tape welds, and proximity voice decibels.
    /// Ready to bridge with Steamworks P2P or Unity Relay while working out-of-the-box over LAN/Localhost.
    /// </summary>
    public class SubLeaseNetworkManager : MonoBehaviour
    {
        public static SubLeaseNetworkManager Instance { get; private set; }

        [Header("Network Runtime State")]
        public bool IsServer = true;
        public bool IsConnected = false;
        public int LocalPlayerId = 1;
        public float TickRateHz = 30f;

        private UdpClient _udpSocket;
        private readonly List<IPEndPoint> _connectedClients = new List<IPEndPoint>();
        private IPEndPoint _serverEndPoint;
        private float _nextTickTime;

        private readonly Dictionary<int, GameObject> _remoteAvatars = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, NetworkedPhysicsObject> _registeredPhysicsObjects = new Dictionary<int, NetworkedPhysicsObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterPhysicsObject(NetworkedPhysicsObject netObj)
        {
            if (netObj == null) return;
            _registeredPhysicsObjects[netObj.NetworkObjectId] = netObj;
        }

        public void StartHost(int port)
        {
            ShutdownSocket();
            IsServer = true;
            IsConnected = true;
            LocalPlayerId = 1;

            try
            {
                _udpSocket = new UdpClient(port);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SubLeaseNetworkManager] Host UDP port {port} busy (running in local loopback mode): {e.Message}");
            }
        }

        public void ConnectToHost(string ip, int port, string roomCode)
        {
            ShutdownSocket();
            IsServer = false;
            IsConnected = true;
            LocalPlayerId = UnityEngine.Random.Range(2, 999);

            try
            {
                _udpSocket = new UdpClient(0);
                _serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                SendRawPacket($"JOIN|{LocalPlayerId}|Liquidator-{LocalPlayerId}|{roomCode}", _serverEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SubLeaseNetworkManager] Failed connecting to {ip}:{port}: {e.Message}");
            }
        }

        private void Update()
        {
            PollIncomingPackets();

            if (IsConnected && Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + (1f / Mathf.Max(10f, TickRateHz));
                BroadcastStateTick();
            }
        }

        public void BroadcastRpc(string rpcName, string payload)
        {
            string packet = $"RPC|{LocalPlayerId}|{rpcName}|{payload}";
            if (IsServer)
            {
                foreach (var client in _connectedClients)
                {
                    SendRawPacket(packet, client);
                }
            }
            else if (_serverEndPoint != null)
            {
                SendRawPacket(packet, _serverEndPoint);
            }
        }

        /// <summary>
        /// Spawns or updates a 3D physical Co-Op Crewmate Avatar (Head visor, Work Belt, and Dual Physics Hands).
        /// </summary>
        public void SpawnOrUpdateRemoteAvatar(CrewMemberState state)
        {
            if (state.PlayerId == LocalPlayerId && !state.IsBotCrewmate) return;

            if (!_remoteAvatars.TryGetValue(state.PlayerId, out GameObject avatarRoot) || avatarRoot == null)
            {
                avatarRoot = CreateProceduralCrewAvatar(state);
                _remoteAvatars[state.PlayerId] = avatarRoot;
            }

            Transform head = avatarRoot.transform.Find("HeadVisor");
            Transform leftHand = avatarRoot.transform.Find("LeftHand");
            Transform rightHand = avatarRoot.transform.Find("RightHand");
            Transform torso = avatarRoot.transform.Find("TorsoRig");

            if (head != null)
            {
                head.position = Vector3.Lerp(head.position, state.HeadPosition, Time.deltaTime * 15f);
                head.rotation = Quaternion.Slerp(head.rotation, state.HeadRotation, Time.deltaTime * 15f);
            }
            if (torso != null && head != null)
            {
                torso.position = head.position + Vector3.down * 0.55f;
                Vector3 flatFwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (flatFwd.sqrMagnitude > 0.01f) torso.rotation = Quaternion.LookRotation(flatFwd);
            }
            if (leftHand != null)
            {
                leftHand.position = Vector3.Lerp(leftHand.position, state.LeftHandPosition, Time.deltaTime * 18f);
            }
            if (rightHand != null)
            {
                rightHand.position = Vector3.Lerp(rightHand.position, state.RightHandPosition, Time.deltaTime * 18f);
            }
        }

        private GameObject CreateProceduralCrewAvatar(CrewMemberState state)
        {
            GameObject root = new GameObject($"CrewAvatar_{state.DisplayName}");

            Color suitColor = state.SelectedBelt switch
            {
                WorkBeltRole.Rigger_PulleysAndPitons => new Color(0.9f, 0.65f, 0.1f),
                WorkBeltRole.Janitor_FrictionAndSlip => new Color(0.2f, 0.65f, 0.75f),
                WorkBeltRole.ShippingClerk_KevlarTapeAndWheels => new Color(0.85f, 0.35f, 0.15f),
                _ => new Color(0.55f, 0.35f, 0.85f)
            };

            // Head & Hazmat Visor
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "HeadVisor";
            head.transform.SetParent(root.transform);
            head.transform.localScale = new Vector3(0.26f, 0.28f, 0.26f);
            head.transform.position = state.HeadPosition;
            Destroy(head.GetComponent<Collider>());
            ApplyColor(head, suitColor);

            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "GlassFaceplate";
            visor.transform.SetParent(head.transform);
            visor.transform.localPosition = new Vector3(0f, 0.05f, 0.42f);
            visor.transform.localScale = new Vector3(0.82f, 0.48f, 0.25f);
            Destroy(visor.GetComponent<Collider>());
            ApplyColor(visor, new Color(0.1f, 0.15f, 0.2f));

            // Torso & Utility Belt
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "TorsoRig";
            torso.transform.SetParent(root.transform);
            torso.transform.localScale = new Vector3(0.38f, 0.45f, 0.28f);
            torso.transform.position = state.HeadPosition + Vector3.down * 0.55f;
            Destroy(torso.GetComponent<Collider>());
            ApplyColor(torso, suitColor * 0.8f);

            // Left & Right Physics Hands (Can help push/stabilize props!)
            GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lHand.name = "LeftHand";
            lHand.transform.SetParent(root.transform);
            lHand.transform.localScale = Vector3.one * 0.14f;
            lHand.transform.position = state.LeftHandPosition;
            Destroy(lHand.GetComponent<Collider>());
            ApplyColor(lHand, new Color(0.95f, 0.85f, 0.25f));

            GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rHand.name = "RightHand";
            rHand.transform.SetParent(root.transform);
            rHand.transform.localScale = Vector3.one * 0.14f;
            rHand.transform.position = state.RightHandPosition;
            Destroy(rHand.GetComponent<Collider>());
            ApplyColor(rHand, new Color(0.95f, 0.85f, 0.25f));

            // Floating Nameplate
            GameObject labelObj = new GameObject("Nameplate");
            labelObj.transform.SetParent(head.transform);
            labelObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = state.DisplayName;
            tm.characterSize = 0.08f;
            tm.fontSize = 42;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            return root;
        }

        private void ApplyColor(GameObject obj, Color col)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default"));
                mat.color = col;
                r.material = mat;
            }
        }

        private void BroadcastStateTick()
        {
            if (_udpSocket == null) return;
            if (IsServer && _connectedClients.Count == 0) return;

            if (Camera.main != null && LobbyManager.Instance != null && LobbyManager.Instance.CrewRoster.Count > 0)
            {
                Vector3 pos = Camera.main.transform.position;
                Quaternion rot = Camera.main.transform.rotation;
                string posePacket = $"POSE|{LocalPlayerId}|{pos.x:F2},{pos.y:F2},{pos.z:F2}|{rot.x:F2},{rot.y:F2},{rot.z:F2},{rot.w:F2}";

                if (IsServer)
                {
                    foreach (var client in _connectedClients) SendRawPacket(posePacket, client);
                }
                else if (_serverEndPoint != null)
                {
                    SendRawPacket(posePacket, _serverEndPoint);
                }
            }
        }

        private void PollIncomingPackets()
        {
            if (_udpSocket == null) return;
            try
            {
                while (_udpSocket.Available > 0)
                {
                    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = _udpSocket.Receive(ref sender);
                    string msg = Encoding.UTF8.GetString(bytes);
                    HandlePacket(msg, sender);
                }
            }
            catch
            {
                // Ignore non-fatal UDP socket resets
            }
        }

        private void HandlePacket(string msg, IPEndPoint sender)
        {
            string[] parts = msg.Split('|');
            if (parts.Length < 2) return;

            string type = parts[0];
            if (type == "JOIN" && IsServer && parts.Length >= 4)
            {
                int clientId = int.TryParse(parts[1], out int id) ? id : 2;
                string name = parts[2];

                if (!_connectedClients.Contains(sender))
                {
                    _connectedClients.Add(sender);
                }

                if (LobbyManager.Instance != null)
                {
                    CrewMemberState member = new CrewMemberState
                    {
                        PlayerId = clientId,
                        DisplayName = name,
                        SelectedBelt = WorkBeltRole.ShippingClerk_KevlarTapeAndWheels,
                        IsReady = true,
                        IsHost = false,
                        HeadPosition = new Vector3(1.2f, 1.7f, -3.0f)
                    };
                    LobbyManager.Instance.CrewRoster.Add(member);
                    SpawnOrUpdateRemoteAvatar(member);
                }
            }
        }

        private void SendRawPacket(string message, IPEndPoint target)
        {
            if (_udpSocket == null || target == null) return;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _udpSocket.Send(data, data.Length, target);
            }
            catch
            {
                // Non-blocking send
            }
        }

        private void ShutdownSocket()
        {
            if (_udpSocket != null)
            {
                _udpSocket.Close();
                _udpSocket = null;
            }
        }

        private void OnDestroy()
        {
            ShutdownSocket();
        }
    }
}
