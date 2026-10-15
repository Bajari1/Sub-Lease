using SubLease.Networking;
using UnityEngine;
using UnityEngine.XR;

namespace SubLease.Player
{
    /// <summary>
    /// Priority P9: Native OpenXR 6-DOF Controller & Haptics Bridge + Diegetic 3D Liquidator Wristwatch.
    /// Bridges UnityEngine.XR InputDevices (Head, LeftHand, RightHand) and haptic impulses with
    /// DualPhysicsPlayerRig, and mounts a physical 3D digital wristwatch on the player's left wrist.
    /// </summary>
    public class OpenXRAndWristwatchSystem : MonoBehaviour
    {
        public static OpenXRAndWristwatchSystem Instance { get; private set; }

        [Header("OpenXR 6-DOF & Haptics State")]
        public bool IsXrHardwareConnected = false;
        public float LastHapticAmplitude = 0f;
        public int TotalHapticPulsesSent = 0;

        [Header("Diegetic 3D Liquidator Wristwatch")]
        public GameObject WristwatchRoot;
        public TextMesh WristwatchReadoutText;
        public string LastFormattedWatchText = "";

        private InputDevice _leftXrController;
        private InputDevice _rightXrController;

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
            RefreshXrInputDevices();
            BuildDiegeticLiquidatorWristwatch();
        }

        private void Update()
        {
            PollXrControllers();
            UpdateWristwatchDisplay();
        }

        public void RefreshXrInputDevices()
        {
            _leftXrController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            _rightXrController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            IsXrHardwareConnected = _leftXrController.isValid || _rightXrController.isValid;
        }

        private void PollXrControllers()
        {
            if (!_leftXrController.isValid || !_rightXrController.isValid)
            {
                RefreshXrInputDevices();
            }

            if (DualPhysicsPlayerRig.Instance == null) return;

            if (_leftXrController.isValid && DualPhysicsPlayerRig.Instance.LeftToolHandAnchor != null)
            {
                if (_leftXrController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 lPos))
                {
                    DualPhysicsPlayerRig.Instance.LeftToolHandAnchor.localPosition = lPos;
                }
                if (_leftXrController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion lRot))
                {
                    DualPhysicsPlayerRig.Instance.LeftToolHandAnchor.localRotation = lRot;
                }
            }

            if (_rightXrController.isValid && DualPhysicsPlayerRig.Instance.RightToolHandAnchor != null)
            {
                if (_rightXrController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rPos))
                {
                    DualPhysicsPlayerRig.Instance.RightToolHandAnchor.localPosition = rPos;
                }
                if (_rightXrController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rRot))
                {
                    DualPhysicsPlayerRig.Instance.RightToolHandAnchor.localRotation = rRot;
                }
            }
        }

        /// <summary>
        /// Sends a tactile haptic vibration impulse proportional to rope tension (T / T_max)
        /// or lifted furniture weight to the player's OpenXR motion controllers.
        /// </summary>
        public float TriggerControllerHapticPulse(bool leftHand, float tensionOrMassRatio, float durationSec = 0.08f)
        {
            float amp = Mathf.Clamp(tensionOrMassRatio, 0.05f, 1.0f);
            LastHapticAmplitude = amp;
            TotalHapticPulsesSent++;

            InputDevice dev = leftHand ? _leftXrController : _rightXrController;
            if (dev.isValid && dev.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
            {
                dev.SendHapticImpulse(0u, amp, durationSec);
            }

            return amp;
        }

        public void BuildDiegeticLiquidatorWristwatch()
        {
            Transform parentWrist = DualPhysicsPlayerRig.Instance != null && DualPhysicsPlayerRig.Instance.LeftToolHandAnchor != null
                ? DualPhysicsPlayerRig.Instance.LeftToolHandAnchor
                : transform;

            WristwatchRoot = new GameObject("Diegetic_Liquidator_Wristwatch");
            WristwatchRoot.transform.SetParent(parentWrist, false);
            WristwatchRoot.transform.localPosition = new Vector3(0.05f, 0.04f, -0.08f);
            WristwatchRoot.transform.localRotation = Quaternion.Euler(25f, 0f, -15f);

            // Watch Casing
            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            casing.name = "WatchCasing";
            casing.transform.SetParent(WristwatchRoot.transform, false);
            casing.transform.localScale = new Vector3(0.09f, 0.02f, 0.07f);
            Destroy(casing.GetComponent<Collider>());
            Renderer cr = casing.GetComponent<Renderer>();
            if (cr != null) cr.material.color = new Color(0.12f, 0.14f, 0.16f);

            // Glowing LCD Faceplate
            GameObject lcdObj = new GameObject("WatchLCDText");
            lcdObj.transform.SetParent(WristwatchRoot.transform, false);
            lcdObj.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            lcdObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            WristwatchReadoutText = lcdObj.AddComponent<TextMesh>();
            WristwatchReadoutText.characterSize = 0.0085f;
            WristwatchReadoutText.fontSize = 32;
            WristwatchReadoutText.anchor = TextAnchor.MiddleCenter;
            WristwatchReadoutText.alignment = TextAlignment.Center;
            WristwatchReadoutText.color = new Color(0.25f, 1.0f, 0.55f);

            UpdateWristwatchDisplay();
        }

        public void UpdateWristwatchDisplay()
        {
            if (WristwatchReadoutText == null) return;

            float depthY = DualPhysicsPlayerRig.Instance != null
                ? DualPhysicsPlayerRig.Instance.transform.position.y
                : 0f;
            int cash = LobbyManager.Instance != null ? LobbyManager.Instance.BankedCash : 450;
            int quota = LobbyManager.Instance != null ? LobbyManager.Instance.CurrentQuotaTarget : 1500;
            int day = LobbyManager.Instance != null ? LobbyManager.Instance.CurrentDay : 1;
            string room = LobbyManager.Instance != null ? LobbyManager.Instance.CurrentRoomCode : "SL-0000";

            LastFormattedWatchText =
                $"DEPTH: {depthY:F1}m | DAY {day}/3\n" +
                $"${cash} / ${quota} [{room}]";
            WristwatchReadoutText.text = LastFormattedWatchText;
        }
    }
}
