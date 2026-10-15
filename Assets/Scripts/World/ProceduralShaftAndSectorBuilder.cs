using SubLease.AI;
using SubLease.Core;
using SubLease.Gameplay;
using SubLease.Networking;
using SubLease.PhysicsSystems;
using UnityEngine;

namespace SubLease.World
{
    /// <summary>
    /// Procedurally constructs the 0m Cargo Van & Drop Hatch Hub, the Vertical Rappel Tube,
    /// Sector 1 (The Yellow Annex), stealable furniture loot, pulley anchors, and the 5 entities.
    /// </summary>
    public static class ProceduralShaftAndSectorBuilder
    {
        public static void BuildCompleteVerticalSliceWorld()
        {
            GameObject worldRoot = new GameObject("SUB_LEASE_ProceduralWorld");

            // 1. Build 0m Surface: The Cargo Van & Drop Hatch Staging Room (with 3.6m x 3.6m center hole)
            BuildDropHatchStagingRoom(worldRoot.transform);

            // 2. Build Vertical Concrete Rappel Tube (0m down to -18m)
            BuildVerticalRappelTube(worldRoot.transform, 0f, -18f);

            // 3. Build -18m Sector 1: The Yellow Annex & Showroom Hall
            BuildYellowAnnexSector(worldRoot.transform, -18f);

            // 4. Spawn Furniture Loot & Physics Contraption Props
            SpawnAllTieredFurnitureLoot(worldRoot.transform, -18f);

            // 5. Spawn the 5 Physics-Quirk Entities
            SpawnSectorEntities(worldRoot.transform, -18f);
        }

        private static void BuildDropHatchStagingRoom(Transform parent)
        {
            GameObject hub = new GameObject("0m_CargoVan_And_DropHatch");
            hub.transform.SetParent(parent, false);

            Color concreteCol = new Color(0.28f, 0.30f, 0.33f);
            Color hazardYellow = new Color(0.92f, 0.72f, 0.12f);

            // 4 Floor slabs around the central 3.6m x 3.6m Drop Hatch hole!
            CreateBox(hub.transform, "Floor_North", new Vector3(0f, -0.25f, 4.9f), new Vector3(13f, 0.5f, 6.2f), concreteCol);
            CreateBox(hub.transform, "Floor_South", new Vector3(0f, -0.25f, -4.9f), new Vector3(13f, 0.5f, 6.2f), concreteCol);
            CreateBox(hub.transform, "Floor_West", new Vector3(-4.2f, -0.25f, 0f), new Vector3(4.6f, 0.5f, 3.6f), concreteCol);
            CreateBox(hub.transform, "Floor_East", new Vector3(4.2f, -0.25f, 0f), new Vector3(4.6f, 0.5f, 3.6f), concreteCol);

            // Safety Curb Strips around the Drop Hatch
            CreateBox(hub.transform, "HatchCurb_N", new Vector3(0f, 0.06f, 1.85f), new Vector3(3.8f, 0.12f, 0.15f), hazardYellow);
            CreateBox(hub.transform, "HatchCurb_S", new Vector3(0f, 0.06f, -1.85f), new Vector3(3.8f, 0.12f, 0.15f), hazardYellow);
            CreateBox(hub.transform, "HatchCurb_W", new Vector3(-1.85f, 0.06f, 0f), new Vector3(0.15f, 0.12f, 3.8f), hazardYellow);
            CreateBox(hub.transform, "HatchCurb_E", new Vector3(1.85f, 0.06f, 0f), new Vector3(0.15f, 0.12f, 3.8f), hazardYellow);

            // Overhead Steel Winch Gantry Frame above Drop Hatch
            CreateBox(hub.transform, "WinchPillar_L", new Vector3(-1.9f, 2.2f, 0f), new Vector3(0.25f, 4.4f, 0.25f), hazardYellow);
            CreateBox(hub.transform, "WinchPillar_R", new Vector3(1.9f, 2.2f, 0f), new Vector3(0.25f, 4.4f, 0.25f), hazardYellow);
            GameObject crossBeam = CreateBox(hub.transform, "WinchCrossBeam_4xPulley", new Vector3(0f, 4.3f, 0f), new Vector3(4.2f, 0.28f, 0.28f), hazardYellow);
            TetherAnchor gantryPulley = crossBeam.AddComponent<TetherAnchor>();
            gantryPulley.AnchorLabel = "4x Overhead Shaft Pulley Winch";
            gantryPulley.IsOverheadPulley = true;
            gantryPulley.PulleyMechanicalAdvantage = 4;
            gantryPulley.IsRappelShaftTopAnchor = true;

            // Pre-rig a Primary Rappel Line hanging from the overhead Winch Beam down to -18m Sector 1!
            GameObject bottomAnchor = new GameObject("ShaftBottomRappelWeight");
            bottomAnchor.transform.SetParent(hub.transform, false);
            bottomAnchor.transform.position = new Vector3(0f, -17.5f, 0f);
            Rigidbody weightRb = bottomAnchor.AddComponent<Rigidbody>();
            weightRb.isKinematic = true;
            bottomAnchor.AddComponent<TetherAnchor>();

            GameObject mainRappelRope = new GameObject("Primary_Shaft_Rappel_Line");
            mainRappelRope.transform.SetParent(hub.transform, false);
            ActiveTetherRope tether = mainRappelRope.AddComponent<ActiveTetherRope>();
            tether.InitializeTether(crossBeam.transform, bottomAnchor.transform, RopeGrade.BraidedSteel_6000N);

            // Build Cargo Scale Extraction Platform at (3.8m, 0.1m, 3.2m)
            BuildCargoScalePlatform(hub.transform, new Vector3(3.8f, 0.08f, 3.2f));

            // Hub Lighting
            CreatePointLight(hub.transform, new Vector3(0f, 4.8f, 0f), new Color(1.0f, 0.92f, 0.78f), 2.8f, 18f);
        }

        private static void BuildCargoScalePlatform(Transform parent, Vector3 pos)
        {
            GameObject scalePad = CreateBox(parent, "CargoExtractionScalePad", pos, new Vector3(3.2f, 0.16f, 3.2f), new Color(0.15f, 0.55f, 0.25f));
            BoxCollider trigger = scalePad.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 6.0f, 0f);
            trigger.size = new Vector3(1.0f, 12.0f, 1.0f);

            CargoScaleExtractionZone zone = scalePad.AddComponent<CargoScaleExtractionZone>();

            GameObject boardObj = new GameObject("CargoScale3DBoard");
            boardObj.transform.SetParent(scalePad.transform, false);
            boardObj.transform.localPosition = new Vector3(0f, 14f, 0f);
            TextMesh tm = boardObj.AddComponent<TextMesh>();
            tm.characterSize = 0.08f;
            tm.fontSize = 42;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.4f, 1.0f, 0.55f);
            zone.ScaleDisplayBoard = tm;

            // Place a starter $180 Brass Bankers Lamp right next to the scale so players can test grabbing & selling immediately!
            SpawnLootPiece(parent, "Starter Brass Bankers Lamp", new Vector3(3.8f, 0.8f, 0.5f), new Vector3(0.35f, 0.45f, 0.35f),
                new Color(0.95f, 0.78f, 0.2f), 180, 3f, 1.5f);

            zone.RecalculateScale();
        }

        private static void BuildVerticalRappelTube(Transform parent, float topY, float bottomY)
        {
            GameObject tube = new GameObject("Vertical_Rappel_Tube_Tier1");
            tube.transform.SetParent(parent, false);

            float height = Mathf.Abs(topY - bottomY) - 3.2f; // Leave 3.2m doorway clearance at bottom of shaft!
            float midY = (topY + (bottomY + 3.2f)) * 0.5f;
            Color shaftCol = new Color(0.22f, 0.23f, 0.25f);

            CreateBox(tube.transform, "ShaftWall_N", new Vector3(0f, midY, 1.95f), new Vector3(4.0f, height, 0.3f), shaftCol);
            CreateBox(tube.transform, "ShaftWall_S", new Vector3(0f, midY, -1.95f), new Vector3(4.0f, height, 0.3f), shaftCol);
            CreateBox(tube.transform, "ShaftWall_W", new Vector3(-1.95f, midY, 0f), new Vector3(0.3f, height, 4.0f), shaftCol);
            CreateBox(tube.transform, "ShaftWall_E", new Vector3(1.95f, midY, 0f), new Vector3(0.3f, height, 4.0f), shaftCol);

            // Add wall anchor pipes every 4.5 meters down the tube for grappling & piton stops
            for (float y = topY - 4f; y > bottomY + 3.5f; y -= 4.5f)
            {
                GameObject pipe = CreateBox(tube.transform, $"ShaftPipeAnchor_{y:F0}m", new Vector3(1.65f, y, 0f), new Vector3(0.18f, 0.18f, 3.4f), new Color(0.75f, 0.45f, 0.15f));
                TetherAnchor pa = pipe.AddComponent<TetherAnchor>();
                pa.AnchorLabel = $"Shaft Pipe ({y:F0}m)";
            }
        }

        private static void BuildYellowAnnexSector(Transform parent, float floorY)
        {
            GameObject sector = new GameObject("Sector1_TheYellowAnnex_-18m");
            sector.transform.SetParent(parent, false);

            Color carpetCol = new Color(0.58f, 0.52f, 0.32f);
            Color wallpaperCol = new Color(0.78f, 0.73f, 0.42f);

            // Main Floor & Ceiling Grid (with central 3.8m x 3.8m shaft opening in ceiling)
            CreateBox(sector.transform, "Annex_CarpetFloor", new Vector3(0f, floorY - 0.25f, 6f), new Vector3(26f, 0.5f, 28f), carpetCol);

            // Outer Liminal Walls
            CreateBox(sector.transform, "Annex_Wall_South", new Vector3(0f, floorY + 2.5f, -7.8f), new Vector3(26f, 5.0f, 0.4f), wallpaperCol);
            CreateBox(sector.transform, "Annex_Wall_North", new Vector3(0f, floorY + 2.5f, 19.8f), new Vector3(26f, 5.0f, 0.4f), wallpaperCol);
            CreateBox(sector.transform, "Annex_Wall_West", new Vector3(-12.8f, floorY + 2.5f, 6f), new Vector3(0.4f, 5.0f, 28f), wallpaperCol);
            CreateBox(sector.transform, "Annex_Wall_East", new Vector3(12.8f, floorY + 2.5f, 6f), new Vector3(0.4f, 5.0f, 28f), wallpaperCol);

            // Partition Wall with Hinged Physics Door leading to the High-Value Showroom
            CreateBox(sector.transform, "Partition_Left", new Vector3(-6.5f, floorY + 2.5f, 5.5f), new Vector3(11f, 5.0f, 0.35f), wallpaperCol);
            CreateBox(sector.transform, "Partition_Right", new Vector3(6.5f, floorY + 2.5f, 5.5f), new Vector3(11f, 5.0f, 0.35f), wallpaperCol);
            BuildHingedPhysicsDoor(sector.transform, new Vector3(-1.0f, floorY + 1.2f, 5.5f));

            // Overhead Pulley Hook & Suspended Chandelier Trap above the doorway!
            GameObject ceilingHook = CreateBox(sector.transform, "CeilingPulleyHook_Doorway", new Vector3(0f, floorY + 4.6f, 4.2f), new Vector3(0.3f, 0.3f, 0.3f), Color.yellow);
            TetherAnchor hookAnchor = ceilingHook.AddComponent<TetherAnchor>();
            hookAnchor.IsOverheadPulley = true;
            hookAnchor.PulleyMechanicalAdvantage = 2;

            // 60Hz Fluorescent Light & Procedural Hum
            GameObject lightFixture = CreatePointLight(sector.transform, new Vector3(0f, floorY + 4.4f, 2f), new Color(1.0f, 0.96f, 0.80f), 2.5f, 22f);
            ProceduralAudioSynth synth = lightFixture.AddComponent<ProceduralAudioSynth>();
            synth.Mode = ProceduralAudioSynth.SynthMode.FluorescentHum60Hz;

            CreatePointLight(sector.transform, new Vector3(0f, floorY + 4.4f, 12f), new Color(1.0f, 0.95f, 0.78f), 2.5f, 22f);
        }

        private static void BuildHingedPhysicsDoor(Transform parent, Vector3 hingeWorldPos)
        {
            GameObject doorFrameAnchor = CreateBox(parent, "DoorHingeFrame", hingeWorldPos, new Vector3(0.15f, 2.4f, 0.35f), new Color(0.35f, 0.25f, 0.15f));
            Rigidbody frameRb = doorFrameAnchor.AddComponent<Rigidbody>();
            frameRb.isKinematic = true;

            GameObject doorPanel = CreateBox(parent, "HingedFireDoor_WithLever", hingeWorldPos + new Vector3(0.95f, 0f, 0f), new Vector3(1.8f, 2.3f, 0.12f), new Color(0.48f, 0.32f, 0.18f));
            Rigidbody doorRb = doorPanel.AddComponent<Rigidbody>();
            doorRb.mass = 35f;
            doorPanel.AddComponent<NetworkedPhysicsObject>().TrueSignedMassKg = 35f;

            HingeJoint hj = doorPanel.AddComponent<HingeJoint>();
            hj.connectedBody = frameRb;
            hj.axis = Vector3.up;
            hj.anchor = new Vector3(-0.5f, 0f, 0f);
            hj.useLimits = true;
            hj.limits = new JointLimits { min = -110f, max = 110f };

            // Physical Lever Doorknob (Valid Rope & Chair-Wedge Anchor!)
            GameObject knob = CreateBox(doorPanel.transform, "DoorLeverHandle", new Vector3(0.38f, -0.05f, 0f), new Vector3(0.15f, 0.08f, 3.2f), new Color(0.85f, 0.75f, 0.25f));
            knob.transform.localPosition = new Vector3(0.38f, -0.05f, 0f);
            TetherAnchor knobAnchor = knob.AddComponent<TetherAnchor>();
            knobAnchor.AnchorLabel = "Door Lever Handle (Tie or Wedge Chair Under!)";
        }

        private static void SpawnAllTieredFurnitureLoot(Transform parent, float floorY)
        {
            // 1. Swivel Task Chair ($90, 8kg, Wheels pre-attached!)
            GameObject chair = SpawnLootPiece(parent, "Swivel Task Chair", new Vector3(-2.5f, floorY + 0.6f, 2.5f),
                new Vector3(0.65f, 0.95f, 0.65f), new Color(0.2f, 0.45f, 0.75f), 90, 8f, 0.5f);
            chair.GetComponent<NetworkedPhysicsObject>().ApplyCasterWheels();

            // 2. 4-Drawer Steel Filing Cabinet ($350, 65kg)
            SpawnLootPiece(parent, "4-Drawer Steel Filing Cabinet", new Vector3(3.2f, floorY + 0.9f, 2.5f),
                new Vector3(0.75f, 1.45f, 0.75f), new Color(0.55f, 0.58f, 0.62f), 350, 65f, 0.4f);

            // 3. Executive Mahogany Desk ($750, 90kg)
            SpawnLootPiece(parent, "Executive Mahogany Desk", new Vector3(-4.2f, floorY + 0.7f, 9.5f),
                new Vector3(1.85f, 0.85f, 0.95f), new Color(0.45f, 0.22f, 0.12f), 750, 90f, 0.6f);

            // 4. Mid-Century Velvet Sofa ($1,400, 115kg, Silent Cushion Shield)
            GameObject sofa = SpawnLootPiece(parent, "Mid-Century Velvet Sofa", new Vector3(0f, floorY + 0.7f, 11.0f),
                new Vector3(2.1f, 0.9f, 0.95f), new Color(0.18f, 0.55f, 0.38f), 1400, 115f, 0.8f);
            sofa.GetComponent<NetworkedPhysicsObject>().ApplyAcousticFoamTape();

            // 5. Antique Grandfather Clock ($2,200, 95kg — Chimes at 105 dB if tilted past 25°!)
            GameObject clock = SpawnLootPiece(parent, "Antique Grandfather Clock", new Vector3(4.2f, floorY + 1.2f, 10.5f),
                new Vector3(0.65f, 2.1f, 0.55f), new Color(0.38f, 0.20f, 0.08f), 2200, 95f, 1.8f);
            clock.GetComponent<LootFurnitureItem>().IsGrandfatherClock = true;

            // 6. Solid Carrara Marble Statue ($4,800, 220kg — Snaps cheap ropes without 4x Pulley or Steel Cable!)
            SpawnLootPiece(parent, "Carrara Marble Statue", new Vector3(0f, floorY + 1.1f, 15.0f),
                new Vector3(0.85f, 1.85f, 0.85f), new Color(0.92f, 0.92f, 0.95f), 4800, 220f, 1.1f);

            // 7. "Helium-Glass" Anomaly Table ($6,500, -50kg NEGATIVE MASS — Tied down by a rope!)
            GameObject heliumTable = SpawnLootPiece(parent, "Helium-Glass Anomaly Table (-50kg)", new Vector3(-5.5f, floorY + 1.2f, 14.5f),
                new Vector3(1.2f, 0.6f, 1.2f), new Color(0.3f, 0.95f, 0.95f), 6500, -50f, 1.6f);
            GameObject floorBolt = CreateBox(parent, "AnomalyBallastAnchor", new Vector3(-5.5f, floorY + 0.1f, 14.5f), new Vector3(0.4f, 0.2f, 0.4f), Color.gray);
            floorBolt.AddComponent<Rigidbody>().isKinematic = true;
            floorBolt.AddComponent<TetherAnchor>();
            GameObject holdRope = new GameObject("AnomalyBallastRope");
            holdRope.transform.SetParent(parent, false);
            holdRope.AddComponent<ActiveTetherRope>().InitializeTether(heliumTable.transform, floorBolt.transform, RopeGrade.HempTwine_800N);

            // 8. DISGUISED SLEEPING MIMIC ($2,500 if wrapped in 4+ strips of Kevlar Tape; glows under UV [F]!)
            GameObject mimicChair = SpawnLootPiece(parent, "Vintage Leather Armchair", new Vector3(3.5f, floorY + 0.65f, 14.5f),
                new Vector3(0.85f, 0.95f, 0.85f), new Color(0.55f, 0.28f, 0.14f), 950, 95f, 1.0f);
            LootFurnitureItem mimicLoot = mimicChair.GetComponent<LootFurnitureItem>();
            mimicLoot.IsDisguisedMimic = true;
            mimicLoot.UpdatePriceTagText();
            EntityPhysicsController mimicAi = mimicChair.AddComponent<EntityPhysicsController>();
            mimicAi.QuirkType = EntityQuirkType.TheMimic_DisguisedProp95Kg;
        }

        private static void SpawnSectorEntities(Transform parent, float floorY)
        {
            // 1. The Ram (180kg Momentum Charger in the East Corridor)
            GameObject ram = CreateBox(parent, "Entity_TheRam_180kg", new Vector3(8.5f, floorY + 0.9f, 12.5f), new Vector3(1.1f, 1.4f, 1.6f), new Color(0.85f, 0.2f, 0.15f));
            Rigidbody ramRb = ram.AddComponent<Rigidbody>();
            ramRb.mass = 180f;
            ramRb.freezeRotation = true;
            ram.AddComponent<TetherAnchor>().AnchorLabel = "The Ram Carapace";
            ram.AddComponent<EntityPhysicsController>().QuirkType = EntityQuirkType.TheRam_MomentumCharger;

            // 2. The Seep (25kg Gap & Rope-Chewing Crawler near West Vent)
            GameObject seep = CreateBox(parent, "Entity_TheSeep_25kg", new Vector3(-8.5f, floorY + 0.35f, 8.0f), new Vector3(0.45f, 0.35f, 1.2f), new Color(0.25f, 0.35f, 0.22f));
            Rigidbody seepRb = seep.AddComponent<Rigidbody>();
            seepRb.mass = 25f;
            seep.AddComponent<TetherAnchor>().AnchorLabel = "The Seep Limb (Lasso to 50kg+ Desk!)";
            seep.AddComponent<EntityPhysicsController>().QuirkType = EntityQuirkType.TheSeep_GapAndTubeCrawler;

            // 3. The Counterweight (Ceiling Winch Predator lurking above the Showroom hole!)
            GameObject cw = CreateBox(parent, "Entity_TheCounterweight_CeilingPredator", new Vector3(0f, floorY + 4.7f, 11.0f), new Vector3(0.9f, 0.4f, 0.9f), new Color(0.55f, 0.12f, 0.28f));
            cw.AddComponent<Rigidbody>().isKinematic = true;
            cw.AddComponent<EntityPhysicsController>().QuirkType = EntityQuirkType.TheCounterweight_CeilingWinchPredator;

            // 4. The Tuning Fork (110kg Blind Acoustic Impulse Hunter in North Alcove)
            GameObject fork = CreateBox(parent, "Entity_TheTuningFork_BlindAcoustic", new Vector3(-8.0f, floorY + 1.2f, 16.5f), new Vector3(0.6f, 2.2f, 0.6f), new Color(0.75f, 0.82f, 0.88f));
            Rigidbody forkRb = fork.AddComponent<Rigidbody>();
            forkRb.mass = 110f;
            forkRb.freezeRotation = true;
            fork.AddComponent<TetherAnchor>().AnchorLabel = "The Tuning Fork Resonance Rod";
            fork.AddComponent<EntityPhysicsController>().QuirkType = EntityQuirkType.TheTuningFork_BlindAcousticHunter;
        }

        private static GameObject SpawnLootPiece(Transform parent, string name, Vector3 pos, Vector3 scale, Color col, int dollarValue, float signedMassKg, float fragility)
        {
            GameObject obj = CreateBox(parent, $"Loot_{name}", pos, scale, col);
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            NetworkedPhysicsObject net = obj.AddComponent<NetworkedPhysicsObject>();
            net.TrueSignedMassKg = signedMassKg;
            if (signedMassKg > 0f)
            {
                rb.mass = signedMassKg;
            }
            else
            {
                net.IsNegativeMassAnomaly = true;
                rb.mass = Mathf.Abs(signedMassKg);
                rb.useGravity = false;
            }

            TetherAnchor anchor = obj.AddComponent<TetherAnchor>();
            anchor.AnchorLabel = name;

            LootFurnitureItem loot = obj.AddComponent<LootFurnitureItem>();
            loot.ItemDisplayName = name;
            loot.BaseDollarValue = dollarValue;
            loot.CurrentDollarValue = dollarValue;
            loot.FragilityMultiplier = fragility;
            loot.UpdatePriceTagText();

            return obj;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = pos;
            box.transform.localScale = scale;

            Renderer r = box.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default"));
                mat.color = color;
                r.material = mat;
            }
            return box;
        }

        private static GameObject CreatePointLight(Transform parent, Vector3 pos, Color col, float intensity, float range)
        {
            GameObject lObj = new GameObject("SectorLight");
            lObj.transform.SetParent(parent, false);
            lObj.transform.position = pos;
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.intensity = intensity;
            l.range = range;
            return lObj;
        }
    }
}
