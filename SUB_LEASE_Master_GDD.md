# SUB-LEASE: Master Game Design Document & Item Bible
**Working Titles:** *SUB-LEASE* / *DEADWEIGHT* / *FLOOR MODEL* / *NO-CLIP REPO*  
**Version:** 2.0 — Complete Production & Item Specification  
**Genre:** Physics-Driven Co-Op & Solo Extraction Heist / VR Immersive Sim  
**Target Demographic:** Ages 15–30 (*Lethal Company*, *R.E.P.O.*, *Kane Pixels' Backrooms*, *Boneworks* audience)  
**Target Platforms:** PCVR (SteamVR) & Meta Quest 3 / 3S (Standalone)  
**Engine & Architecture:** Unity 6 (PhysX 5, OpenXR, 100% Procedural C# Systems)

---

## 1. Executive Summary & Mechanical Hook

### 1.1 The 10-Second Pitch
You and your crew breach a vertical maintenance hatch into the Backrooms for one reason: **millions of dollars of untaxed, pristine Mid-Century Modern furniture, crystal chandeliers, arcade cabinets, and anomalous sculptures sitting down there for free.**

Armed with ropes, grappling hooks, flashlights, duct tape, and moving-company hardware, you rappel down vertical concrete tubes into progressively deeper, stranger liminal floors. Your job is to **appraise, steal, protect, and physically hoist heavy furniture back up the shafts** to meet your Cash Quota—while outsmarting physics-driven entities that want to smash your haul (and you).

### 1.2 Why "Stealing Furniture" Is the Ultimate Physics Hook
1. **Your Loot IS Your Barricade (The Greed vs. Survival Dilemma):**
   * Every movable prop in the map is simultaneously **Money ($)** and a **Barricade, Counterweight, or Weapon**.
   * Need to wedge a door shut against a charging monster? That **$850 Mahogany Executive Chair** fits perfectly under the handle—*but every impact dents the wood and knocks $200 off its resale value!*
   * Do you cut the rope on a **$3,500 Venetian Crystal Chandelier** to crush a monster beneath it, saving your life at the cost of your biggest payday?
2. **Furniture Creates Authentic Moving-Company Physics Problems:**
   * A $90 plastic chair weighs `8 kg` and clips easily to your belt, but barely moves the quota needle.
   * A $4,500 Solid Marble Statue weighs `220 kg`, scrapes loudly across concrete (attracting acoustic predators), and will snap a standard rope if you try to hoist it up a 100-foot vertical tube without rigging a compound pulley or dropping a heavier counterweight.
3. **100% Universal Physics (Zero Fake Scripting):**
   * Every tool—from a flashlight to a grappling hook or kitchen timer—is a real `Rigidbody`. You can tie a flashlight to a ceiling rope to make a swinging pendulum spotlight, duct-tape caster wheels under a heavy sofa, or harpoon a charging monster to a 300kg concrete pillar.

---

## 2. The Core Gameplay Loop (15–25 Minute Runs)

1. **Stage 1 — The Cargo Van & Drop Hatch (`0m`):**
   * Check your 3-Day Quota on the terminal, buy ropes, grappling hooks, tape rolls, flashlights, and pulleys from the gear chute, and anchor your primary lines around the Drop Hatch.
2. **Stage 2 — Rappel the Vertical Tubes (`-30m` to `-600m+`):**
   * Clip your waist brake carabiner to a rope, step into the vertical maintenance tube, and rappel down into the megastructure. Deeper shafts introduce offset ledges, angled chutes, wind tunnels, and flooded sumps.
3. **Stage 3 — Recon, Appraise & Pre-Rig:**
   * Scout the floor using flashlights, UV blacklights, and scales. Identify high-value furniture, test suspicious props for sleeping **Mimics**, and pre-rig your escape path (taping vents shut, stringing tripwires, hanging pulley blocks at the shaft entrance).
4. **Stage 4 — The Heist & Haul:**
   * Prep your loot for transport: wrap fragile chandeliers in Bubble-Wrap, duct-tape 4 swivel caster wheels under a `120 kg` velvet sofa, or wrap Acoustic Foam Tape around a Grandfather Clock's pendulum so it doesn't chime while tilted.
5. **Stage 5 — Vertical Shaft Extraction:**
   * Haul your furniture up the vertical tubes using **Compound Pulleys**, **Counterweight Drops** (pushing a worthless `180 kg` soda machine down the shaft to pull your `120 kg` sofa up!), **CO2 Lift Bags**, or **Motorized Winches** while fending off entities alerted by the noise and rope tension.
6. **Stage 6 — Cash Out on the Cargo Scale:**
   * Drop intact furniture (and taped-up live monsters!) onto the Cargo Scale at `0m`, collect cash minus dent penalties, and unlock deeper shafts and crazier tools.

---

## 3. Complete Equipment & Item Compendium (36 Physical Items)

Every item is a physical object that clips to your **Utility Belt** or stows in your **Equipment Crate**. Nothing exists solely in a UI menu.

### Category A: Ropes, Tethers & Rappelling Gear
| # | Item Name | Cost | Physical Parameters | Gameplay & Systemic Physics Uses |
| :- | :--- | :--- | :--- | :--- |
| **A1** | **Hemp Twine Spool** | *Starter* | `Length: 30m` \| `BreakForce: 800N` \| `Mass: 1kg` | Basic door tying, shin-height tripwires, and light rappelling. Snaps if loaded above `80 kg` under gravity (`F = mg`) or if chewed by *The Seep*. |
| **A2** | **Dynamic Climbing Rope** | $250 | `Length: 80m` \| `BreakForce: 2,200N` \| `Stretch: 8%` | Absorbs sudden kinetic shock when rappelling deep shafts or catching falling furniture before it shatters on the floor. |
| **A3** | **Braided Steel Winch Cable** | $650 | `Length: 150m` \| `BreakForce: 6,000N` \| `Conductive: Yes` | Unbreakable by monsters, immune to sharp concrete edges, and **conducts electricity** (connect a broken wall junction box to a metal door handle to shock anything touching the door!). |
| **A4** | **Industrial Bungee Cord** | $300 | `Length: 15m` \| `Spring k: 450 N/m` \| `Break: 2,500N` | Stores massive elastic potential energy (`F = -kx`). Pull a heavy door wide open against a bungee, hold it with thin twine, and let a monster trip the twine to slam the door at 60 mph. |
| **A5** | **Waist Brake Carabiner** | *Starter* | `Friction: Variable (Trigger)` \| `Lock-Off: Toggle` | Clips your waist to any vertical rope. Squeeze trigger to control descent speed, or press Lock-Off to **hang suspended mid-air inside a tube with both hands free** to shoot a grappling hook or tape a vent! |
| **A6** | **Motorized Belt Ascender** | $1,100 | `Lift Force: 3,000N` \| `Speed: 4 m/s` \| `Noise: 85 dB` | Battery-powered winch that zips you and `120 kg` of attached furniture straight up a vertical shaft—*but its loud motor whine attracts The Tuning Fork from three rooms away!* |

---

### Category B: Grappling Hooks, Harpoons & Anchor Hardware
| # | Item Name | Cost | Physical Parameters | Gameplay & Systemic Physics Uses |
| :- | :--- | :--- | :--- | :--- |
| **B1** | **3-Pronged Throwing Grapple** | $200 | `Mass: 3.5kg` \| `Hold Force: 2,000N` \| `Impact: 65 dB` | Tied to any rope end. Swing it in VR like a lasso/pendulum and throw! Clamps onto pipes, railings, chandeliers, or **furniture across a flooded room/shaft** so you can "fish" loot from safety. |
| **B2** | **Pneumatic Piton Harpoon** | $750 | `Range: 35m` \| `Hold Force: 5,000N` \| `6 Air Shots` | Compressed-air launcher that fires a tethered steel spike directly into solid concrete ceilings, shaft walls, or **into an entity's torso**—letting you harpoon *The Ram* and anchor the cable to a 300kg pillar! |
| **B3** | **Silent Magnetic Grapple** | $450 | `Hold Force: 2,500N (Metal Only)` \| `Impact: 5 dB` | Rubber-coated neodymium magnet head. Locks onto metal air ducts, steel doors, filing cabinets, and elevator cages with **zero metallic clatter**, keeping you invisible to *The Tuning Fork*. |
| **B4** | **Suction Cup Pitons (3-Pack)** | $220 | `Hold Force: 1,500N` \| `Reusable` | Press onto any flat surface (tile, glass, drywall, marble) and flip the vacuum lever to create a custom rope anchor, pulley mount, or tripwire post where no pipe exists. |
| **B5** | **Compound Pulley Block** | $350 | `Mechanical Advantage: 2x or 4x` | Clip to the top of a rappel shaft or ceiling hook and thread your rope through the sheaves. Divides effective furniture weight by 4, letting one player lift a `220 kg` marble statue by hand! |
| **B6** | **Ratchet One-Way Cleat** | $180 | `Locks Rope Direction` \| `Max Load: 4,000N` | Mounts next to a shaft opening. As you pull furniture up a tube hand-over-hand, the ratchet automatically locks the rope so if you let go to fight a monster, your sofa doesn't plummet back down the shaft! |

---

### Category C: Flashlights, Illumination & Optical Scanners
In *SUB-LEASE*, light sources are physical rigidbodies you hold, clip to your chest, hang from ropes, or **duct-tape onto moving contraptions**.

| # | Item Name | Cost | Physical Parameters | Gameplay & Systemic Physics Uses |
| :- | :--- | :--- | :--- | :--- |
| **C1** | **Angle-Head Clip Light** | *Starter* | `Battery: 15 min` \| `Mass: 0.5kg` \| `Click: 35 dB` | Clips to your belt for hands-free climbing, or **duct-tapes to a rolling chair / ceiling rope** to create a moving decoy spotlight while you work in the dark. |
| **C2** | **High-Lumen Halogen Floodlight** | $400 | `Mass: 6.0kg` \| `Beam: 10,000 lm` \| `Battery: 8 min` | Heavy portable work light. **Shining its high beam directly into *The Counterweight*'s ceiling hole or at *The Seep* burns/blinds them**, forcing them to drop whatever furniture or player they grabbed! |
| **C3** | **UV Blacklight Inspector** | $300 | `Wavelength: 365nm` \| `Battery: 25 min` | Essential appraiser tool: reveals invisible **structural fractures** on fragile loot, highlights **high-stress rope fibers** about to snap, and exposes **glowing biological veins on sleeping Mimic furniture!** |
| **C4** | **Hand-Crank Dynamo Lantern** | $180 | `Infinite Battery` \| `360° Glow` \| `Crank: 60 dB` | Hang from a rope mid-room or tie to your loot cart for 360-degree lighting. Never needs batteries, but cranking the handle generates mechanical noise. |
| **C5** | **Strobe & Siren Decoy Flare** | $150 | `Duration: 30s` \| `115 dB Siren + Blinding Strobe` | Pull the cap and throw (or tape onto *The Ram*'s back!). Simultaneously blinds light-sensitive monsters and deafens *The Tuning Fork*, masking the noise of your extraction haul. |
| **C6** | **Laser Trip-Beam Pointer** | $140 | `Range: 40m` \| `Audio Chime on Break` | Tape to a doorframe or shaft wall. Projects a thin red laser beam; if an invisible/crawling entity or shifting Mimic breaks the beam, your belt receiver emits a quiet tactile vibration. |

---

### Category D: Tapes, Adhesives & Protective Packaging
| # | Item Name | Cost | Physical Parameters | Gameplay & Systemic Physics Uses |
| :- | :--- | :--- | :--- | :--- |
| **D1** | **Standard Grey Duct Tape** | $60 | `20 Strips/Roll` \| `Tear Force: 1,200N per strip` | Welds any two rigidbodies into a compound structure; seals ceiling vents; locks door seams. Multiple strips stack linearly (`3 strips = 3,600N`). |
| **D2** | **Kevlar Strapping Tape** | $280 | `15 Strips/Roll` \| `Tear Force: 4,500N per strip` | Industrial fiberglass-reinforced tape. Strong enough to **tape a sleeping Mimic's jaws/limbs shut** (requires 4 strips) so you can haul the live monster up the tube and sell it! |
| **D3** | **Acoustic Foam Tape** | $200 | `12 Strips/Roll` \| `Collision Sound: -90%` | Wrap around furniture legs, rattling door handles, grappling hook claws, or your own boots to move heavy objects right past *The Tuning Fork* in dead silence. |
| **D4** | **Rubber Electrical Tape** | $150 | `15 Strips/Roll` \| `100% Voltage Insulation` | Wrap around steel cables, metal door handles, or crowbars so you can safely grab electrified traps without getting shocked. |
| **D5** | **Heavy Bubble-Wrap Roll** | $180 | `Covers 4 Large Props` \| `Impact Damage: -85%` | Wrap around fragile, high-dollar furniture (Crystal Chandeliers, Glass Tables, Marble Busts) so banging against narrow shaft walls while hoisting doesn't chip off their $ value! |
| **D6** | **Industrial Flypaper Strip** | $130 | `4m Sticky Roll` \| `Shear Drag: 900N` | Unroll across a floor or inside a vertical vent tube. Slows *The Seep* to a crawl and forces charging enemies' feet to stick to the floor tiles. |

---

### Category E: Moving-Company Hardware, Leverage & Surface Tools
| # | Item Name | Cost | Physical Parameters | Gameplay & Systemic Physics Uses |
| :- | :--- | :--- | :--- | :--- |
| **E1** | **Swivel Caster Wheels (4-Pack)** | $140 | `Rolling Friction: 0.015` \| `Max Load: 300kg` | **Duct-tape onto the bottom of any heavy sofa, wardrobe, or safe** to turn an immovable `150 kg` prop into a smooth-rolling cart—or a high-speed downhill battering ram! |
| **E2** | **Steel Crowbar / Lever Pipe** | $120 | `Length: 1.2m` \| `Torque Multiplier: 5x` | Pry open jammed elevator doors, wedge under `300 kg` fallen slabs using a brick fulcrum, hook over taut steel cables to **zip-line across atriums**, or crossbar double doors. |
| **E3** | **Inflatable CO2 Lift Bag** | $380 | `Upward Lift: +95 kg (Air/Water)` \| `Single Use` | Strap or tape to a heavy piece of furniture at the bottom of a vertical shaft or flooded pool and pull the lanyard—inflates a salvage balloon that floats `95 kg` straight to the ceiling! |
| **E4** | **Industrial Slip-Detergent Jug** | $110 | `4 Uses` \| `Sets Floor Friction to 0.005` | Pour in front of open shafts, stairwells, or doorways. Charging monsters lose all traction and slide uncontrollably; also lets you slide heavy non-wheeled crates across a room with one finger. |
| **E5** | **Spring-Loaded Box Cutter** | $80 | `Instant Sever` \| `Belt Holstered` | Slice taut ropes or tape welds in `0.1s` to drop suspended counterweight traps on enemies—or emergency-cut a hoist rope if a monster grabs your furniture mid-shaft. |
| **E6** | **Foldable Stair-Climbing Dolly** | $320 | `Tri-Wheel Axle` \| `Halves Perceived Mass` | Strap any tall furniture (Grandfather Clocks, Arcade Cabinets, Vending Machines) onto the dolly frame to roll them smoothly up stairs and angled chutes with one hand. |

---

### Category F: Acoustic, Diagnostic & Remote Gadgets
| # | Item Name | Cost | Physical Parameters | Gameplay & Systemic Physics Uses |
| :- | :--- | :--- | :--- | :--- |
| **F1** | **Wind-Up Kitchen Timer** | $90 | `0–60s Dial` \| `Ring: 95 dB for 10s` | Twist the dial, tape it to a rolling chair or tie it to a pendulum rope, and let it ring across the sector to bait *The Tuning Fork* and *The Ram* into a trap (or into each other!). |
| **F2** | **Digital Hanging Scale** | $130 | `Max: 500kg` \| `Displays kg & Est. $` | Hook between your rope and any prop. Displays exact mass (so you can calculate exact counterweights) and **instantly exposes disguised Mimics**, which always read `95.0 kg`! |
| **F3** | **Walkie-Talkie Pair** | $160 | `Range: Infinite` \| `Relays Mic & Physics Sound` | Communicate with teammates down in the tubes—OR **leave Walkie-Talkie B inside a closet with a ceiling trap and yell into Walkie-Talkie A** from 100 meters away to trigger an acoustic ambush! |
| **F4** | **Compressed Air-Horn** | $100 | `Output: 135 dB` \| `3 Blasts` | Point-blank blast into *The Tuning Fork* overloads its acoustic organs and ragdoll-stuns it for 6 seconds—at the cost of alerting every other entity on the floor! |
| **F5** | **Contact Stethoscope** | $170 | `Wall/Door Audio Amplification: 10x` | Press against a closed door, ceiling tile, or suspicious armchair. Lets you hear entities breathing in the next room—or hear a **Mimic's slow heartbeat** inside a piece of furniture! |
| **F6** | **RC Motorized Roller Chassis** | $420 | `Speed: 6 m/s` \| `Load: 25kg` \| `Remote Control` | A small high-torque RC flatbed. Duct-tape a flashlight, kitchen timer, or camera onto it to scout vents and lure monsters across the floor via remote control. |

---

## 4. Complete Stealable Furniture & Anomalous Loot Catalog

Every piece of furniture has a **Base Value ($)**, **Mass (kg)**, **Fragility**, and—on deeper floors—**Physical Quirks** that turn transporting it into an emergent puzzle:

| Tier | Loot Item | Depth | Base Value | Mass | Fragility | Special Transport & Physics Quirk |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T1** | **Swivel Task Chair** | `-20m+` | $90 | `8 kg` | Low | Has built-in caster wheels; great starter projectile/wedge or rolling base for taping boxes on top. |
| **T1** | **Brass Bankers Lamp** | `-20m+` | $180 | `3 kg` | High | Glass shade shatters on hard drops (`-$120`); plugs into wall sockets or runs on a taped battery to light dark shafts. |
| **T1** | **Potted Monstera Plant** | `-30m+` | $240 | `22 kg` | Medium | Ceramic pot cracks if tipped; soil spills a high-friction dirt patch that stops sliding on wet floors! |
| **T1** | **4-Drawer Steel Filing Cabinet** | `-40m+` | $350 | `65 kg` | Very Low | Drawers physically slide out if tilted forward! You must **tape the drawers shut** before hoisting, or they slide open mid-shaft, shift the center of gravity, and dump files everywhere. |
| **T2** | **Executive Mahogany Desk** | `-80m+` | $750 | `90 kg` | Low | Too wide (`1.8m`) to fit through standard single doors upright—players must physically rotate it 90 degrees onto its side in VR to squeeze through doorways! |
| **T2** | **Mid-Century Velvet Sofa** | `-100m+` | $1,400 | `115 kg` | Medium | Thick velvet cushions naturally dampen collision noise (`-50% dB`) and act as a soft crash pad at the bottom of a rappel shaft! |
| **T2** | **1982 CRT Television Cart** | `-120m+` | $1,100 | `55 kg` | High | Top-heavy rolling cart; if the glass CRT tube is smashed, it implodes with a `110 dB` pop and releases a brief electrical shock. |
| **T2** | **Antique Grandfather Clock** | `-150m+` | $2,200 | `95 kg` | High | **Ticks constantly (`40 dB`), and chimes at `105 dB` if tilted past 25 degrees!** Players must open the glass case and **duct-tape the brass pendulum** before tilting or hoisting it! |
| **T3** | **Venetian Crystal Chandelier** | `-200m+` | $3,500 | `70 kg` | **Extreme** | Starts suspended 8 meters up in atrium ceilings. You must grapple/climb up, secure a pulley line *before* cutting its chain, and wrap it in Bubble-Wrap before lowering. |
| **T3** | **Retro Polybius Arcade Cabinet** | `-240m+` | $2,900 | `135 kg` | High | Randomly boots up its 8-bit attract music (`85 dB`) when jostled unless you wrap Acoustic Foam Tape over its speaker grille. |
| **T3** | **Solid Carrara Marble Statue** | `-300m+` | $4,800 | `220 kg` | Medium | So heavy (`2,156N` downward weight) that Hemp Twine (`800N`) and Dynamic Rope (`2,200N`) snap instantly if you try a direct vertical lift—**requires Steel Cable or a 4x Compound Pulley!** |
| **T3** | **Gilded Baroque Mirror** | `-320m+` | $3,200 | `45 kg` | **Extreme** | Reflects flashlight beams! Angle it in a hallway to bounce a Halogen Floodlight beam around a corner to freeze ceiling *Counterweights* while you work. |
| **T4** | **"Helium-Glass" Anomaly Table** | `-400m+` | $6,500 | **`-50 kg`** | High | **Negative gravitational mass—it falls UPWARD toward the ceiling!** If you let go of it in an atrium, it rockets into the rafters. You must tie or tape at least `60 kg` of ballast weight to it just to keep it on the floor! |
| **T4** | **Gyroscopic Brass Armillary** | `-450m+` | $7,200 | `80 kg` | Medium | Spinning internal rings generate intense gyroscopic torque—resists turning corners and pulls sideways when hoisted on a single rope unless stabilized with a two-rope bridle. |
| **T4** | **Superconducting Server Monolith** | `-500m+` | $9,000 | `160 kg` | Low | Emits a periodic magnetic pulse every 20 seconds that pulls all nearby metal props (and your steel crowbar/grapple!) toward it unless wrapped in Rubber Electrical Tape. |
| **Special** | **Taped-Up Live Mimic** | *Any* | **$2,500** | `95.0 kg` | N/A | A sleeping Mimic immobilized with at least 4 strips of Kevlar Tape. Thrashes every 12 seconds on the hoist rope—sell it alive on the Cargo Scale for a massive bounty! |

---

## 5. Complete Entity Field Guide (5 Roaming + 3 Apex Shaft Bosses)

### 5.1 The 5 Core Roaming Entities
| Entity | Mass & Force | Physics Quirk & Threat | Item & Environment Counter-Strategies |
| :--- | :--- | :--- | :--- |
| **1. The Ram** | `Mass: 180 kg`<br>`Charge: 4,500N`<br>`Speed: 11 m/s` | High linear momentum, awful braking/turning friction. Bull-charges players and **shatters expensive furniture in its path into $0 splinters**. | • **Tripwires:** String rope at shin height (`25cm`) to flip its active ragdoll at full speed.<br>• **Slip Detergent:** Pour in front of shafts so it slides off ledges.<br>• **Free Hoist Trick:** Tie a rope from a door to your heavy loot at the bottom of a shaft—trick The Ram into charging out a window so its momentum **hoists your furniture up the shaft!** |
| **2. The Seep** | `Mass: 25 kg`<br>`Pull: 350N`<br>`Gap Size: 15cm` | Multi-jointed contortionist that slithers under door gaps, through vents, and **inside vertical rappel tubes**; chews through Hemp Twine under tension. | • **Duct Tape:** Tape vent grates and under-door gaps shut.<br>• **Ball-and-Chain:** Because it only has `350N` of pull force, snag it with a Grappling Hook or rope and tie it to any `50kg+` desk—it can't drag the weight!<br>• **Halogen Floodlight:** High beams burn it and force it back into vents. |
| **3. The Mimic** | `Mass: 95.0 kg`<br>`Bite: 1,800N` | Disguises itself as high-value furniture in the room. Wakes up and attacks when grabbed with bare hands or loaded onto the elevator. | • **UV Blacklight:** Exposes glowing vascular veins under its wood/fabric surface.<br>• **Hanging Scale / Poke Test:** Always weighs `95.0 kg` even if shaped like a tiny lamp; feels suspiciously heavy when nudged with a crowbar.<br>• **Kevlar Tape Capture:** Wrap 4+ strips of Kevlar Tape around it *while it sleeps* to weld its jaws shut and **sell it on the Cargo Scale for $2,500!** |
| **4. The Counterweight** | `Ceiling Fixed`<br>`Lift: 1,600N`<br>`Cable: 25m` | Hides inside missing ceiling tiles and at the top of vertical rappel shafts. Drops sticky biological cables to hoist players up—or **steal furniture right off your hoist line!** | • **Halogen Floodlight:** Shining light into its ceiling hole makes it release its catch immediately.<br>• **Overload Rip:** Grab its dangling cable and clip it to a `200kg+` marble statue or radiator—when it reels in, the excess weight rips the monster out of the ceiling!<br>• **Free Elevator Exploit:** Let it grab your `120kg` sofa at the bottom of a shaft, let it pull the sofa to the top floor for you, then blind it with a flashlight so it drops the sofa at the top! |
| **5. The Tuning Fork** | `Mass: 110 kg`<br>`Speed: 12 m/s`<br>`Blind: 100%` | Completely blind metallic entity; sprints to the exact 3D coordinate of any `OnCollisionEnter` impulse, rope snap, Grandfather Clock chime, or player mic sound. | • **Acoustic Tape + Caster Wheels:** Silent-roll heavy furniture right past it.<br>• **Walkie-Talkie / Kitchen Timer:** Trigger loud distractions two rooms away.<br>• **Air-Horn Overload:** Point-blank blast (`135 dB`) overloads its resonance rods and ragdoll-stuns it for 6 seconds. |

### 5.2 The 3 Apex Shaft Bosses (Deep Sector Encounters)
1. **Apex 1 (`-200m` Brutalist Atrium): *The Pendulum***
   * A colossal concrete-and-brass metronome swinging across the central 6-story hoist shaft, threatening to smash any furniture you haul up the center line.
   * **Counterplay:** Harpoon its swinging mass at the apex of its arc (`v = 0`) and anchor the steel cable to a cracked load-bearing column so its downward swing rips the ceiling structure onto itself.
2. **Apex 2 (`-350m` Deep Poolrooms): *The Undertow***
   * A submerged vortex entity in the flooded shafts that drags floating rafts and sinking furniture down into the drain abyss.
   * **Counterplay:** Duct-tape 3 **Inflatable CO2 Lift Bags** to a heavy steel cabinet, let The Undertow snag the cabinet, and pull the inflation cord via a trigger-rope—rocketing the creature upward with `3,000N` of buoyancy straight into the high-voltage ceiling grid.
3. **Apex 3 (`-500m` HVAC Megastructure): *The Centrifuge***
   * A cyclonic turbine entity at the bottom of the main air shaft that alternates between violent suction and violent updraft blasts.
   * **Counterplay:** Wait for the updraft blast to **float heavy furniture up the 200-foot shaft for free** using a taped plywood sail, or jam its intake blades by launching a caster-wheeled steel locker into its core during the suction phase.

---

## 6. Vertical Map Architecture ("The Tube & Sector Matrix")

| Depth Zone | Sector Name | Rappel Tube Geometry | Environmental Physics Mechanic |
| :--- | :--- | :--- | :--- |
| **`0m`** | **The Drop Hatch & Cargo Van** | Surface breech hole (`3m x 3m`) with heavy winch frame. | Safe extraction zone, **Cargo Scale**, Shop Terminal, Equipment Racks. |
| **`-20m to -100m`** | **Sector 1: The Yellow Annex** | Straight vertical concrete maintenance tubes (`15m–30m` drops) with wall pipes. | Hinged lever doors, removable drop-ceiling tiles (for hidden overhead rope routing), rolling office chairs, water coolers. |
| **`-100m to -250m`** | **Sector 2: The Brutalist Archive** | Multi-stage offset elevator shafts (`50m–80m` drops) requiring pendulum rope swings between ledges. | Unpowered counterweight cages, rolling library ladders, high catwalks, suspended crystal chandeliers. |
| **`-250m to -400m`** | **Sector 3: The Deep Poolrooms** | Ceramic drainpipes & half-submerged vertical sumps (`80m–120m` drops). | **Archimedes Buoyancy** (wooden/plastic furniture floats up flooded shafts automatically; metal/marble sinks to the bottom), slippery wet tile (`friction = 0.02`). |
| **`-400m to -600m+`** | **Sector 4: The Tilted Hotel & Vault** | Rotating gimbal shafts & high-wind HVAC turbine chutes (`150m+` drops). | **Shifting Gravity Vectors** (corridors tilt 45 degrees every 3 minutes, causing un-anchored furniture and monsters to avalanche downhill) & **Aerodynamic Wind Drag**. |

---

## 7. Unity 6 & Meta Quest 3 Technical Architecture

### 7.1 Procedural Code-First Modules
1. **`GameBootstrapper.cs`:** Single entry-point script that constructs the Drop Hatch, generates the procedural vertical tubes and liminal rooms, spawns furniture and entities, and initializes the player rig with zero manual scene setup.
2. **`DualPhysicsPlayerRig.cs`:** Supports both **OpenXR VR** (PCVR & Quest 3 physical hands) and **Instant Desktop Mouse/Keyboard Simulator Mode** (WASD + Mouse Look + Left/Right Virtual Hands for Grab, Rope, Grapple, Tape, and Flashlight) for rapid testing.
3. **`TetherAndRappelSystem.cs`:** Handles contextual rope anchoring, `ConfigurableJoint` tension & breaking math, `PulleyNode` force multiplication (`2x`/`4x`), Waist Carabiner rappel sliding/locking, and shin-height tripwire triggers.
4. **`DuctTapeAndGadgetSystem.cs`:** Handles compound `Rigidbody` welding, strip tear-force stacking, Caster Wheel attachment, Bubble-Wrap damage reduction, Acoustic Tape muffling, Grappling Hook projectile latches, and UV/Halogen flashlight raycasts.
5. **`LootAppraisalAndQuotaManager.cs`:** Tracks furniture `BaseDollarValue`, `ConditionPercent`, collision dent penalties (`OnCollisionEnter`), Digital Scale readouts, Cargo Scale extraction payouts, and Shop Terminal purchases.
6. **`EntityEcosystemAI.cs`:** Procedural multi-joint active-ragdoll controllers for **The Ram**, **The Seep**, **The Mimic**, **The Counterweight**, and **The Tuning Fork**, including cross-entity infighting physics.
