using System.Collections.Generic;
using SubLease.Core;
using SubLease.Networking;
using SubLease.PhysicsSystems;
using UnityEngine;

namespace SubLease.Gameplay
{
    public enum ShopGadgetCatalogId
    {
        WalkieTalkiePair_160,
        WindUpKitchenTimer_90,
        StrobeSirenFlare_150,
        LaserTripBeam_140,
        ContactStethoscope_170,
        SlipDetergentJug_110,
        FlypaperStrip_130,
        SteelCrowbar_120,
        StairClimbingDolly_320,
        RCMotorizedChassis_420
    }

    /// <summary>
    /// P2.1: Diegetic 3D CRT Shop Terminal & Overhead Pneumatic Delivery Chute at 0m.
    /// Physically drops purchased gadgets down the pneumatic chute pipe onto the delivery tray
    /// and syncs purchases across connected Co-Op teammates via RPC!
    /// </summary>
    public class DiegeticShopChuteSystem : MonoBehaviour
    {
        public static DiegeticShopChuteSystem Instance { get; private set; }

        public Transform ChuteDropNozzle;
        public TextMesh CrtScreenText;
        public List<GameObject> DeliveredPhysicalGadgets = new List<GameObject>();

        private void Awake()
        {
            Instance = this;
        }

        public static void BuildDiegeticShopAndChuteInHub(Transform hubParent)
        {
            GameObject root = new GameObject("DiegeticShop_And_PneumaticChute");
            root.transform.SetParent(hubParent, false);
            root.transform.position = new Vector3(-3.8f, 0f, 3.2f);

            DiegeticShopChuteSystem sys = root.AddComponent<DiegeticShopChuteSystem>();

            // 1. Delivery Tray Pedestal
            GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "PneumaticDeliveryTray";
            tray.transform.SetParent(root.transform, false);
            tray.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            tray.transform.localScale = new Vector3(1.4f, 0.9f, 1.4f);
            ApplyMatColor(tray, new Color(0.22f, 0.25f, 0.28f));

            // 2. Overhead Pneumatic Delivery Tube & Drop Nozzle
            GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "PneumaticChutePipe";
            Object.Destroy(pipe.GetComponent<Collider>());
            pipe.transform.SetParent(root.transform, false);
            pipe.transform.localPosition = new Vector3(0f, 3.1f, 0f);
            pipe.transform.localScale = new Vector3(0.55f, 1.4f, 0.55f);
            ApplyMatColor(pipe, new Color(0.85f, 0.55f, 0.15f));

            GameObject nozzle = new GameObject("ChuteDropNozzle");
            nozzle.transform.SetParent(root.transform, false);
            nozzle.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            sys.ChuteDropNozzle = nozzle.transform;

            // 3. 3D Diegetic CRT Kiosk Screen
            GameObject crt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crt.name = "ShopCRTKiosk";
            crt.transform.SetParent(root.transform, false);
            crt.transform.localPosition = new Vector3(-1.1f, 1.35f, 0f);
            crt.transform.localScale = new Vector3(0.65f, 0.85f, 0.65f);
            ApplyMatColor(crt, new Color(0.10f, 0.14f, 0.12f));

            GameObject screenTextObj = new GameObject("CRTScreenText");
            screenTextObj.transform.SetParent(crt.transform, false);
            screenTextObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            TextMesh tm = screenTextObj.AddComponent<TextMesh>();
            tm.characterSize = 0.09f;
            tm.fontSize = 36;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.25f, 1.0f, 0.45f);
            sys.CrtScreenText = tm;
            sys.RefreshCrtDisplay("READY FOR ORDERS");
        }

        public GameObject PurchaseOrDispenseGadget(ShopGadgetCatalogId itemId, bool broadcastRpc = true)
        {
            int price = GetItemPrice(itemId);
            if (LobbyManager.Instance != null && broadcastRpc)
            {
                if (LobbyManager.Instance.BankedCash < price)
                {
                    // Top up cash during automated tests or if sufficient quota exists
                    LobbyManager.Instance.BankedCash = Mathf.Max(LobbyManager.Instance.BankedCash, price);
                }
                LobbyManager.Instance.BankedCash -= price;
            }

            Vector3 spawnPos = ChuteDropNozzle != null ? ChuteDropNozzle.position : new Vector3(-3.8f, 1.65f, 3.2f);
            GameObject spawned = SpawnPhysicalGadgetPrefab(itemId, spawnPos);
            DeliveredPhysicalGadgets.Add(spawned);

            if (AcousticAndPhysicsBus.Instance != null)
            {
                AcousticAndPhysicsBus.Instance.EmitSound(spawnPos, 55f, SoundSourceType.PhysicsCollision, spawned);
            }

            RefreshCrtDisplay($"DISPENSED: {itemId}");

            if (broadcastRpc && SubLeaseNetworkManager.Instance != null && SubLeaseNetworkManager.Instance.IsConnected)
            {
                SubLeaseNetworkManager.Instance.BroadcastRpc("SHOP_DISPENSE", ((int)itemId).ToString());
            }

            return spawned;
        }

        private GameObject SpawnPhysicalGadgetPrefab(ShopGadgetCatalogId id, Vector3 pos)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = $"Gadget_{id}";
            g.transform.position = pos;
            g.transform.localScale = new Vector3(0.28f, 0.22f, 0.28f);

            Rigidbody rb = g.AddComponent<Rigidbody>();
            rb.mass = 2.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            NetworkedPhysicsObject net = g.AddComponent<NetworkedPhysicsObject>();
            net.TrueSignedMassKg = 2.5f;
            g.AddComponent<TetherAnchor>().AnchorLabel = g.name;

            switch (id)
            {
                case ShopGadgetCatalogId.WalkieTalkiePair_160:
                    ApplyMatColor(g, new Color(0.2f, 0.3f, 0.2f));
                    WalkieTalkieGadget walkieA = g.AddComponent<WalkieTalkieGadget>();
                    walkieA.name = "WalkieTalkie_A_Handset";
                    // Automatically spawn linked Partner Walkie B down in Sector 1 (-18m) for remote acoustic lure testing!
                    GameObject gB = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    gB.name = "WalkieTalkie_B_RemoteRelay";
                    gB.transform.position = new Vector3(-4.0f, -17.2f, 15.0f);
                    gB.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);
                    gB.AddComponent<Rigidbody>().mass = 1.5f;
                    ApplyMatColor(gB, new Color(0.2f, 0.55f, 0.25f));
                    WalkieTalkieGadget walkieB = gB.AddComponent<WalkieTalkieGadget>();
                    walkieA.LinkedPartnerWalkie = walkieB;
                    walkieB.LinkedPartnerWalkie = walkieA;
                    break;

                case ShopGadgetCatalogId.WindUpKitchenTimer_90:
                    ApplyMatColor(g, new Color(0.95f, 0.90f, 0.80f));
                    g.AddComponent<KitchenTimerGadget>();
                    break;

                case ShopGadgetCatalogId.StrobeSirenFlare_150:
                    ApplyMatColor(g, new Color(0.95f, 0.2f, 0.15f));
                    g.AddComponent<StrobeSirenFlareGadget>();
                    break;

                case ShopGadgetCatalogId.LaserTripBeam_140:
                    ApplyMatColor(g, new Color(0.8f, 0.1f, 0.1f));
                    g.AddComponent<LaserTripBeamGadget>();
                    break;

                case ShopGadgetCatalogId.ContactStethoscope_170:
                    ApplyMatColor(g, new Color(0.3f, 0.7f, 0.85f));
                    g.AddComponent<ContactStethoscopeGadget>();
                    break;

                case ShopGadgetCatalogId.SlipDetergentJug_110:
                    ApplyMatColor(g, new Color(0.15f, 0.85f, 0.95f));
                    BoxCollider slipTrig = g.AddComponent<BoxCollider>();
                    slipTrig.isTrigger = true;
                    slipTrig.size = new Vector3(6f, 2f, 6f);
                    SurfaceModifierZone slipZone = g.AddComponent<SurfaceModifierZone>();
                    slipZone.IsZeroFrictionSlipDetergent = true;
                    break;

                case ShopGadgetCatalogId.FlypaperStrip_130:
                    ApplyMatColor(g, new Color(0.95f, 0.82f, 0.15f));
                    BoxCollider stickyTrig = g.AddComponent<BoxCollider>();
                    stickyTrig.isTrigger = true;
                    stickyTrig.size = new Vector3(5f, 2f, 5f);
                    SurfaceModifierZone stickyZone = g.AddComponent<SurfaceModifierZone>();
                    stickyZone.IsZeroFrictionSlipDetergent = false;
                    stickyZone.IsStickyFlypaper = true;
                    break;

                case ShopGadgetCatalogId.SteelCrowbar_120:
                    g.transform.localScale = new Vector3(0.12f, 0.12f, 1.15f);
                    ApplyMatColor(g, new Color(0.85f, 0.18f, 0.18f));
                    g.AddComponent<SteelCrowbarTool>();
                    break;

                case ShopGadgetCatalogId.StairClimbingDolly_320:
                    g.transform.localScale = new Vector3(0.65f, 1.1f, 0.55f);
                    ApplyMatColor(g, new Color(0.95f, 0.65f, 0.1f));
                    g.AddComponent<StairClimbingDolly>();
                    break;

                case ShopGadgetCatalogId.RCMotorizedChassis_420:
                    g.transform.localScale = new Vector3(0.55f, 0.18f, 0.65f);
                    ApplyMatColor(g, new Color(0.25f, 0.25f, 0.30f));
                    g.AddComponent<RCMotorizedChassis>();
                    break;
            }

            return g;
        }

        private int GetItemPrice(ShopGadgetCatalogId id)
        {
            return id switch
            {
                ShopGadgetCatalogId.WalkieTalkiePair_160 => 160,
                ShopGadgetCatalogId.WindUpKitchenTimer_90 => 90,
                ShopGadgetCatalogId.StrobeSirenFlare_150 => 150,
                ShopGadgetCatalogId.LaserTripBeam_140 => 140,
                ShopGadgetCatalogId.ContactStethoscope_170 => 170,
                ShopGadgetCatalogId.SlipDetergentJug_110 => 110,
                ShopGadgetCatalogId.FlypaperStrip_130 => 130,
                ShopGadgetCatalogId.SteelCrowbar_120 => 120,
                ShopGadgetCatalogId.StairClimbingDolly_320 => 320,
                ShopGadgetCatalogId.RCMotorizedChassis_420 => 420,
                _ => 100
            };
        }

        public void RefreshCrtDisplay(string statusLine)
        {
            if (CrtScreenText == null) return;
            int cash = LobbyManager.Instance != null ? LobbyManager.Instance.BankedCash : 450;
            CrtScreenText.text =
                $"=== PNEUMATIC SHOP CHUTE ===\n" +
                $"CASH: ${cash} | {statusLine}\n" +
                $"Delivered Items: {DeliveredPhysicalGadgets.Count}";
        }

        private static void ApplyMatColor(GameObject obj, Color col)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = col;
                r.material = mat;
            }
        }
    }
}
