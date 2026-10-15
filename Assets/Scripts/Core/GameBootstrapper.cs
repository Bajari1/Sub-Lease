using SubLease.Networking;
using SubLease.Player;
using SubLease.World;
using UnityEngine;

namespace SubLease.Core
{
    /// <summary>
    /// Automatically initializes the entire playable SUB-LEASE game (Multiplayer Lobby, Acoustic Bus,
    /// 0m Drop Hatch, Rappel Tube, Sector 1 Furniture Loot, 5 Entities, and Player Rig) on Play.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        private static bool _hasBootstrappedScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnPlay()
        {
            if (FindFirstObjectByType<GameBootstrapper>() != null) return;

            GameObject bootObj = new GameObject("_SUB_LEASE_GameBootstrapper");
            bootObj.AddComponent<GameBootstrapper>();
        }

        private void Awake()
        {
            if (_hasBootstrappedScene && FindFirstObjectByType<DualPhysicsPlayerRig>() != null) return;
            _hasBootstrappedScene = true;

            InitializeCoreManagers();

            if (GameObject.Find("SUB_LEASE_ProceduralWorld") == null)
            {
                ProceduralShaftAndSectorBuilder.BuildCompleteVerticalSliceWorld();
            }

            if (FindFirstObjectByType<DualPhysicsPlayerRig>() == null)
            {
                SpawnPlayerRigAtDropHatch();
            }
        }

        private void InitializeCoreManagers()
        {
            if (FindFirstObjectByType<AcousticAndPhysicsBus>() == null)
            {
                gameObject.AddComponent<AcousticAndPhysicsBus>();
            }
            if (FindFirstObjectByType<SubLeaseNetworkManager>() == null)
            {
                gameObject.AddComponent<SubLeaseNetworkManager>();
            }
            if (FindFirstObjectByType<LobbyManager>() == null)
            {
                gameObject.AddComponent<LobbyManager>();
            }
            if (FindFirstObjectByType<VoiceChatAcousticBridge>() == null)
            {
                gameObject.AddComponent<VoiceChatAcousticBridge>();
            }
            if (FindFirstObjectByType<CoOpSandboxieTestHarness>() == null)
            {
                gameObject.AddComponent<CoOpSandboxieTestHarness>();
            }
        }

        private void SpawnPlayerRigAtDropHatch()
        {
            // Remove any default static Main Camera if one exists so it doesn't conflict with PlayerRig camera
            Camera existingCam = Camera.main;
            if (existingCam != null && existingCam.GetComponentInParent<DualPhysicsPlayerRig>() == null)
            {
                Destroy(existingCam.gameObject);
            }

            GameObject playerObj = new GameObject("LocalLiquidator_PlayerRig");
            playerObj.transform.position = new Vector3(0f, 1.15f, -3.8f);
            playerObj.transform.rotation = Quaternion.identity;

            CharacterController cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = Vector3.zero;

            playerObj.AddComponent<DualPhysicsPlayerRig>();
        }
    }
}
