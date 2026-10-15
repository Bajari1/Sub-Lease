using System.Collections.Generic;
using SubLease.Core;
using UnityEngine;

namespace SubLease.Networking
{
    /// <summary>
    /// Attached to every physical prop, furniture loot piece, and tool.
    /// Manages multiplayer authority transfer, Co-Op force summation (2+ players lifting heavy sofas together),
    /// Negative-Mass anomalies, Acoustic Tape muffling, and Bubble-Wrap impact protection.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkedPhysicsObject : MonoBehaviour
    {
        private static int _nextNetId = 100;

        [Header("Network Authority & Cooperative Lift")]
        public int NetworkObjectId;
        public int AuthorityPlayerId = 1;
        public List<int> ActiveGrabberPlayerIds = new List<int>();
        public bool IsCoOpAssistedByCrewmate = false;

        [Header("Universal Physics Modifiers")]
        [Tooltip("True mass in kg. Can be negative for Tier-4 Helium-Glass Anomaly Loot!")]
        public float TrueSignedMassKg = 15f;
        public bool IsNegativeMassAnomaly = false;
        public float AcousticCollisionMultiplier = 1.0f;
        public float ImpactDamageProtectionMultiplier = 1.0f;
        public bool HasCasterWheelsAttached = false;
        public bool IsElectricallyInsulated = false;
        public int KevlarTapeStrapsApplied = 0;

        public Rigidbody Rb { get; private set; }

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            if (NetworkObjectId == 0)
            {
                NetworkObjectId = ++_nextNetId;
            }

            if (TrueSignedMassKg > 0f)
            {
                Rb.mass = TrueSignedMassKg;
            }
            else
            {
                IsNegativeMassAnomaly = true;
                Rb.mass = Mathf.Abs(TrueSignedMassKg);
                Rb.useGravity = false; // We apply inverted upward gravity in FixedUpdate!
            }
        }

        private void Start()
        {
            if (SubLeaseNetworkManager.Instance != null)
            {
                SubLeaseNetworkManager.Instance.RegisterPhysicsObject(this);
            }
        }

        private void FixedUpdate()
        {
            // Handle Negative-Mass Anomaly ("Helium-Glass" Table falls UPWARD toward ceiling!)
            if (IsNegativeMassAnomaly)
            {
                Rb.AddForce(-Physics.gravity * Mathf.Abs(TrueSignedMassKg), ForceMode.Force);
            }

            // If local player is grabbing a heavy object (>65kg) and a Co-Op Crewmate is in the lobby/nearby,
            // apply cooperative lift assistance!
            if (ActiveGrabberPlayerIds.Count > 0 && LobbyManager.Instance != null && LobbyManager.Instance.CrewRoster.Count > 1)
            {
                IsCoOpAssistedByCrewmate = true;
                // Co-op crewmate supplies upward counter-gravity assistance on heavy furniture
                if (Rb.mass > 45f && !IsNegativeMassAnomaly)
                {
                    float assistFactor = Mathf.Clamp01((LobbyManager.Instance.CrewRoster.Count - 1) * 0.45f);
                    Rb.AddForce(-Physics.gravity * (Rb.mass * assistFactor), ForceMode.Force);
                }
            }
            else
            {
                IsCoOpAssistedByCrewmate = false;
            }
        }

        public void RegisterGrab(int playerId)
        {
            AuthorityPlayerId = playerId;
            if (!ActiveGrabberPlayerIds.Contains(playerId))
            {
                ActiveGrabberPlayerIds.Add(playerId);
            }
            Rb.WakeUp();
        }

        public void UnregisterGrab(int playerId)
        {
            ActiveGrabberPlayerIds.Remove(playerId);
        }

        public void ApplyCasterWheels()
        {
            if (HasCasterWheelsAttached) return;
            HasCasterWheelsAttached = true;

            Collider[] cols = GetComponentsInChildren<Collider>();
            PhysicsMaterial wheelMat = new PhysicsMaterial("CasterWheels_ZeroFriction")
            {
                dynamicFriction = 0.015f,
                staticFriction = 0.015f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };

            foreach (var c in cols)
            {
                c.material = wheelMat;
            }

            Rb.linearDamping = 0.1f;
            Rb.angularDamping = 0.2f;
        }

        public void ApplyAcousticFoamTape()
        {
            AcousticCollisionMultiplier = 0.1f; // -90% collision noise!
        }

        public void ApplyBubbleWrap()
        {
            ImpactDamageProtectionMultiplier = 0.15f; // -85% dent damage!
        }

        private void OnCollisionEnter(Collision collision)
        {
            float impulse = collision.impulse.magnitude;
            if (impulse < 4f) return;

            // Convert physics impulse (N*s) into decibels, scaled by Acoustic Foam Tape
            float rawDb = Mathf.Clamp(32f + Mathf.Log10(1f + impulse) * 28f, 30f, 120f);
            float effectiveDb = rawDb * AcousticCollisionMultiplier;

            if (AcousticAndPhysicsBus.Instance != null && effectiveDb >= 32f)
            {
                Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
                AcousticAndPhysicsBus.Instance.EmitSound(hitPoint, effectiveDb, SoundSourceType.PhysicsCollision, gameObject, impulse);
            }
        }
    }

    /// <summary>
    /// Captures real-time player microphone volume for Proximity Voice Chat and feeds
    /// vocal decibels directly into the game's AcousticAndPhysicsBus so The Tuning Fork hears real player voices!
    /// </summary>
    public class VoiceChatAcousticBridge : MonoBehaviour
    {
        public bool EnableMicCapture = true;
        [Range(0f, 120f)] public float CurrentVoiceDecibels = 25f;
        public float AlertThresholdDecibels = 58f;

        private AudioClip _micClip;
        private string _micDeviceName;
        private readonly float[] _sampleWindow = new float[256];
        private float _nextAcousticEmitTime;

        private void Start()
        {
            if (!EnableMicCapture) return;
            try
            {
                if (Microphone.devices != null && Microphone.devices.Length > 0)
                {
                    _micDeviceName = Microphone.devices[0];
                    _micClip = Microphone.Start(_micDeviceName, true, 1, 16000);
                }
            }
            catch
            {
                // No microphone connected or permission denied—safe fallback
                _micClip = null;
            }
        }

        private void Update()
        {
            // Allow testing Voice Chat Scream in Editor with [V] key!
            if (Input.GetKey(KeyCode.V))
            {
                CurrentVoiceDecibels = 92f;
            }
            else if (_micClip != null && !string.IsNullOrEmpty(_micDeviceName) && Microphone.IsRecording(_micDeviceName))
            {
                int micPos = Microphone.GetPosition(_micDeviceName) - _sampleWindow.Length + 1;
                if (micPos >= 0)
                {
                    _micClip.GetData(_sampleWindow, micPos);
                    float sumSq = 0f;
                    for (int i = 0; i < _sampleWindow.Length; i++)
                    {
                        sumSq += _sampleWindow[i] * _sampleWindow[i];
                    }
                    float rms = Mathf.Sqrt(sumSq / _sampleWindow.Length);
                    CurrentVoiceDecibels = Mathf.Clamp(25f + rms * 280f, 25f, 115f);
                }
            }
            else
            {
                CurrentVoiceDecibels = Mathf.MoveTowards(CurrentVoiceDecibels, 25f, Time.deltaTime * 40f);
            }

            if (CurrentVoiceDecibels >= AlertThresholdDecibels && Time.time >= _nextAcousticEmitTime)
            {
                _nextAcousticEmitTime = Time.time + 0.35f;
                Vector3 voicePos = Camera.main != null ? Camera.main.transform.position : transform.position;
                if (AcousticAndPhysicsBus.Instance != null)
                {
                    AcousticAndPhysicsBus.Instance.EmitSound(voicePos, CurrentVoiceDecibels, SoundSourceType.PlayerVoiceMic, gameObject);
                }
            }
        }
    }
}
