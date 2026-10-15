# SUB-LEASE: Prioritized Development Work Layout & Roadmap (ALL STAGES P0–P5 COMPLETED & VERIFIED)

This document tracks the full engineering execution order for ***SUB-LEASE***, organized from **P0 (Completed Foundation)** through **P5 (Polish, Optimization & Launch)**. All 6 stages (**P0–P5**) have been implemented, built to `Builds/Windows/SubLease.exe` via Unity 6 (`6000.4.0f1`), and verified with 2-Client Co-Op testing (Host + Sandboxie `DefaultBox` Secondary Steam Account Client).

---

## ✅ P0: Completed Foundation (Done & Verified in Unity 6 — Commit `fc4fb6b`)
- [x] **Master Game Design & Item Compendium:** [SUB_LEASE_Master_GDD.pdf](./SUB_LEASE_Master_GDD.pdf), [SUB_LEASE_Master_GDD.md](./SUB_LEASE_Master_GDD.md), and [SUB_LEASE_Master_GDD.html](./SUB_LEASE_Master_GDD.html).
- [x] **Unity 6 (`6000.4.0f1`) Project & Default Playable Scene:** [SubLease_DropHatch_Main.unity](./Assets/Scenes/SubLease_DropHatch_Main.unity) generated and compile-verified (`ExitCode: 0`).
- [x] **Multiplayer & Co-Op Lobby Integration:** [LobbyManager.cs](./Assets/Scripts/Networking/LobbyManager.cs) (6-char Room Codes `SL-XXXX`, UDP LAN Auto-Discovery on Port `47777`, 4 Work Belt Loadouts, and 1-Click AI Co-Op Crewmate Bot spawner) + [SubLeaseNetworkManager.cs](./Assets/Scripts/Networking/SubLeaseNetworkManager.cs) (`Port 47778` state sync + 3D Crew Avatars).
- [x] **Cooperative Physics Authority & Voice-Chat Acoustic Bridge:** [NetworkedPhysicsObject.cs](./Assets/Scripts/Networking/NetworkedPhysicsObject.cs) (multi-player lift assistance, negative-mass `-50kg` upward gravity, surface modifiers) + `VoiceChatAcousticBridge` (live microphone RMS -> dB emission).
- [x] **Universal Physics Toolkit (Baseline):** [TetherAndRappelSystem.cs](./Assets/Scripts/PhysicsSystems/TetherAndRappelSystem.cs) (rope anchoring, Newton break limits, `2x`/`4x` pulley advantage, tripwires, grappling hooks) + [DuctTapeAndGadgetSystem.cs](./Assets/Scripts/PhysicsSystems/DuctTapeAndGadgetSystem.cs) (tape welding, CO2 lift bags, 3-mode Flashlight/Halogen/UV).
- [x] **Core Furniture Loot, Cargo Scale & 5 Entities (Baseline):** [LootAppraisalAndQuotaManager.cs](./Assets/Scripts/Gameplay/LootAppraisalAndQuotaManager.cs), [EntityEcosystemAI.cs](./Assets/Scripts/AI/EntityEcosystemAI.cs), and [ProceduralShaftAndSectorBuilder.cs](./Assets/Scripts/World/ProceduralShaftAndSectorBuilder.cs).

---

## ✅ P1: Deep VR Hand Physics, Two-Handed Rigging & Tactile Rappelling (Completed & Verified — Commit `6fede5b`)
*Goal: Make every physical interaction in VR and Desktop Sim feel heavy, mechanical, and responsive.*

- [x] **Two-Handed Independent PD-Joint Grabbing & $90^\circ$ Doorway Rotation ([`DualPhysicsPlayerRig.cs`](./Assets/Scripts/Player/DualPhysicsPlayerRig.cs)):** Independent Left & Right Hand `AddForceAtPosition` springs (`2,800N` combined lift) + `[Z]`/`[X]` $90^\circ$ roll torque so `1.8m`-wide Mahogany Desks rotate onto their side to fit through doorways.
- [x] **Physical 3D Waist Belt Holster Ring ([`WaistBeltHolsterSystem.cs`](./Assets/Scripts/Player/WaistBeltHolsterSystem.cs)):** 6 physical holster pouches around the player's waist that track horizontal yaw and equip tools when looked at and clicked.
- [x] **Hand-Over-Hand Pulley Hoisting & Ratchet One-Way Shaft Cleats ([`RatchetOneWayCleat`](./Assets/Scripts/Player/WaistBeltHolsterSystem.cs#L129-L163)):** Co-op `HOIST_PULL` RPC multiplication across connected clients + one-way ratcheting so `115kg` sofas hoisted up the `-18m` tube lock mid-air when released.
- [x] **Automated 2-Client Sandboxie Co-Op Verification ([`CoOpSandboxieTestHarness.cs`](./Assets/Scripts/Networking/CoOpSandboxieTestHarness.cs)):** Built `Builds/Windows/SubLease.exe` and verified Host + Sandboxie (`DefaultBox`) Client UDP handshake and cooperative pulley hoisting (`-16.82m -> -0.95m`).

---

## ✅ P2: Full 36-Item Shop & Physical Contraption Mechanics (Completed & Verified — Commit `c19d15b`)
*Goal: Expand from the baseline starter tools to all 36 physical items in the Item Bible.*

- [x] **Physical Drop Hatch Shop Terminal & Delivery Chute ([`DiegeticShopChuteSystem.cs`](./Assets/Scripts/Gameplay/DiegeticShopChuteSystem.cs)):**
   - 3D Diegetic CRT Terminal & Overhead Pneumatic Delivery Chute in the `0m` Cargo Van room with `SHOP_DISPENSE` multiplayer RPC synchronization that physically drops purchased items onto the floor.
- [x] **Remaining High-Priority Physical Gadgets ([`AdvancedPhysicalGadgets.cs`](./Assets/Scripts/PhysicsSystems/AdvancedPhysicalGadgets.cs)):**
   - **Walkie-Talkie Pair (`F3`):** Real-time spatial audio relay between Walkie A and Walkie B (`TransmitAudioToPartner` -> `98 dB` remote emission 100m away).
   - **Wind-Up Kitchen Timer (`F1`) & Strobe Flare (`C5`):** Physical twist-dial countdown lures (`115 dB` / `110 dB`) that can be duct-taped onto rolling chairs or monsters.
   - **Laser Trip-Beam Pointer (`C6`) & Contact Stethoscope (`F5`):** Early-warning beam trip detector and `48 BPM` Mimic biological heartbeat detector (`InspectTargetForMimicHeartbeat`).
   - **Industrial Slip-Detergent Jug (`E4`) & Flypaper Strip (`D6`):** Zero-friction floor puddle zones (`friction = 0.005`) and high-drag sticky strips (`900N` shear resistance).
   - **Steel Crowbar (`E2`), Stair-Climbing Dolly (`E6`) & RC Motorized Chassis (`F4`):** `5x` fulcrum prying (`2,750N` output from `550N` input), upright furniture stabilization dolly, and remote-controlled motorized decoy buggy.

---

## ✅ P3: Multi-Joint Procedural Entity Rigging & AI Polish (Completed & Verified — Commit `cdd4112`)
*Goal: Upgrade the 5 Entities from single-collider physics prototypes to terrifying multi-jointed active ragdolls.*

- [x] **Procedural Multi-Joint Active Ragdoll Bodies ([`ProceduralMultiJointEntityRig.cs`](./Assets/Scripts/AI/ProceduralMultiJointEntityRig.cs)):**
   - **The Ram:** 4-legged piston/hoof joint rig that physically stumbles when front shins hit a tripwire or slip decal.
   - **The Seep:** 10-segment articulated spine (`10/10` segments) that bends around corners and squeezes under `15cm` door gaps.
   - **The Mimic:** 4-mandible unfolding jaw array + visual yellow Kevlar Tape capture cocoon that binds its jaws shut for the `$2,500` live capture bounty.
   - **The Counterweight:** Retractable ceiling winch drum + fleshy biological snare cable.
   - **The Tuning Fork:** 7-rod vibrating metallic array (`7/7` rods) that visually oscillates at nearby collision frequencies.
- [x] **Door & Barricade Breach AI States:**
   - Entities physically test tied/wedged doors (`TestAndAttemptDoorBreach`) against barricade resistance (`1,200N` rope/wedge lock) and automatically reroute through vents when blocked (`SeepVentReroute=True`).

---

## ✅ P4: Multi-Depth Procedural Sectors (`-38m` to `-74m`) & 3 Apex Shaft Bosses (Completed & Verified — Commit `9328c07`)
*Goal: Build out the deeper vertical biomes and their signature macro-physics anomalies.*

- [x] **Sector 2 (`-38m`) — The Brutalist Archive ([`DeepMacroPhysicsSectorsAndBosses.cs`](./Assets/Scripts/World/DeepMacroPhysicsSectorsAndBosses.cs)):**
   - Multi-story concrete atrium, suspended **$3,500 Venetian Crystal Chandelier**, **$2,800 Polybius Prototype Arcade Cabinet** (`65 dB` hum), and **Apex Boss 1: *The Pendulum*** (defeated by anchoring a `6,000N` Braided Steel Cable at peak swing to collapse its support column).
- [x] **Sector 3 (`-56m`) — The Deep Poolrooms:**
   - Flooded sump with **Archimedes Buoyancy (`ArchimedesBuoyancyZone`, $F_b = \rho V g$)**, **$3,200 Gilded Baroque Mirror**, and **Apex Boss 2: *The Undertow*** (defeated by attaching `3x` CO2 Buoyancy Lift Bags for `+2,796N` upward force).
- [x] **Sector 4 (`-74m`) — The Tilted Hotel & HVAC Wind Chutes:**
   - Rotating gimbal floor (`TiltedHotelAndWindChuteZone`, $45^\circ$ tilt) + HVAC updraft wind tunnel (`+32 m/s^2`), **$4,200 Gyroscopic Brass Armillary Sphere**, **$5,000 Superconducting Server Monolith**, and **Apex Boss 3: *The Centrifuge*** (defeated by taping Caster Wheels to the `160kg` Server Monolith and launching it into the turbine suction core).

---

## ✅ P5: Steamworks / Quest 3 Networking, Performance Culling & Streamer Cam (Completed & Verified)
*Goal: Prepare for PCVR Steam Next Fest Demo & Meta Quest 3 Standalone build.*

- [x] **Steamworks P2P / Unity Relay Transport Plug-In ([`SteamAndRelayTransportBridge.cs`](./Assets/Scripts/Networking/SteamAndRelayTransportBridge.cs)):**
   - Bridges `LobbyManager` 6-character Room Codes (`SL-XXXX`) with Steam Lobby Invite URIs (`steam://joinlobby/480/...`), Relay Allocation Tokens (`RELAY-ALLOC-...`), and automatic Sandboxie multi-account Steam isolation (`bajari_SteamMain` vs `Sandboxie_SteamAcct_2`).
- [x] **Quest 3 Performance Guardrails ([`RoomPhysicsCuller`](./Assets/Scripts/Core/QuestOptimizationAndStreamerCam.cs)):**
   - Enforces the `Max 30 Awake Rigidbodies per Room` cap (`AwakeRBs=12/30`), locks `Time.fixedDeltaTime = 1f / 72f` (72Hz Quest 3 solver), and sleeps distant undisturbed rigidbodies (`>28m`).
- [x] **Streamer Spectator Cam & Picture-in-Picture Trap Cam ([`StreamerSpectatorCam`](./Assets/Scripts/Core/QuestOptimizationAndStreamerCam.cs)):**
   - Smoothed wide-FOV (`92°`) desktop spectator camera with a live `[F9]` Picture-in-Picture **"Trap Cam"** (`256x144` RenderTexture) monitoring the Abyssal Shaft and rigged traps.

---

# 🗺️ PHASE 2 ROADMAP: Production Polish, Seeded Replayability, Audio/Visual Overhaul & Hardware Release (P6–P10)

With the entire mechanical, networking, AI, and physics simulation foundation (**P0–P5**) complete and verified across two clients, **Phase 2** transforms the systemic greybox build into a commercial-grade PCVR, Desktop Co-Op, and Meta Quest 3 release.

---

## 🔄 P6: Seeded Procedural Shift Loop, Multi-Day Quota Progression & "Abyssal Eviction"
*Goal: Turn the vertical slice into an infinitely replayable roguelite extraction loop.*

1. **Seeded Procedural Floor & Shaft Generator (`SeededShiftGenerator.cs`):**
   - Synchronize a `uint ShiftSeed` via [`LobbyManager.cs`](./Assets/Scripts/Networking/LobbyManager.cs) across all connected clients.
   - Procedurally vary room layouts, doorway bottlenecks (forcing $90^\circ$ desk rotations or window rappels), shaft drop offsets, loot spawn tables, and entity starting vents per shift seed.
2. **Physical Blast-Door Extraction Lever & 3-Day Quota Cycle:**
   - Add a physical **Hydraulic Extraction Lever** in the `0m` Drop Hatch room. Pulling it seals the shaft blast doors, cashes in all furniture on the [`CargoScaleZone`](./Assets/Scripts/Gameplay/LootAppraisalAndQuotaManager.cs), repairs/restocks the room, and advances to the next Shift Day (`Day 1 -> Day 2 -> Day 3`) with escalating quotas (`$1,500 -> $2,800 -> $4,500+`).
3. **Quota Failure Consequence ("The Abyssal Eviction"):**
   - If the crew fails to meet the 3-Day Quota when pulling the lever on Day 3, trigger the **Abyssal Eviction** sequence: sirens blare, the `0m` Drop Hatch floor unlatches, and the entire room plummets into the `-74m` Centrifuge shaft.

---

## 🎨 P7: Liminal PBR Visual Overhaul, Volumetric Shaft Lighting & Fracture VFX
*Goal: Replace solid-color greybox materials with atmospheric Kane Pixels-style liminal aesthetics and tactile destruction feedback.*

1. **Procedural Liminal PBR Surface & Texture Generator (`LiminalMaterialGenerator.cs`):**
   - Generate procedural high-contrast normal/albedo textures at runtime for **1990s damp yellow chevron wallpaper**, **stained acoustic ceiling tiles**, **wet brutalist board-formed concrete**, **glossy Poolrooms ceramic tiles**, and **wood/leather/velvet/marble furniture finishes**.
2. **Flickering Fluorescent Ballasts & Volumetric Shaft Fog:**
   - Add dynamic fluorescent ceiling light fixtures that flicker when entities pass underneath or when heavy collisions shake the sector, paired with depth-graded abyssal fog (`#d4cf99` yellow haze at `-18m` -> `#0c2836` deep aqua at `-56m` -> `#1a0d0a` industrial rust at `-74m`).
3. **Physical Shatter, Spark & Markdown VFX (`LootDestructionVfxSystem.cs`):**
   - Spawn physical glass shards, wood splinters, or electrical arcs when fragile furniture (*Venetian Crystal Chandelier*, *Gilded Baroque Mirror*, *Polybius Arcade Cabinet*) impacts surfaces $>5\text{ m/s}$, accompanied by floating 3D `-$350 SCRATCHED!` markdown popups.

---

## 🔊 P8: Multi-Channel Spatial 3D Audio Synthesizer & Shaft Reverb
*Goal: Make every rope strain, footsteps, monster rattle, and furniture scrape audible in 3D space without relying on external audio files.*

1. **Procedural 3D Foley & Mechanical Audio Synthesizer (`ProceduralAudioSynthesizer.cs`):**
   - Synthesize real-time spatialized `AudioClip` waveforms on every physical interaction:
     - **Rope Strain Creak:** Pitch and click rate scale continuously with Newton tension ratio ($T / T_{\max}$) between `75%` and `99%` right before a rope snaps.
     - **Ratchet Winch & Pulley Clicks:** Crisp metallic ratcheting clicks on every hand-over-hand hoist pull.
     - **Duct Tape Rip & Piton Impact:** High-frequency adhesive tear and resonant steel-on-concrete hammer ring.
     - **Entity Signatures:** Heavy pneumatic hoof impacts (*The Ram*), wet chitinous slithering (*The Seep*), 48 BPM muffled organic heartbeat (*The Mimic*), ceiling winch cable whine (*The Counterweight*), and metallic harmonic resonance (*The Tuning Fork*).
2. **Acoustic Shaft Reverb & Occlusion:**
   - Apply distance-based low-pass filtering through closed fire doors and vertical shaft echo reflections.

---

## 🥽 P9: Native OpenXR 6-DOF Hardware Integration & Diegetic Liquidator Wristwatch
*Goal: Full plug-and-play VR headset + motion controller support alongside Desktop Co-Op cross-play.*

1. **OpenXR 6-DOF Controller & Haptics Bridge (`OpenXRHardwareBridge.cs`):**
   - Bind `UnityEngine.XR.InputDevices` (Headset, Left Controller, Right Controller) directly into [`DualPhysicsPlayerRig.cs`](./Assets/Scripts/Player/DualPhysicsPlayerRig.cs) so VR players physically reach, grab (`Grip`), climb ropes hand-over-hand, weld duct tape (`Trigger`), and feel controller haptic pulses proportional to object weight and rope tension.
2. **Diegetic 3D Liquidator Wristwatch (`LiquidatorWristwatchUI.cs`):**
   - Mount a physical 3D retro digital watch on the player's left wrist displaying live **Depth (`-38m`)**, **Banked Cash & Quota (`$1,500`)**, **Live Voice Mic Decibel Bar (`dB`)**, and **Lobby Room Code (`SL-XXXX`)**, allowing players to toggle off the debug `OnGUI` overlay (`[F1]`) for 100% diegetic immersion.

---

## 📦 P10: Meta Quest 3 APK Build Target, Persistent Save Profiles & Steam Next Fest Packaging
*Goal: Ship-ready builds for both Windows PCVR/Desktop and Meta Quest 3 Standalone.*

1. **Automated Meta Quest 3 APK Builder (`BuildQuest3StandaloneApk`):**
   - Extend [`SubLeaseEditorBuilder.cs`](./Assets/Scripts/Editor/SubLeaseEditorBuilder.cs) to compile an IL2CPP ARM64 Android APK (`Builds/Quest3/SubLease_Quest3.apk`) pre-configured with OpenXR and the `RoomPhysicsCuller` 72Hz guardrail.
2. **Persistent Liquidator Career Save System (`CareerProgressionManager.cs`):**
   - Save unlocked Work Belt perks, lifetime extracted dollars, deepest sector reached, and custom hazmat suit colors across sessions.

