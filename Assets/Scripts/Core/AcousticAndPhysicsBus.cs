using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubLease.Core
{
    public enum SoundSourceType
    {
        PhysicsCollision,
        RopeCreakOrSnap,
        PlayerVoiceMic,
        GadgetTimerOrHorn,
        WalkieTalkieRelay,
        GrandfatherClockChime,
        MotorizedWinch
    }

    [Serializable]
    public struct AcousticEvent
    {
        public Vector3 WorldPosition;
        public float Decibels;
        public float ImpulseMagnitude;
        public SoundSourceType SourceType;
        public GameObject Instigator;
        public float Timestamp;
    }

    /// <summary>
    /// Global physics trauma & acoustic impulse bus.
    /// Drives entity hunting (The Tuning Fork), furniture dent penalties, and procedural sound synthesis.
    /// </summary>
    public class AcousticAndPhysicsBus : MonoBehaviour
    {
        public static AcousticAndPhysicsBus Instance { get; private set; }

        public event Action<AcousticEvent> OnAcousticImpulse;

        [Header("Recent Acoustic History (For AI & HUD)")]
        public List<AcousticEvent> RecentEvents = new List<AcousticEvent>();
        public float LastPeakDecibels;
        public Vector3 LastLoudestPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Decay peak decibel meter on HUD
            LastPeakDecibels = Mathf.MoveTowards(LastPeakDecibels, 20f, Time.deltaTime * 25f);

            // Prune events older than 8 seconds
            float cutoff = Time.time - 8f;
            for (int i = RecentEvents.Count - 1; i >= 0; i--)
            {
                if (RecentEvents[i].Timestamp < cutoff)
                {
                    RecentEvents.RemoveAt(i);
                }
            }
        }

        public void EmitSound(Vector3 position, float decibels, SoundSourceType sourceType, GameObject instigator = null, float impulse = 0f)
        {
            if (decibels < 25f) return;

            AcousticEvent evt = new AcousticEvent
            {
                WorldPosition = position,
                Decibels = decibels,
                ImpulseMagnitude = impulse,
                SourceType = sourceType,
                Instigator = instigator,
                Timestamp = Time.time
            };

            RecentEvents.Add(evt);
            if (decibels >= LastPeakDecibels)
            {
                LastPeakDecibels = decibels;
                LastLoudestPosition = position;
            }

            OnAcousticImpulse?.Invoke(evt);
        }
    }

    /// <summary>
    /// Synthesizes procedural liminal hum, rope creaks, tape rips, and collision thuds in real time
    /// so the project requires zero external audio files.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralAudioSynth : MonoBehaviour
    {
        public enum SynthMode
        {
            FluorescentHum60Hz,
            OneShotEffect
        }

        public SynthMode Mode = SynthMode.OneShotEffect;
        [Range(0f, 1f)] public float MasterGain = 0.12f;

        private double _phase;
        private double _sampleRate = 48000.0;
        private float _burstTimer;
        private float _burstFrequency = 220f;
        private float _burstNoiseMix = 0.2f;

        private void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000.0;
            AudioSource src = GetComponent<AudioSource>();
            src.playOnAwake = true;
            src.loop = true;
            src.spatialBlend = Mode == SynthMode.FluorescentHum60Hz ? 0.85f : 0.5f;
            if (!src.isPlaying) src.Play();
        }

        public void TriggerProceduralBurst(float frequency, float duration, float noiseMix)
        {
            _burstFrequency = frequency;
            _burstTimer = duration;
            _burstNoiseMix = Mathf.Clamp01(noiseMix);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            System.Random rng = new System.Random();

            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = 0f;

                if (Mode == SynthMode.FluorescentHum60Hz)
                {
                    _phase += (60.0 * 2.0 * Math.PI) / _sampleRate;
                    if (_phase > Math.PI * 2.0) _phase -= Math.PI * 2.0;
                    // 60Hz fundamental + 120Hz harmonic buzz
                    sample = (float)(Math.Sin(_phase) * 0.6 + Math.Sin(_phase * 2.0) * 0.3 + Math.Sin(_phase * 3.0) * 0.1) * MasterGain;
                }
                else if (_burstTimer > 0f)
                {
                    _burstTimer -= (float)(1.0 / _sampleRate);
                    _phase += (_burstFrequency * 2.0 * Math.PI) / _sampleRate;
                    float tone = (float)Math.Sin(_phase);
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    sample = Mathf.Lerp(tone, noise, _burstNoiseMix) * MasterGain * Mathf.Clamp01(_burstTimer * 6f);
                }

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = sample;
                }
            }
        }
    }
}
