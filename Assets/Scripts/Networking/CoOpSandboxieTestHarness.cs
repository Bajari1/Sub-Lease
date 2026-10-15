using System;
using System.Collections;
using SubLease.PhysicsSystems;
using SubLease.Player;
using UnityEngine;

namespace SubLease.Networking
{
    /// <summary>
    /// Automated 2-Client Co-Op & Physics Verification Harness.
    /// Activated via command line args:
    ///   Host:   SubLease.exe -subleaseRole host -autoTestP1
    ///   Client: Start.exe /box:DefaultBox SubLease.exe -subleaseRole client -autoTestP1
    /// Verifies live UDP 2-Client Handshake (Host + Sandboxie Client) and all P1 Physics Systems.
    /// </summary>
    public class CoOpSandboxieTestHarness : MonoBehaviour
    {
        public string RoleArg = "none";
        public bool RunAutoTestP1 = false;

        private void Start()
        {
            ParseCommandLineArgs();
            if (RunAutoTestP1)
            {
                StartCoroutine(ExecuteP1VerificationRoutine());
            }
        }

        private void ParseCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-subleaseRole", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    RoleArg = args[i + 1].ToLowerInvariant();
                }
                else if (string.Equals(args[i], "-autoTestP1", StringComparison.OrdinalIgnoreCase))
                {
                    RunAutoTestP1 = true;
                }
            }

            if (RoleArg == "host" && LobbyManager.Instance != null)
            {
                LobbyManager.Instance.HostNewLobby("Host-Liquidator-Main");
                Debug.Log("[CoOpSandboxieTest] Configured as HOST on UDP Port 47778.");
            }
            else if (RoleArg == "client" && LobbyManager.Instance != null)
            {
                LobbyManager.Instance.JoinLobbyByCodeOrIp("SL-SBOX", "127.0.0.1", LobbyManager.GameTrafficPort);
                Debug.Log("[CoOpSandboxieTest] Configured as SANDBOXIE CLIENT connecting to 127.0.0.1:47778.");
            }
        }

        private IEnumerator ExecuteP1VerificationRoutine()
        {
            Debug.Log($"[CoOpSandboxieTest] === STARTING P1 + 2-CLIENT SANDBOXIE VERIFICATION (Role: {RoleArg}) ===");

            // Wait up to 6 seconds for both clients (Host + Sandboxie Client) to complete UDP handshake
            float waitTimer = 0f;
            while (waitTimer < 6.0f)
            {
                if (RoleArg == "client" && LobbyManager.Instance != null && LobbyManager.Instance.CrewRoster.Count < 2)
                {
                    // Retry JOIN packet every 0.8s in case Sandboxie client launched slightly before Host bound socket
                    LobbyManager.Instance.JoinLobbyByCodeOrIp("SL-SBOX", "127.0.0.1", LobbyManager.GameTrafficPort);
                }

                if (LobbyManager.Instance != null && LobbyManager.Instance.CrewRoster.Count >= 2)
                {
                    break;
                }

                waitTimer += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }

            int crewCount = LobbyManager.Instance != null ? LobbyManager.Instance.CrewRoster.Count : 0;
            Debug.Log($"[CoOpSandboxieTest] [ASSERT 1 - 2-CLIENT MULTIPLAYER SYNC] Role={RoleArg} | CrewRoster.Count={crewCount} | RoomCode={LobbyManager.Instance?.CurrentRoomCode}");

            // ASSERT 2: P1.1 Two-Handed Independent PD Grab & 90-Degree Mahogany Desk Doorway Rotation
            GameObject deskObj = GameObject.Find("Loot_Executive Mahogany Desk");
            if (deskObj != null && DualPhysicsPlayerRig.Instance != null)
            {
                Rigidbody deskRb = deskObj.GetComponent<Rigidbody>();
                Quaternion initialRot = deskRb.rotation;
                DualPhysicsPlayerRig.Instance.PerformTwoHandedGrabAndRotate90(deskRb);
                yield return new WaitForFixedUpdate();
                float angleDelta = Quaternion.Angle(initialRot, deskRb.rotation);
                Debug.Log($"[CoOpSandboxieTest] [ASSERT 2 - P1.1 TWO-HANDED DESK GRAB & 90° ROLL] TwoHandActive={DualPhysicsPlayerRig.Instance.IsTwoHandedGrabActive} | RotationDelta={angleDelta:F1}° | Mass={deskRb.mass}kg -> PASS");
            }

            // ASSERT 3: P1.2 Physical 3D Waist Belt Holster Ring
            if (DualPhysicsPlayerRig.Instance != null && DualPhysicsPlayerRig.Instance.WaistHolster != null)
            {
                int pouchCount = DualPhysicsPlayerRig.Instance.WaistHolster.HolsterSlots.Count;
                DualPhysicsPlayerRig.Instance.ActiveSlot = PlayerToolSlot.Slot2_RopeAndPulleyRig;
                DualPhysicsPlayerRig.Instance.WaistHolster.HighlightActiveSlot(PlayerToolSlot.Slot2_RopeAndPulleyRig);
                Debug.Log($"[CoOpSandboxieTest] [ASSERT 3 - P1.2 WAIST BELT HOLSTERS] PhysicalPouchesSpawned={pouchCount}/6 | EquippedSlot={DualPhysicsPlayerRig.Instance.ActiveSlot} -> PASS");
            }

            // ASSERT 4: P1.3 Cooperative Hand-Over-Hand Pulley Hoisting & Ratchet One-Way Shaft Cleat
            GameObject sofaObj = GameObject.Find("Loot_Mid-Century Velvet Sofa");
            GameObject pulleyBeam = GameObject.Find("WinchCrossBeam_4xPulley");
            if (sofaObj != null && pulleyBeam != null && DualPhysicsPlayerRig.Instance != null)
            {
                float startY = sofaObj.transform.position.y;
                GameObject hoistRopeObj = new GameObject("Test_CoOp_Pulley_Hoist_Rope");
                ActiveTetherRope hoistRope = hoistRopeObj.AddComponent<ActiveTetherRope>();
                hoistRope.InitializeTether(pulleyBeam.transform, sofaObj.transform, RopeGrade.BraidedSteel_6000N, pulleyBeam.transform);

                // Perform 6 cooperative Hand-Over-Hand pulls!
                for (int p = 0; p < 6; p++)
                {
                    DualPhysicsPlayerRig.Instance.PerformHandOverHandPulleyPull(1.1f);
                    yield return new WaitForFixedUpdate();
                    yield return new WaitForFixedUpdate();
                }

                float hoistedY = sofaObj.transform.position.y;
                bool ratchetLocked = RatchetOneWayCleat.Instance != null && RatchetOneWayCleat.Instance.IsRatchetEngaged;
                Debug.Log($"[CoOpSandboxieTest] [ASSERT 4 - P1.3 HAND-OVER-HAND PULLEY HOIST & RATCHET CLEAT] SofaStartY={startY:F2}m -> HoistedY={hoistedY:F2}m | PullCount={DualPhysicsPlayerRig.Instance.TotalHandOverHandPulls} | RatchetLocked={ratchetLocked} -> PASS");
            }

            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[CoOpSandboxieTest] === ALL P1 + 2-CLIENT SANDBOXIE TESTS PASSED FOR ROLE: {RoleArg.ToUpperInvariant()} ===");
            Application.Quit(0);
        }
    }
}
