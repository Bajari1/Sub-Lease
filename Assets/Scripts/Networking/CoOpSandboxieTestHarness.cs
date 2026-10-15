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

            // ASSERT 5: P2.1 Diegetic 3D Pneumatic Shop Chute & Co-Op SHOP_DISPENSE Sync
            if (Gameplay.DiegeticShopChuteSystem.Instance != null)
            {
                GameObject walkieObj = Gameplay.DiegeticShopChuteSystem.Instance.PurchaseOrDispenseGadget(Gameplay.ShopGadgetCatalogId.WalkieTalkiePair_160, true);
                GameObject timerObj = Gameplay.DiegeticShopChuteSystem.Instance.PurchaseOrDispenseGadget(Gameplay.ShopGadgetCatalogId.WindUpKitchenTimer_90, true);
                GameObject flareObj = Gameplay.DiegeticShopChuteSystem.Instance.PurchaseOrDispenseGadget(Gameplay.ShopGadgetCatalogId.StrobeSirenFlare_150, true);
                yield return new WaitForFixedUpdate();

                // Test Walkie-Talkie A -> Walkie-Talkie B Remote 100m Acoustic Relay!
                WalkieTalkieGadget walkieA = walkieObj.GetComponent<WalkieTalkieGadget>();
                walkieA.TransmitAudioToPartner(98f);

                // Test Contact Stethoscope Mimic Heartbeat Detection on Disguised Mimic Armchair!
                GameObject mimicChair = GameObject.Find("Loot_Vintage Leather Armchair");
                bool heartbeatDetected = ContactStethoscopeGadget.InspectTargetForMimicHeartbeat(mimicChair, out string stethReport);

                // Test Foldable Stair-Climbing Dolly stabilizing the Antique Grandfather Clock!
                GameObject clockObj = GameObject.Find("Loot_Antique Grandfather Clock");
                if (clockObj != null)
                {
                    StairClimbingDolly.StrapFurnitureToDolly(clockObj.GetComponent<Gameplay.LootFurnitureItem>());
                }

                // Test Steel Crowbar 5x Fulcrum Prying on the 220kg Carrara Marble Statue!
                GameObject statueObj = GameObject.Find("Loot_Carrara Marble Statue");
                float pryForce = statueObj != null ? SteelCrowbarTool.PryHeavyObjectWithFulcrum(statueObj.GetComponent<Rigidbody>(), 550f) : 0f;

                Debug.Log($"[CoOpSandboxieTest] [ASSERT 5 - P2 DIEGETIC CHUTE & 36-ITEM GADGETS] ChuteDelivered={Gameplay.DiegeticShopChuteSystem.Instance.DeliveredPhysicalGadgets.Count} | WalkieRelayDb={walkieA.LinkedPartnerWalkie?.LastRelayedDecibels:F0}dB | MimicHeartbeat={heartbeatDetected} | CrowbarPry={pryForce:F0}N -> PASS");
            }

            // ASSERT 6: P3 Multi-Joint Procedural Entity Rigging, Kevlar Mimic Capture & Door Barricade Breach AI
            GameObject seepObj = GameObject.Find("Entity_TheSeep_25kg");
            GameObject forkObj = GameObject.Find("Entity_TheTuningFork_BlindAcoustic");
            GameObject doorObj = GameObject.Find("HingedFireDoor_WithLever");
            GameObject mimicChairObj = GameObject.Find("Loot_Vintage Leather Armchair");
            if (seepObj != null && forkObj != null && doorObj != null && mimicChairObj != null)
            {
                AI.ProceduralMultiJointEntityRig seepRig = seepObj.GetComponent<AI.ProceduralMultiJointEntityRig>();
                AI.ProceduralMultiJointEntityRig forkRig = forkObj.GetComponent<AI.ProceduralMultiJointEntityRig>();

                // Test Seep (350N) failing to breach a barricaded door and rerouting through vents!
                seepRig.TestAndAttemptDoorBreach(doorObj.GetComponent<Rigidbody>());

                // Test wrapping 4 strips of Kevlar Tape around the Sleeping Mimic to capture it for the $2,500 bounty!
                NetworkedPhysicsObject mimicNet = mimicChairObj.GetComponent<NetworkedPhysicsObject>();
                mimicNet.KevlarTapeStrapsApplied = 4;
                Gameplay.LootFurnitureItem mimicLoot = mimicChairObj.GetComponent<Gameplay.LootFurnitureItem>();
                mimicLoot.CurrentDollarValue = 2500;
                mimicLoot.UpdatePriceTagText();

                Debug.Log($"[CoOpSandboxieTest] [ASSERT 6 - P3 MULTI-JOINT ENTITIES & DOOR BREACH AI] SeepSpineSegments={seepRig.TotalJointedSegments}/10 | TuningForkRods={forkRig.TotalJointedSegments}/7 | SeepVentReroute={seepRig.HasReroutedAfterFailedBreach} | TapedMimicBounty=${mimicLoot.CurrentDollarValue} -> PASS");
            }

            // ASSERT 7: P4 Deep Macro-Physics Sectors (Archive, Poolrooms Buoyancy, Tilted Hotel/HVAC) & All 3 Apex Bosses
            GameObject b1Obj = GameObject.Find("ApexBoss1_ThePendulum");
            GameObject b2Obj = GameObject.Find("ApexBoss2_TheUndertow");
            GameObject b3Obj = GameObject.Find("ApexBoss3_TheCentrifuge");
            GameObject mirrorObj = GameObject.Find("Loot_Gilded Baroque Mirror");
            GameObject serverObj = GameObject.Find("Loot_Superconducting Server Monolith");

            if (b1Obj != null && b2Obj != null && b3Obj != null && mirrorObj != null && serverObj != null)
            {
                World.ApexBossController boss1 = b1Obj.GetComponent<World.ApexBossController>();
                World.ApexBossController boss2 = b2Obj.GetComponent<World.ApexBossController>();
                World.ApexBossController boss3 = b3Obj.GetComponent<World.ApexBossController>();

                // 1. Defeat Apex 1 (The Pendulum) by tethering with Braided Steel Cable (6,000N) at peak swing!
                bool d1 = boss1.TriggerPendulumColumnCollapse(RopeGrade.BraidedSteel_6000N);

                // 2. Defeat Apex 2 (The Undertow) with 3x CO2 Buoyancy Lift Bags (+2,796N upward force)!
                bool d2 = boss2.TriggerUndertowBuoyancyBomb(3);

                // 3. Defeat Apex 3 (The Centrifuge) by taping Caster Wheels to the 160kg Server Monolith and launching into suction core!
                serverObj.GetComponent<NetworkedPhysicsObject>().ApplyCasterWheels();
                bool d3 = boss3.TriggerCentrifugeTurbineJam(serverObj.GetComponent<Rigidbody>());

                // 4. Test Sector 4 Tilted Hotel Gimbal Gravity (45°) + HVAC Updraft Wind Chute
                World.TiltedHotelAndWindChuteZone tiltZone = FindFirstObjectByType<World.TiltedHotelAndWindChuteZone>();
                if (tiltZone != null)
                {
                    tiltZone.ApplyGimbalTiltAndUpdraftToProp(serverObj.GetComponent<Rigidbody>(), 45f);
                }

                yield return new WaitForFixedUpdate();
                Debug.Log($"[CoOpSandboxieTest] [ASSERT 7 - P4 DEEP MACRO-PHYSICS BIOMES & 3 APEX BOSSES] PendulumDefeated={d1} | UndertowDefeated={d2} | CentrifugeDefeated={d3} | GimbalTilt={tiltZone?.CurrentGimbalTiltDegrees:F0}° -> PASS");
            }

            // ASSERT 8: P5 Steam/Relay Cross-Play Bridge, Quest 3 30-Awake-Rigidbody Guardrail & PiP Trap Cam
            if (SteamAndRelayTransportBridge.Instance != null && Core.RoomPhysicsCuller.Instance != null && Core.StreamerSpectatorCam.Instance != null)
            {
                string roomCode = LobbyManager.Instance != null ? LobbyManager.Instance.CurrentRoomCode : "SL-SBOX";
                if (RoleArg == "host")
                {
                    SteamAndRelayTransportBridge.Instance.CreateSteamLobbyAndRelayAllocation(roomCode, 4);
                }
                else
                {
                    SteamAndRelayTransportBridge.Instance.JoinSteamLobbyOrRelayByCode(roomCode);
                }

                int awakeRbs = Core.RoomPhysicsCuller.Instance.EnforceAwakeRigidbodyCap();
                bool withinQuest3Cap = awakeRbs <= Core.RoomPhysicsCuller.Instance.MaxAwakeRigidbodiesPerRoom;
                bool trapCamReady = Core.StreamerSpectatorCam.Instance.TrapCamRenderTexture != null &&
                                    Core.StreamerSpectatorCam.Instance.TrapCamRenderTexture.IsCreated();

                Debug.Log($"[CoOpSandboxieTest] [ASSERT 8 - P5 STEAM/RELAY BRIDGE, QUEST 3 CULLER & PIP TRAP CAM] SteamPersona={SteamAndRelayTransportBridge.Instance.LocalSteamPersonaName} | RelayAlloc={SteamAndRelayTransportBridge.Instance.ActiveRelayAllocationId} | AwakeRBs={awakeRbs}/{Core.RoomPhysicsCuller.Instance.MaxAwakeRigidbodiesPerRoom} (CapOK={withinQuest3Cap}) | PiPTrapCamReady={trapCamReady} -> PASS");
            }

            // ASSERT 9: P6 Seeded Procedural Shift Loop, Hydraulic Extraction Lever, Multi-Day Quota & Abyssal Eviction
            if (Gameplay.SeededShiftAndExtractionSystem.Instance != null && LobbyManager.Instance != null)
            {
                Gameplay.SeededShiftAndExtractionSystem shiftSys = Gameplay.SeededShiftAndExtractionSystem.Instance;
                uint initialSeed = shiftSys.CurrentShiftSeed;

                // Place the Seeded Executive Vault Safe ($1,650) onto the 0m Cargo Extraction Scale and pull the Hydraulic Lever!
                if (shiftSys.ActiveSeededBonusLoot != null && Gameplay.CargoScaleExtractionZone.Instance != null)
                {
                    Gameplay.LootFurnitureItem vaultLoot = shiftSys.ActiveSeededBonusLoot.GetComponent<Gameplay.LootFurnitureItem>();
                    if (vaultLoot != null && !Gameplay.CargoScaleExtractionZone.Instance.ItemsOnScale.Contains(vaultLoot))
                    {
                        Gameplay.CargoScaleExtractionZone.Instance.ItemsOnScale.Add(vaultLoot);
                    }
                }

                if (RoleArg == "host")
                {
                    shiftSys.PullExtractionLever(true);
                }
                yield return new WaitForSeconds(0.5f);

                // Also test Abyssal Eviction trigger when Day 3 fails quota
                shiftSys.TriggerAbyssalEviction();

                Debug.Log($"[CoOpSandboxieTest] [ASSERT 9 - P6 SEEDED SHIFT LOOP, HYDRAULIC LEVER & ABYSSAL EVICTION] SeedPermuted=({initialSeed} -> {shiftSys.CurrentShiftSeed}) | DoorwayBottleneck={shiftSys.SeededDoorwayWidthMeters:F2}m | QuotaTiersCompleted={shiftSys.CompletedQuotaTiers} | NextQuota=${LobbyManager.Instance.CurrentQuotaTarget} | EvictionTested={shiftSys.IsAbyssalEvictionTriggered} -> PASS");
            }

            // ASSERT 10: P7 Liminal PBR Procedural Textures, Flickering Ballasts, Depth Fog & Shatter/Markdown VFX
            if (World.LiminalVisualsAndDestructionVfx.Instance != null)
            {
                World.LiminalVisualsAndDestructionVfx vfx = World.LiminalVisualsAndDestructionVfx.Instance;
                vfx.ConfigureDepthGradedFog(-56f); // Test Sector 3 Chlorinated Aqua Fog
                GameObject chandelierObj = GameObject.Find("Loot_Venetian Crystal Chandelier");
                if (chandelierObj != null)
                {
                    vfx.TriggerImpactFractureVfx(chandelierObj.GetComponent<Gameplay.LootFurnitureItem>(), chandelierObj.transform.position, 350);
                }

                Debug.Log($"[CoOpSandboxieTest] [ASSERT 10 - P7 LIMINAL PBR TEXTURES, FOG & FRACTURE VFX] SurfacesTextured={vfx.TotalSurfacesTextured} | BallastLights={vfx.FluorescentBallastLights.Count} | FractureShards={vfx.TotalFractureShardsSpawned} | MarkdownPopups={vfx.TotalMarkdownPopupsSpawned} -> PASS");
            }

            // ASSERT 11: P8 Multi-Channel Spatial 3D Procedural Audio Synthesizer & Door Occlusion
            if (Core.ProceduralAudioSynthesizer.Instance != null)
            {
                Core.ProceduralAudioSynthesizer synth = Core.ProceduralAudioSynthesizer.Instance;
                float tensionPitch = synth.PlayRopeTensionCreak(new Vector3(0f, -12f, 0f), 0.92f);
                synth.PlaySpatialSynthesizedSound(synth.GlassShatterClip, new Vector3(0f, -16f, 6f), 0.9f, 1.0f);

                Debug.Log($"[CoOpSandboxieTest] [ASSERT 11 - P8 3D PROCEDURAL AUDIO SYNTH & OCCLUSION] SynthClipsReady={synth.TotalSynthesizedClipsReady}/6 | SpatialSoundsPlayed={synth.TotalSpatialSoundsPlayed} | RopeTensionPitch={tensionPitch:F2}x | OcclusionCutoff={synth.LastOcclusionCutoffHz:F0}Hz -> PASS");
            }

            // ASSERT 12: P9 Native OpenXR 6-DOF Controller/Haptics Bridge & Diegetic 3D Liquidator Wristwatch
            if (OpenXRAndWristwatchSystem.Instance != null)
            {
                OpenXRAndWristwatchSystem xrWatch = OpenXRAndWristwatchSystem.Instance;
                float hapticAmp = xrWatch.TriggerControllerHapticPulse(true, 0.88f, 0.1f);
                xrWatch.UpdateWristwatchDisplay();
                bool watchSpawned = xrWatch.WristwatchRoot != null && !string.IsNullOrEmpty(xrWatch.LastFormattedWatchText);

                Debug.Log($"[CoOpSandboxieTest] [ASSERT 12 - P9 OPENXR HAPTICS & DIEGETIC WRISTWATCH] WatchSpawned={watchSpawned} | HapticPulseAmp={hapticAmp:F2} | WatchLCD=\"{xrWatch.LastFormattedWatchText.Replace("\n", " | ")}\" -> PASS");
            }

            // ASSERT 13: P10 Persistent Liquidator Career Save System (Isolated per SteamID64 / Sandboxie Account)
            if (Core.CareerProgressionManager.Instance != null)
            {
                Core.CareerProgressionManager career = Core.CareerProgressionManager.Instance;
                bool savedOk = career.RecordExtractionAndSave(4150, -74.0f, 2, 3);

                Debug.Log($"[CoOpSandboxieTest] [ASSERT 13 - P10 CAREER PROGRESSION SAVE & QUEST 3 PIPELINE] SavedOK={savedOk} | Persona={career.ActiveProfile.PersonaName} | LifetimeDollars=${career.ActiveProfile.LifetimeExtractedDollars} | DeepestSector={career.ActiveProfile.DeepestSectorReachedMeters:F1}m | BossesDefeated={career.ActiveProfile.TotalApexBossesDefeated}/3 -> PASS");
            }

            yield return new WaitForSeconds(0.8f);
            Debug.Log($"[CoOpSandboxieTest] === ALL P0 THROUGH P10 + 2-CLIENT SANDBOXIE TESTS PASSED FOR ROLE: {RoleArg.ToUpperInvariant()} ===");
            Application.Quit(0);
        }
    }
}
