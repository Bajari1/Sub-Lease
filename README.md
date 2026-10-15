# SUB-LEASE (`Argon`)
**Physics-Driven Co-Op & Solo VR Liminal Furniture Heist & Vertical Extraction Sim**  
*Built on Unity 6 (`6000.4.0f1`) + PhysX 5 + OpenXR / Desktop Physics Simulator*

📄 **Design Documentation:**
* [SUB_LEASE_Master_GDD.pdf](./SUB_LEASE_Master_GDD.pdf) — Complete 7-Page Master GDD & 36-Item Compendium
* [SUB_LEASE_Master_GDD.md](./SUB_LEASE_Master_GDD.md) — Editable Markdown Specification

---

## Quick Start (Zero Manual Scene Setup)
1. Open `e:\Projects\Games\Argon` in **Unity 6 (`6000.4.0f1`)**.
2. Open `Assets/Scenes/SubLease_DropHatch_Main.unity` (or press **Play** in *any* scene—`GameBootstrapper` automatically constructs the entire 3D world, Co-Op Lobby, Rappel Tube, Loot Furniture, and Entities at runtime).
3. Press **`[Tab]`** to toggle the **Co-Op Lobby & Drop Hatch Terminal** (Host a Room Code, Join over LAN/IP, or click **"+ Spawn Co-Op Crewmate Bot"** to test cooperative heavy lifting solo!).

---

## Controls (Desktop Physics Simulator & OpenXR Ready)
| Key / Input | Action |
| :--- | :--- |
| **`WASD` + `Mouse`** | Move & Look |
| **`[Tab]`** | Toggle **Co-Op Lobby, Room Code & Equipment Shop Terminal** |
| **`[R]`** | Toggle **Waist Brake Carabiner Lock-Off** inside vertical rappel shafts (`Space` to ascend, `C` to brake-slide down with both hands free!) |
| **`[1]` — Physics Hands** | Left-Click to Grab/Carry furniture, Scroll Wheel to push/pull, **`[G]`** to attach **4x Swivel Caster Wheels**, **`[B]`** to attach **Inflatable CO2 Lift Bag (`+95kg`)** |
| **`[2]` — Rope & Pulley Rig** | Click Anchor A -> (Optional Overhead Pulley Hook) -> Click Anchor B to string a rope or shin-height tripwire. Scroll Wheel hoists/shortens rope; Right-Click severs rope. |
| **`[3]` — Grappling Hook** | Left-Click to launch a tethered **3-Pronged Grapple Claw** across shafts or onto distant furniture |
| **`[4]` — Duct Tape Gun** | Click Surface A -> Click Surface B to weld props together, seal vents, or wrap **Kevlar Tape** around a sleeping Mimic |
| **`[5]` / `[F]` — Flashlight** | Cycle between **Standard Work Beam**, **10,000-Lumen Halogen Floodlight** (blinds *The Counterweight* & *The Seep*), and **365nm UV Blacklight** (exposes glowing veins on sleeping Mimics!) |
| **`[6]` — Digital Scale** | Aim at furniture to inspect exact mass in `kg` (exposes disguised `95.0 kg` Mimics!) |
| **`[T]`** | Cycle active Rope Grade (`Hemp 800N` / `Dynamic 2200N` / `Steel 6000N` / `Bungee`) or Tape Variant (`Grey` / `Kevlar` / `Acoustic` / `Rubber` / `Bubble-Wrap`) |
| **`[E]`** | Cash out all furniture resting on the **0m Cargo Extraction Scale** |
| **`[H]` / `[V]`** | **`[H]`** Blast `135 dB` Compressed Air-Horn (stuns *The Tuning Fork* for 6s) \| **`[V]`** Test Proximity Voice-Chat Scream (`92 dB`) |

---

## Core C# Architecture (`Assets/Scripts/`)
* `Core/GameBootstrapper.cs` — Zero-dependency runtime world & manager initialization.
* `Core/AcousticAndPhysicsBus.cs` — Global collision impulse (`I = m * dv`) sound propagation + real-time 60Hz fluorescent hum synthesizer.
* `Networking/LobbyManager.cs` — 6-Character Room Codes (`SL-XXXX`), automatic UDP LAN discovery (`Port 47777`), Work Belt roles, and AI Co-Op Crewmate bots.
* `Networking/SubLeaseNetworkManager.cs` — Low-latency UDP replication (`Port 47778`) and procedural 3D Crewmate Avatars.
* `Networking/NetworkedPhysicsObject.cs` — Multi-player cooperative lift assistance, negative-mass (`-50kg` Helium-Glass Table) upward gravity, and surface modifiers.
* `PhysicsSystems/TetherAndRappelSystem.cs` — Contextual `TetherAnchor` snapping, Newton tension breaking, `2x`/`4x` pulley advantage, tripwires, and `GrappleHookProjectile`.
* `PhysicsSystems/DuctTapeAndGadgetSystem.cs` — Compound `FixedJoint` welding, `InflatableLiftBag`, and 3-mode `PhysicalFlashlightTool`.
* `Gameplay/LootAppraisalAndQuotaManager.cs` — Furniture resale values, collision dent penalties, Grandfather Clock tilt chimes, Mimic bounties, and `CargoScaleExtractionZone`.
* `AI/EntityEcosystemAI.cs` — Physics-driven AI for **The Ram**, **The Seep**, **The Mimic**, **The Counterweight**, and **The Tuning Fork**, including cross-entity infighting.
* `World/ProceduralShaftAndSectorBuilder.cs` — Procedural builder for the `0m` Drop Hatch, `-18m` Vertical Rappel Tube, and **Sector 1: The Yellow Annex**.
