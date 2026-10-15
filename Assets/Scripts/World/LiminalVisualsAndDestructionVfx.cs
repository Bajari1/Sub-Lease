using System.Collections.Generic;
using SubLease.Gameplay;
using SubLease.Player;
using UnityEngine;

namespace SubLease.World
{
    /// <summary>
    /// Priority P7: Procedural Liminal PBR Surface & Texture Generator, Flickering Fluorescent
    /// Ballasts, Depth-Graded Volumetric Shaft Fog, and Physical Shatter/Spark/Markdown VFX.
    /// </summary>
    public class LiminalVisualsAndDestructionVfx : MonoBehaviour
    {
        public static LiminalVisualsAndDestructionVfx Instance { get; private set; }

        [Header("Generated Procedural Liminal Textures")]
        public Texture2D YellowChevronWallpaperTex;
        public Texture2D AcousticCeilingTileTex;
        public Texture2D BrutalistConcreteTex;
        public Texture2D PoolroomsCeramicTileTex;
        public int TotalSurfacesTextured = 0;

        [Header("Flickering Fluorescent Fixtures & Fog")]
        public List<Light> FluorescentBallastLights = new List<Light>();
        public Color CurrentDepthFogColor;
        public int TotalFractureShardsSpawned = 0;
        public int TotalMarkdownPopupsSpawned = 0;

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
            GenerateAllProceduralLiminalTextures();
            ApplyLiminalTexturesToWorldGeometry();
            SpawnFluorescentCeilingFixtures();
            ConfigureDepthGradedFog(0f);
        }

        private void Update()
        {
            float playerY = DualPhysicsPlayerRig.Instance != null
                ? DualPhysicsPlayerRig.Instance.transform.position.y
                : 0f;
            ConfigureDepthGradedFog(playerY);
            UpdateFluorescentBallastFlicker();
        }

        public void GenerateAllProceduralLiminalTextures()
        {
            YellowChevronWallpaperTex = GenerateChevronWallpaperTexture(128, 128);
            AcousticCeilingTileTex = GenerateAcousticTileTexture(128, 128);
            BrutalistConcreteTex = GenerateBrutalistConcreteTexture(128, 128);
            PoolroomsCeramicTileTex = GeneratePoolroomsTileTexture(128, 128);
        }

        private Texture2D GenerateChevronWallpaperTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color baseYellow = new Color(0.83f, 0.79f, 0.52f);
            Color stripeDark = new Color(0.71f, 0.66f, 0.39f);
            Color baseboardStain = new Color(0.48f, 0.43f, 0.24f);

            for (int y = 0; y < height; y++)
            {
                float dampFactor = y < 20 ? (20 - y) / 20f : 0f;
                for (int x = 0; x < width; x++)
                {
                    int stripe = (x % 32);
                    int chevronOffset = Mathf.Abs((x % 16) - 8);
                    bool isPattern = ((y + chevronOffset * 2) % 24) < 6 || stripe < 3;
                    Color c = isPattern ? stripeDark : baseYellow;
                    c = Color.Lerp(c, baseboardStain, dampFactor * 0.65f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateAcousticTileTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color tileWhite = new Color(0.86f, 0.85f, 0.80f);
            Color gridBar = new Color(0.45f, 0.45f, 0.42f);
            Color pinhole = new Color(0.62f, 0.60f, 0.55f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isGrid = (x % 32 == 0) || (y % 32 == 0);
                    bool isDot = ((x * 37 + y * 13) % 19) == 0;
                    tex.SetPixel(x, y, isGrid ? gridBar : (isDot ? pinhole : tileWhite));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateBrutalistConcreteTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color baseConcrete = new Color(0.36f, 0.38f, 0.40f);
            Color seamDark = new Color(0.22f, 0.24f, 0.26f);

            for (int y = 0; y < height; y++)
            {
                bool isSeam = (y % 24) == 0;
                for (int x = 0; x < width; x++)
                {
                    float grain = (((x * 17 + y * 31) % 13) / 13f - 0.5f) * 0.06f;
                    Color c = isSeam ? seamDark : new Color(baseConcrete.r + grain, baseConcrete.g + grain, baseConcrete.b + grain);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GeneratePoolroomsTileTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color tileAqua = new Color(0.68f, 0.92f, 0.94f);
            Color groutLine = new Color(0.20f, 0.52f, 0.58f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isGrout = (x % 16 == 0) || (y % 16 == 0);
                    tex.SetPixel(x, y, isGrout ? groutLine : tileAqua);
                }
            }
            tex.Apply();
            return tex;
        }

        public void ApplyLiminalTexturesToWorldGeometry()
        {
            TotalSurfacesTextured = 0;
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer r in allRenderers)
            {
                if (r == null || r.sharedMaterial == null) continue;
                string objName = r.gameObject.name;

                if (objName.Contains("Wall") || objName.Contains("Divider") || objName.Contains("Header"))
                {
                    r.material.mainTexture = YellowChevronWallpaperTex;
                    r.material.mainTextureScale = new Vector2(4f, 2f);
                    TotalSurfacesTextured++;
                }
                else if (objName.Contains("ShaftWall") || objName.Contains("Archive") || objName.Contains("Column"))
                {
                    r.material.mainTexture = BrutalistConcreteTex;
                    r.material.mainTextureScale = new Vector2(3f, 6f);
                    TotalSurfacesTextured++;
                }
                else if (objName.Contains("Poolrooms"))
                {
                    r.material.mainTexture = PoolroomsCeramicTileTex;
                    r.material.mainTextureScale = new Vector2(6f, 6f);
                    TotalSurfacesTextured++;
                }
                else if (objName.Contains("Floor"))
                {
                    r.material.mainTexture = AcousticCeilingTileTex;
                    r.material.mainTextureScale = new Vector2(5f, 5f);
                    TotalSurfacesTextured++;
                }
            }
        }

        private void SpawnFluorescentCeilingFixtures()
        {
            Vector3[] positions =
            {
                new Vector3(0f, 3.6f, -2.5f),
                new Vector3(0f, -14.2f, 5.0f),
                new Vector3(0f, -34.2f, 6.0f),
                new Vector3(0f, -52.2f, -6.0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fixture.name = $"FluorescentBallastFixture_{i}";
                fixture.transform.SetParent(transform, false);
                fixture.transform.position = positions[i];
                fixture.transform.localScale = new Vector3(1.6f, 0.06f, 0.35f);
                Destroy(fixture.GetComponent<Collider>());

                Renderer r = fixture.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(1f, 0.98f, 0.82f);
                }

                Light l = fixture.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 16f;
                l.intensity = 1.35f;
                l.color = new Color(1f, 0.95f, 0.78f);
                FluorescentBallastLights.Add(l);
            }
        }

        private void UpdateFluorescentBallastFlicker()
        {
            for (int i = 0; i < FluorescentBallastLights.Count; i++)
            {
                Light l = FluorescentBallastLights[i];
                if (l == null) continue;
                float buzz = Mathf.Sin(Time.time * 60f + i * 2.1f);
                l.intensity = buzz > 0.92f ? 0.45f : 1.35f;
            }
        }

        public void ConfigureDepthGradedFog(float depthY)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.014f;

            if (depthY > -22f)
            {
                CurrentDepthFogColor = new Color(0.83f, 0.81f, 0.60f); // Sector 1 Damp Yellow Haze
            }
            else if (depthY > -45f)
            {
                CurrentDepthFogColor = new Color(0.34f, 0.36f, 0.39f); // Sector 2 Brutalist Mist
            }
            else if (depthY > -62f)
            {
                CurrentDepthFogColor = new Color(0.07f, 0.28f, 0.35f); // Sector 3 Chlorinated Aqua
            }
            else
            {
                CurrentDepthFogColor = new Color(0.22f, 0.09f, 0.07f); // Sector 4 Industrial Rust Haze
            }

            RenderSettings.fogColor = CurrentDepthFogColor;
        }

        /// <summary>
        /// Spawns physical glass/wood/spark fracture shards and a floating 3D "-$XXX SCRATCHED!"
        /// appraisal markdown popup whenever fragile furniture takes impact damage.
        /// </summary>
        public void TriggerImpactFractureVfx(LootFurnitureItem item, Vector3 hitPoint, int dollarLoss)
        {
            if (item == null) return;

            // Spawn 4 physical fracture shards
            for (int i = 0; i < 4; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"FractureShard_{item.ItemDisplayName}_{i}";
                shard.transform.position = hitPoint + Random.insideUnitSphere * 0.25f;
                shard.transform.localScale = new Vector3(0.08f, 0.03f, 0.11f);
                Destroy(shard.GetComponent<Collider>());

                Renderer r = shard.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = item.FragilityMultiplier > 1.5f
                        ? new Color(0.75f, 0.95f, 1.0f) // Crystal/Mirror Glass Shard
                        : new Color(0.55f, 0.35f, 0.18f); // Mahogany Wood Splinter
                }

                Destroy(shard, 2.5f);
                TotalFractureShardsSpawned++;
            }

            // Spawn floating 3D "-$XXX DENTED!" popup
            GameObject popupObj = new GameObject($"MarkdownPopup_{dollarLoss}");
            popupObj.transform.position = hitPoint + Vector3.up * 0.9f;
            TextMesh tm = popupObj.AddComponent<TextMesh>();
            tm.text = $"-${Mathf.Max(25, dollarLoss)} SCRATCHED! ({item.ConditionPercent:F0}%)";
            tm.characterSize = 0.065f;
            tm.fontSize = 38;
            tm.color = new Color(1.0f, 0.25f, 0.2f);
            tm.anchor = TextAnchor.MiddleCenter;
            Destroy(popupObj, 2.5f);
            TotalMarkdownPopupsSpawned++;
        }
    }
}
