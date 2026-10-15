using System.Collections.Generic;
using SubLease.Networking;
using SubLease.Player;
using UnityEngine;

namespace SubLease.Core
{
    /// <summary>
    /// Priority P5.2: Meta Quest 3 & PCVR Physics Performance Guardrail.
    /// Enforces a hard cap of Max 30 Awake Rigidbodies per Room, locks physics fixedDeltaTime
    /// to 72Hz (1/72s) for VR stability, and sleeps undisturbed distant props.
    /// </summary>
    public class RoomPhysicsCuller : MonoBehaviour
    {
        public static RoomPhysicsCuller Instance { get; private set; }

        [Header("Quest 3 Physics Guardrails")]
        public int MaxAwakeRigidbodiesPerRoom = 30;
        public float ActiveSleepDistanceMeters = 28f;
        public float TargetPhysicsHz = 72f;
        public int CurrentAwakeRigidbodyCount;
        public int CurrentSleepingRigidbodyCount;

        private float _nextCullPassTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Lock physics solver to 72Hz (Meta Quest 3 baseline) with 8 velocity/position iterations for pulley chains
            Time.fixedDeltaTime = 1f / TargetPhysicsHz;
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 3;
        }

        private void Update()
        {
            if (Time.time >= _nextCullPassTime)
            {
                _nextCullPassTime = Time.time + 0.5f;
                EnforceAwakeRigidbodyCap();
            }
        }

        public int EnforceAwakeRigidbodyCap()
        {
            Vector3 playerPos = DualPhysicsPlayerRig.Instance != null
                ? DualPhysicsPlayerRig.Instance.transform.position
                : Vector3.zero;

            Rigidbody[] allBodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            List<(Rigidbody rb, float distSq)> awakeCandidates = new List<(Rigidbody, float)>(allBodies.Length);

            int sleepingCount = 0;
            for (int i = 0; i < allBodies.Length; i++)
            {
                Rigidbody rb = allBodies[i];
                if (rb == null || rb.isKinematic) continue;

                NetworkedPhysicsObject netObj = rb.GetComponent<NetworkedPhysicsObject>();
                bool isActivelyHeld = netObj != null && netObj.ActiveGrabberPlayerIds.Count > 0;
                float distSq = (rb.position - playerPos).sqrMagnitude;

                // Sleep undisturbed distant props beyond ActiveSleepDistanceMeters
                if (!isActivelyHeld && distSq > ActiveSleepDistanceMeters * ActiveSleepDistanceMeters &&
                    rb.linearVelocity.sqrMagnitude < 0.02f)
                {
                    rb.Sleep();
                    sleepingCount++;
                    continue;
                }

                if (!rb.IsSleeping())
                {
                    awakeCandidates.Add((rb, distSq));
                }
                else
                {
                    sleepingCount++;
                }
            }

            // If awake count exceeds the Quest 3 30-body cap, sort by distance and sleep the furthest low-velocity bodies
            if (awakeCandidates.Count > MaxAwakeRigidbodiesPerRoom)
            {
                awakeCandidates.Sort((a, b) => a.distSq.CompareTo(b.distSq));
                for (int i = MaxAwakeRigidbodiesPerRoom; i < awakeCandidates.Count; i++)
                {
                    Rigidbody excessRb = awakeCandidates[i].rb;
                    NetworkedPhysicsObject netObj = excessRb.GetComponent<NetworkedPhysicsObject>();
                    if (netObj != null && netObj.ActiveGrabberPlayerIds.Count > 0) continue;

                    excessRb.Sleep();
                    sleepingCount++;
                }
                CurrentAwakeRigidbodyCount = MaxAwakeRigidbodiesPerRoom;
            }
            else
            {
                CurrentAwakeRigidbodyCount = awakeCandidates.Count;
            }

            CurrentSleepingRigidbodyCount = sleepingCount;
            return CurrentAwakeRigidbodyCount;
        }
    }

    /// <summary>
    /// Priority P5.3: Smoothed Streamer Spectator Camera & Picture-in-Picture "Trap Cam".
    /// Eliminates VR head-bob jitter on the desktop spectator feed and provides a live
    /// RenderTexture PiP monitor showing the Abyssal Shaft / rigged traps.
    /// </summary>
    public class StreamerSpectatorCam : MonoBehaviour
    {
        public static StreamerSpectatorCam Instance { get; private set; }

        [Header("Streamer Spectator Settings")]
        public bool EnablePictureInPictureTrapCam = true;
        public float PositionSmoothSpeed = 12f;
        public float RotationSmoothSpeed = 10f;
        public float StreamerFieldOfView = 92f;

        [Header("Picture-in-Picture Trap Cam")]
        public Camera TrapMonitorCamera;
        public RenderTexture TrapCamRenderTexture;
        public string ActiveTrapCamLabel = "TRAP CAM 01: ABYSSAL WINCH & PENDULUM SHAFT (-28m)";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeTrapMonitorCamera();
        }

        private void InitializeTrapMonitorCamera()
        {
            TrapCamRenderTexture = new RenderTexture(256, 144, 16, RenderTextureFormat.ARGB32);
            TrapCamRenderTexture.name = "SubLease_PiP_TrapCam_RT";
            TrapCamRenderTexture.Create();

            GameObject trapCamObj = new GameObject("SubLease_PiP_TrapMonitorCam");
            trapCamObj.transform.SetParent(transform, false);
            // Position looking down the main elevator shaft toward Sector 1 & Apex 1 Pendulum
            trapCamObj.transform.position = new Vector3(0f, -6.5f, -4.5f);
            trapCamObj.transform.rotation = Quaternion.Euler(58f, 0f, 0f);

            TrapMonitorCamera = trapCamObj.AddComponent<Camera>();
            TrapMonitorCamera.fieldOfView = 78f;
            TrapMonitorCamera.nearClipPlane = 0.2f;
            TrapMonitorCamera.farClipPlane = 95f;
            TrapMonitorCamera.targetTexture = TrapCamRenderTexture;
            TrapMonitorCamera.enabled = true;
        }

        public void FocusTrapCamOnTarget(Transform target, string label)
        {
            if (target == null || TrapMonitorCamera == null) return;
            TrapMonitorCamera.transform.position = target.position + new Vector3(0f, 2.4f, -3.5f);
            TrapMonitorCamera.transform.LookAt(target.position);
            ActiveTrapCamLabel = label;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                EnablePictureInPictureTrapCam = !EnablePictureInPictureTrapCam;
                if (TrapMonitorCamera != null)
                {
                    TrapMonitorCamera.enabled = EnablePictureInPictureTrapCam;
                }
            }
        }

        private void OnGUI()
        {
            if (!EnablePictureInPictureTrapCam || TrapCamRenderTexture == null) return;

            float width = 270f;
            float height = 165f;
            float x = Screen.width - width - 16f;
            float y = Screen.height - height - 16f;

            GUI.Box(new Rect(x - 6f, y - 24f, width + 12f, height + 32f), "");
            GUI.color = new Color(0.4f, 1f, 0.65f);
            GUI.Label(new Rect(x, y - 20f, width, 20f), $"[F9] {ActiveTrapCamLabel}");
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x, y, width, height - 12f), TrapCamRenderTexture, ScaleMode.ScaleToFit, false);

            if (RoomPhysicsCuller.Instance != null)
            {
                GUI.color = new Color(0.8f, 0.9f, 1f);
                GUI.Label(
                    new Rect(x, y + height - 12f, width, 18f),
                    $"Quest3 Guardrail: {RoomPhysicsCuller.Instance.CurrentAwakeRigidbodyCount}/{RoomPhysicsCuller.Instance.MaxAwakeRigidbodiesPerRoom} Awake RBs (72Hz)"
                );
                GUI.color = Color.white;
            }
        }
    }
}
