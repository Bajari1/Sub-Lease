# SUB-LEASE: Prioritized Development Work Layout & Roadmap

This document tracks the full engineering execution order for ***SUB-LEASE***, organized from **P0 (Completed Foundation)** through **P5 (Polish, Optimization & Launch)**.

---

## ✅ P0: Completed Foundation (Done & Verified in Unity 6)
- [x] **Master Game Design & Item Compendium:** [SUB_LEASE_Master_GDD.pdf](./SUB_LEASE_Master_GDD.pdf), [SUB_LEASE_Master_GDD.md](./SUB_LEASE_Master_GDD.md), and [SUB_LEASE_Master_GDD.html](./SUB_LEASE_Master_GDD.html).
- [x] **Unity 6 (`6000.4.0f1`) Project & Default Playable Scene:** [SubLease_DropHatch_Main.unity](./Assets/Scenes/SubLease_DropHatch_Main.unity) generated and compile-verified (`ExitCode: 0`).
- [x] **Multiplayer & Co-Op Lobby Integration:** [LobbyManager.cs](./Assets/Scripts/Networking/LobbyManager.cs) (6-char Room Codes `SL-XXXX`, UDP LAN Auto-Discovery on Port `47777`, 4 Work Belt Loadouts, and 1-Click AI Co-Op Crewmate Bot spawner) + [SubLeaseNetworkManager.cs](./Assets/Scripts/Networking/SubLeaseNetworkManager.cs) (`Port 47778` state sync + 3D Crew Avatars).
- [x] **Cooperative Physics Authority & Voice-Chat Acoustic Bridge:** [NetworkedPhysicsObject.cs](./Assets/Scripts/Networking/NetworkedPhysicsObject.cs) (multi-player lift assistance, negative-mass `-50kg` upward gravity, surface modifiers) + `VoiceChatAcousticBridge` (live microphone RMS -> dB emission).
- [x] **Universal Physics Toolkit (Baseline):** [TetherAndRappelSystem.cs](./Assets/Scripts/PhysicsSystems/TetherAndRappelSystem.cs) (rope anchoring, Newton break limits, `2x`/`4x` pulley advantage, tripwires, grappling hooks) + [DuctTapeAndGadgetSystem.cs](./Assets/Scripts/PhysicsSystems/DuctTapeAndGadgetSystem.cs) (tape welding, CO2 lift bags, 3-mode Flashlight/Halogen/UV).
- [x] **Core Furniture Loot, Cargo Scale & 5 Entities (Baseline):** [LootAppraisalAndQuotaManager.cs](./Assets/Scripts/Gameplay/LootAppraisalAndQuotaManager.cs), [EntityEcosystemAI.cs](./Assets/Scripts/AI/EntityEcosystemAI.cs), and [ProceduralShaftAndSectorBuilder.cs](./Assets/Scripts/World/ProceduralShaftAndSectorBuilder.cs).

---

## ✅ P1: Deep VR Hand Physics, Two-Handed Rigging & Tactile Rappelling (Completed & Verified via 2-Client Sandboxie Test)
*Goal: Make every physical interaction in VR and Desktop Sim feel heavy, mechanical, and responsive.*

- [x] **Two-Handed Independent PD-Joint Grabbing & $90^\circ$ Doorway Rotation ([`DualPhysicsPlayerRig.cs`](./Assets/Scripts/Player/DualPhysicsPlayerRig.cs)):** Independent Left & Right Hand `AddForceAtPosition` springs (`2,800N` combined lift) + `[Z]`/`[X]` $90^\circ$ roll torque so `1.8m`-wide Mahogany Desks rotate onto their side to fit through doorways.
- [x] **Physical 3D Waist Belt Holster Ring ([`WaistBeltHolsterSystem.cs`](./Assets/Scripts/Player/WaistBeltHolsterSystem.cs)):** 6 physical holster pouches around the player's waist that track horizontal yaw and equip tools when looked at and clicked.
- [x] **Hand-Over-Hand Pulley Hoisting & Ratchet One-Way Shaft Cleats ([`RatchetOneWayCleat`](./Assets/Scripts/Player/WaistBeltHolsterSystem.cs#L129-L163)):** Co-op `HOIST_PULL` RPC multiplication across connected clients + one-way ratcheting so `115kg` sofas hoisted up the `-18m` tube lock mid-air when released.
- [x] **Automated 2-Client Sandboxie Co-Op Verification ([`CoOpSandboxieTestHarness.cs`](./Assets/Scripts/Networking/CoOpSandboxieTestHarness.cs)):** Built `Builds/Windows/SubLease.exe` and verified Host + Sandboxie (`DefaultBox`) Client UDP handshake (`Room: SL-9KRE`, `CrewCount: 2`) and cooperative pulley hoisting (`-16.83m -> -0.94m`).

---

## 🛠️ P2: Full 36-Item Shop & Physical Contraption Mechanics
*Goal: Expand from the baseline starter tools to all 36 physical items in the Item Bible.*

1. **Physical Drop Hatch Shop Terminal & Delivery Chute:**
   - Replace the temporary HUD shop buttons with a **3D Diegetic CRT Terminal & Pneumatic Delivery Chute** in the `0m` Cargo Van room that physically drops purchased items (Steel Cables, Kevlar Tape Rolls, Caster Wheel 4-Packs, Kitchen Timers, Walkie-Talkies, Piton Harpoons) onto the floor.
2. **Remaining High-Priority Physical Gadgets:**
   - **Walkie-Talkie Pair (`F3`):** Real-time spatial audio relay between Walkie A and Walkie B so yelling into Walkie A emits an `AcousticEvent` at Walkie B's position 100m away.
   - **Wind-Up Kitchen Timer (`F1`) & Strobe Flare (`C5`):** Physical twist-dial countdown lures that can be duct-taped onto rolling chairs or monsters.
   - **Laser Trip-Beam Pointer (`C6`) & Contact Stethoscope (`F5`):** Early-warning detection tools for stalking entities and sleeping Mimic heartbeats.
   - **Industrial Slip-Detergent Jug (`E4`) & Flypaper Strip (`D6`):** Pourable zero-friction floor decals (`friction = 0.005`) and high-drag sticky strips (`900N` shear resistance).
   - **Steel Crowbar (`E2`) & Stair-Climbing Dolly (`E6`):** Fulcrum prying math ($\tau = r \times F$) for jammed blast doors + zip-lining hook on taut steel cables.

---

## 👹 P3: Multi-Joint Procedural Entity Rigging & AI Polish
*Goal: Upgrade the 5 Entities from single-collider physics prototypes to terrifying multi-jointed active ragdolls.*

1. **Procedural Multi-Joint Active Ragdoll Bodies (`EntityEcosystemAI.cs`):**
   - **The Ram:** 4-legged piston/hoof joint rig that physically stumbles when front shins hit a tripwire or slip decal.
   - **The Seep:** 10-segment articulated `CharacterJoint` spine that bends around corners, squeezes through `15cm` under-door gaps and ceiling vents, and visibly wraps around twine ropes to chew them.
   - **The Mimic:** Procedural jaw/limb unfolding animation when triggered; visual Kevlar Tape wrap mesh that physically binds its jaws shut for the `$2,500` live capture bounty.
   - **The Counterweight:** Retractable ceiling tile trapdoor + biological winch cable with tension strain audio.
   - **The Tuning Fork:** Vibrating metallic tuning-rod array that visually oscillates and rings at the frequency of nearby collisions.
2. **Door & Barricade Breach AI States:**
   - Entities physically test tied/wedged doors (`ApplyTorqueToDoor`), transfer stress into attached ropes/tape welds, and reroute through vents or adjacent halls if barricade resistance exceeds their force limit.

---

## 🏢 P4: Multi-Depth Procedural Sectors (`-100m` to `-600m`) & 3 Apex Shaft Bosses
*Goal: Build out the deeper vertical biomes and their signature macro-physics anomalies.*

1. **Sector 2 (`-100m to -250m`) — The Brutalist Archive:**
   - Multi-story concrete atrium, offset elevator shafts requiring pendulum swings, unpowered counterweight cages, suspended **$3,500 Venetian Crystal Chandeliers**, and **Apex Boss 1: *The Pendulum***.
2. **Sector 3 (`-250m to -400m`) — The Deep Poolrooms:**
   - Flooded vertical sumps with **Archimedes Buoyancy (`F_b = rho * V * g`)** where wooden furniture floats up shafts automatically and marble sinks, zero-friction wet ceramic tiles, and **Apex Boss 2: *The Undertow***.
3. **Sector 4 (`-400m to -600m+`) — The Tilted Hotel & HVAC Wind Chutes:**
   - Rotating gimbal corridors that tilt $45^\circ$ every 3 minutes (causing un-anchored furniture and monsters to avalanche downhill), aerodynamic wind tunnels, anomalous loot (*Gyroscopic Armillary*, *Superconducting Server*), and **Apex Boss 3: *The Centrifuge***.

---

## 🚀 P5: Steamworks / Quest 3 Networking, Performance Culling & Streamer Cam
*Goal: Prepare for PCVR Steam Next Fest Demo & Meta Quest 3 Standalone build.*

1. **Steamworks P2P / Unity Relay Transport Plug-In:**
   - Hook `SubLeaseNetworkManager` into Steam Lobbies / Unity Relay for 1-click friend invites across the internet alongside existing LAN/Room-Code support.
2. **Quest 3 Performance Guardrails (`RoomPhysicsCuller.cs`):**
   - Enforce the `30 Awake Rigidbodies per Room` cap, automatic `Rigidbody.Sleep()` on undisturbed props, and portal airlock culling for rooms 2 doors away to lock 72/90 FPS on Quest 3.
3. **Streamer Spectator Cam (`SpectatorCam.cs`):**
   - Smoothed wide-FOV 1080p desktop monitor view with optional Picture-in-Picture **"Trap Cam"** window so content creators capture viral clips automatically.
