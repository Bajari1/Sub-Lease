using UnityEngine;

namespace SubLease.Core
{
    /// <summary>
    /// Priority P8: Multi-Channel Spatial 3D Procedural Audio Synthesizer & Shaft Reverb.
    /// Generates real-time procedural AudioClips for rope strain creaks, ratchet winch clicks,
    /// duct tape rips, glass shatters, Mimic 48 BPM heartbeats, and Tuning Fork harmonic resonance.
    /// </summary>
    public class ProceduralAudioSynthesizer : MonoBehaviour
    {
        public static ProceduralAudioSynthesizer Instance { get; private set; }

        [Header("Synthesized Procedural AudioClips")]
        public AudioClip RopeStrainCreakClip;
        public AudioClip RatchetWinchClickClip;
        public AudioClip DuctTapeRipClip;
        public AudioClip GlassShatterClip;
        public AudioClip MimicHeartbeatClip;
        public AudioClip TuningForkResonanceClip;

        [Header("Spatial Audio & Occlusion Stats")]
        public int TotalSynthesizedClipsReady = 0;
        public int TotalSpatialSoundsPlayed = 0;
        public float LastOcclusionCutoffHz = 22000f;

        private const int SampleRate = 22050;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SynthesizeAllProceduralWaveforms();
        }

        private void Start()
        {
            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.OnAcousticImpulse += OnAcousticWaveReceived;
            }
        }

        public void SynthesizeAllProceduralWaveforms()
        {
            RopeStrainCreakClip = CreateProceduralClip("Synth_RopeStrainCreak", 0.35f, (t, i) =>
            {
                float click = (i % 650 < 45) ? Mathf.Sin(t * 240f * Mathf.PI * 2f) : 0f;
                return click * Mathf.Exp(-t * 4f) * 0.6f;
            });

            RatchetWinchClickClip = CreateProceduralClip("Synth_RatchetClick", 0.12f, (t, i) =>
            {
                float metallic = Mathf.Sin(t * 1450f * Mathf.PI * 2f) + 0.5f * Mathf.Sin(t * 2900f * Mathf.PI * 2f);
                return metallic * Mathf.Exp(-t * 32f) * 0.5f;
            });

            DuctTapeRipClip = CreateProceduralClip("Synth_DuctTapeRip", 0.25f, (t, i) =>
            {
                float noise = (((i * 1103515245 + 12345) & 0x7FFF) / 16384f) - 1f;
                float env = Mathf.Sin((t / 0.25f) * Mathf.PI);
                return noise * env * 0.4f;
            });

            GlassShatterClip = CreateProceduralClip("Synth_GlassShatter", 0.45f, (t, i) =>
            {
                float chime = Mathf.Sin(t * 3100f * Mathf.PI * 2f) + Mathf.Sin(t * 4850f * Mathf.PI * 2f);
                return chime * Mathf.Exp(-t * 9f) * 0.45f;
            });

            MimicHeartbeatClip = CreateProceduralClip("Synth_Mimic48BpmHeartbeat", 0.5f, (t, i) =>
            {
                float lub = t < 0.12f ? Mathf.Sin(t * 58f * Mathf.PI * 2f) * Mathf.Exp(-t * 18f) : 0f;
                float dub = (t > 0.18f && t < 0.32f) ? Mathf.Sin((t - 0.18f) * 72f * Mathf.PI * 2f) * Mathf.Exp(-(t - 0.18f) * 22f) : 0f;
                return (lub + dub) * 0.75f;
            });

            TuningForkResonanceClip = CreateProceduralClip("Synth_TuningForkResonance", 0.65f, (t, i) =>
            {
                float ring = Mathf.Sin(t * 440f * Mathf.PI * 2f) + 0.6f * Mathf.Sin(t * 880f * Mathf.PI * 2f) + 0.3f * Mathf.Sin(t * 1320f * Mathf.PI * 2f);
                return ring * Mathf.Exp(-t * 3.5f) * 0.35f;
            });

            TotalSynthesizedClipsReady = 6;
        }

        private AudioClip CreateProceduralClip(string clipName, float durationSec, System.Func<float, int, float> generator)
        {
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(SampleRate * durationSec));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                samples[i] = Mathf.Clamp(generator(t, i), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnAcousticWaveReceived(AcousticEvent evt)
        {
            AudioClip chosen = evt.SourceType switch
            {
                SoundSourceType.RopeCreakOrSnap => RopeStrainCreakClip,
                SoundSourceType.MotorizedWinch => RatchetWinchClickClip,
                SoundSourceType.GrandfatherClockChime => TuningForkResonanceClip,
                _ => RatchetWinchClickClip
            };
            PlaySpatialSynthesizedSound(chosen, evt.WorldPosition, Mathf.Clamp01(evt.Decibels / 115f), 1.0f);
        }

        public float PlayRopeTensionCreak(Vector3 worldPos, float tensionRatio)
        {
            float pitch = Mathf.Lerp(0.75f, 1.85f, Mathf.Clamp01(tensionRatio));
            PlaySpatialSynthesizedSound(RopeStrainCreakClip, worldPos, Mathf.Clamp01(tensionRatio), pitch);
            return pitch;
        }

        public void PlaySpatialSynthesizedSound(AudioClip clip, Vector3 worldPos, float volume = 0.8f, float pitch = 1.0f)
        {
            if (clip == null) return;

            TotalSpatialSoundsPlayed++;

            // Calculate acoustic door/wall occlusion between sound and player
            LastOcclusionCutoffHz = 22000f;
            if (Player.DualPhysicsPlayerRig.Instance != null)
            {
                Vector3 listenerPos = Player.DualPhysicsPlayerRig.Instance.transform.position + Vector3.up * 0.6f;
                if (Physics.Linecast(worldPos, listenerPos, out RaycastHit hit))
                {
                    if (hit.collider != null && (hit.collider.name.Contains("Door") || hit.collider.name.Contains("Wall")))
                    {
                        LastOcclusionCutoffHz = 950f; // Muffled behind closed fire door or wall!
                        volume *= 0.45f;
                    }
                }
            }

            GameObject emitter = new GameObject($"SpatialSynthAudio_{clip.name}");
            emitter.transform.position = worldPos;
            AudioSource src = emitter.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = 1.0f;
            src.minDistance = 2.0f;
            src.maxDistance = 42.0f;
            src.volume = volume;
            src.pitch = pitch;
            src.Play();
            Destroy(emitter, clip.length + 0.15f);
        }
    }
}
