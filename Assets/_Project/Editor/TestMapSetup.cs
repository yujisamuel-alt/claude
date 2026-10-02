using System;
using System.IO;
using System.Linq;
using Enxada.Calendar;
using Enxada.Core;
using Enxada.Farming;
using Enxada.Inventory;
using Enxada.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace Enxada.EditorTools
{
    /// <summary>
    /// Menu Enxada/Setup/Criar Mapa de Teste (Etapa 1): monta jogador, câmera Cinemachine,
    /// Pixel Perfect e um mapa com colisão. A cena TestMap é GERADA: o menu a recria do zero
    /// a cada execução, então não edite essa cena à mão (edite este script).
    /// Assets (sprites, tiles, config, prefab) só são criados se ainda não existem.
    /// </summary>
    public static class TestMapSetup
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Dev/TestMap.unity";
        private const string PlayerConfigPath = Root + "/Data/Config/PlayerConfig.asset";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player.prefab";
        private const string InputActionsPath = Root + "/Settings/Input/EnxadaControls.inputactions";
        private const string DayNightConfigPath = Root + "/Data/Config/DayNightConfig.asset";
        private const string DemoEndedPath = Root + "/Data/Events/DemoEnded.asset";

        // Cama e ponto onde o jogador acorda (dentro da clareira inicial).
        private static readonly Vector2Int BedCell = new Vector2Int(22, 19);
        private static readonly Vector2Int WakeCell = new Vector2Int(22, 18);

        // O poço fica na clareira, perto da cama.
        private static readonly Vector2Int WellCell = new Vector2Int(20, 20);

        private const int MapWidth = 48;
        private const int MapHeight = 32;
        private static readonly Vector2Int Spawn = new Vector2Int(24, 17);

        // Referência 320x180 com tiles de 16 px => meia altura = 5,625 unidades.
        private const float OrthographicSize = 180f / PlaceholderArt.PixelsPerUnit / 2f;

        [MenuItem("Enxada/Setup/Criar Mapa de Teste", priority = 1)]
        public static void CreateTestMap()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                EditorUtility.DisplayDialog("Enxada & Encrenca",
                    "Rode primeiro Enxada > Setup > Configurar Projeto (tudo).", "Ok");
                return;
            }

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                Debug.LogError($"[Setup] {InputActionsPath} não encontrado ou inválido.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var config = EnsurePlayerConfig();
            var facingSprites = PlaceholderArt.PlayerFacings();
            var playerPrefab = EnsurePlayerPrefab(config, inputActions, facingSprites);
            var gameConfig = ProjectSetup.LoadOrCreateGameConfig();

            // A cena Boot é regerada primeiro (ela cresce a cada etapa); a TestMap fica aberta no final.
            ProjectSetup.RebuildBootScene(gameConfig);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Bootstrap próprio: a TestMap roda sozinha, sem passar pela Boot.
            BootstrapBuilder.Build(gameConfig, false, out var clockDriver);

            var grid = new GameObject("Grid").AddComponent<Grid>();
            var layers = BuildTilemaps(grid);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(Spawn.x + 0.5f, Spawn.y + 0.5f, 0f);

            var database = ItemSetup.EnsureDatabase();
            BuildHighlight(config, player.GetComponent<PlayerController>(), grid, inputActions);
            BuildFarm(layers, grid, database);
            var globalLight = BuildGlobalLight();
            BuildCamera(player.transform);

            HudBuilder.Build(inputActions);
            var transition = BuildSleepSystem(player.transform, clockDriver);
            BuildWorldItems(database);
            BuildWell();
            BuildDayNightLighting(globalLight);
            AddDebugKeys(clockDriver, transition);

            EnsureFolder(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Setup] Mapa de teste criado em {ScenePath}. Aperte Play!");
        }

        // ------------------------------------------------------------------ assets

        private static PlayerConfig EnsurePlayerConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath);
            if (config != null)
                return config;

            EnsureFolder(Path.GetDirectoryName(PlayerConfigPath));
            config = ScriptableObject.CreateInstance<PlayerConfig>();
            AssetDatabase.CreateAsset(config, PlayerConfigPath);
            return config;
        }

        private static GameObject EnsurePlayerPrefab(PlayerConfig config, InputActionAsset inputActions,
            Sprite[] facingSprites)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null)
                return existing;

            var go = new GameObject("Player");
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = facingSprites[(int)FacingDirection.Down];
            spriteRenderer.sortingOrder = 10;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            // Colisão só nos pés, para o jogador poder passar "por trás" de árvores e pedras.
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 0.4f);
            box.offset = new Vector2(0f, -0.3f);

            var controller = go.AddComponent<PlayerController>();
            var so = new SerializedObject(controller);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("inputActions").objectReferenceValue = inputActions;
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            var sprites = so.FindProperty("facingSprites");
            sprites.arraySize = facingSprites.Length;
            for (var i = 0; i < facingSprites.Length; i++)
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = facingSprites[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(Path.GetDirectoryName(PlayerPrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        // ------------------------------------------------------------------ mapa

        private sealed class MapLayers
        {
            public Tilemap Ground;
            public Tilemap Soil;
            public Tilemap Crops;
            public Tilemap Obstacles;
            public TileBase Grass;
            public TileBase Dirt;
            public TileBase Water;
        }

        private static MapLayers BuildTilemaps(Grid grid)
        {
            var grass = PlaceholderArt.EnsureTile("Grass", PlaceholderArt.Grass(), Tile.ColliderType.None);
            var dirt = PlaceholderArt.EnsureTile("Dirt", PlaceholderArt.Dirt(), Tile.ColliderType.None);
            var water = PlaceholderArt.EnsureTile("Water", PlaceholderArt.Water(), Tile.ColliderType.Grid);
            var tree = PlaceholderArt.EnsureTile("Tree", PlaceholderArt.Tree(), Tile.ColliderType.Grid);

            // Ordem de desenho: chão < terra arada < obstáculos < objetos do sítio (2) < itens (3) < jogador (10).
            var ground = CreateTilemap(grid.transform, "Ground", 0);
            var soil = CreateTilemap(grid.transform, "Soil", 1);
            var crops = CreateTilemap(grid.transform, "Crops", 2);
            var obstacles = CreateTilemap(grid.transform, "Obstacles", 2);

            var obstaclesGo = obstacles.gameObject;
            var body = obstaclesGo.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = obstaclesGo.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var tilemapCollider = obstaclesGo.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            for (var y = 0; y < MapHeight; y++)
            for (var x = 0; x < MapWidth; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                ground.SetTile(cell, y == Spawn.y - 1 && x > 0 && x < MapWidth - 1 ? dirt : grass);

                var obstacle = ObstacleAt(x, y, water, tree);
                if (obstacle != null)
                    obstacles.SetTile(cell, obstacle);
            }

            return new MapLayers { Ground = ground, Soil = soil, Crops = crops, Obstacles = obstacles, Grass = grass, Dirt = dirt, Water = water };
        }

        // Só a borda do mapa e a lagoa ficam no tilemap. Mato, galhos, pedras e árvores do sítio
        // são objetos (FarmObjectField), porque reagem às ferramentas.
        private static TileBase ObstacleAt(int x, int y, TileBase water, TileBase tree)
        {
            if (x == 0 || y == 0 || x == MapWidth - 1 || y == MapHeight - 1)
                return tree;

            if (x >= 30 && x <= 38 && y >= 8 && y <= 14)
                return water;

            return null;
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return tilemap;
        }

        private static void BuildHighlight(PlayerConfig config, PlayerController player, Grid grid,
            InputActionAsset inputActions)
        {
            var go = new GameObject("TargetTileHighlight");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderArt.Highlight();
            renderer.sortingOrder = 5;

            var selector = go.AddComponent<TargetTileSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("grid").objectReferenceValue = grid;
            so.FindProperty("inputActions").objectReferenceValue = inputActions;
            so.FindProperty("highlight").objectReferenceValue = go.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var interactor = go.AddComponent<PlayerInteractor>();
            var interactorSo = new SerializedObject(interactor);
            interactorSo.FindProperty("selector").objectReferenceValue = selector;
            interactorSo.FindProperty("grid").objectReferenceValue = grid;
            interactorSo.FindProperty("player").objectReferenceValue = player;
            interactorSo.FindProperty("inputActions").objectReferenceValue = inputActions;
            interactorSo.ApplyModifiedPropertiesWithoutUndo();

            var swingGo = new GameObject("ToolSwing");
            var swing = swingGo.AddComponent<SpriteRenderer>();
            swing.sortingOrder = 11;

            var toolUser = go.AddComponent<ItemUser>();
            var toolSo = new SerializedObject(toolUser);
            toolSo.FindProperty("config").objectReferenceValue = FarmSetup.EnsureToolConfig();
            toolSo.FindProperty("selector").objectReferenceValue = selector;
            toolSo.FindProperty("player").objectReferenceValue = player;
            toolSo.FindProperty("grid").objectReferenceValue = grid;
            toolSo.FindProperty("inputActions").objectReferenceValue = inputActions;
            toolSo.FindProperty("swing").objectReferenceValue = swing;
            toolSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ fazenda

        private static void BuildFarm(MapLayers layers, Grid grid, ItemDatabase database)
        {
            var farmGo = new GameObject("Farm");
            var farm = farmGo.AddComponent<FarmTilemapController>();
            var farmSo = new SerializedObject(farm);
            farmSo.FindProperty("grid").objectReferenceValue = grid;
            farmSo.FindProperty("ground").objectReferenceValue = layers.Ground;
            farmSo.FindProperty("soil").objectReferenceValue = layers.Soil;
            farmSo.FindProperty("crops").objectReferenceValue = layers.Crops;
            farmSo.FindProperty("obstacles").objectReferenceValue = layers.Obstacles;
            farmSo.FindProperty("tilledTile").objectReferenceValue = FarmSetup.TilledTile();
            farmSo.FindProperty("wateredTile").objectReferenceValue = FarmSetup.WateredTile();
            farmSo.FindProperty("waterTile").objectReferenceValue = layers.Water;
            var tillable = farmSo.FindProperty("tillableGroundTiles");
            tillable.arraySize = 2;
            tillable.GetArrayElementAtIndex(0).objectReferenceValue = layers.Grass;
            tillable.GetArrayElementAtIndex(1).objectReferenceValue = layers.Dirt;
            farmSo.ApplyModifiedPropertiesWithoutUndo();

            var field = farmGo.AddComponent<FarmObjectField>();
            var fieldSo = new SerializedObject(field);
            fieldSo.FindProperty("config").objectReferenceValue = FarmSetup.EnsureObjectConfig(database);
            fieldSo.FindProperty("farm").objectReferenceValue = farm;
            fieldSo.FindProperty("grid").objectReferenceValue = grid;
            fieldSo.FindProperty("boundsMin").vector2IntValue = new Vector2Int(1, 1);
            fieldSo.FindProperty("boundsMax").vector2IntValue = new Vector2Int(MapWidth - 2, MapHeight - 2);
            fieldSo.FindProperty("clearingMin").vector2IntValue = new Vector2Int(Spawn.x - 5, Spawn.y - 5);
            fieldSo.FindProperty("clearingMax").vector2IntValue = new Vector2Int(Spawn.x + 5, Spawn.y + 5);
            fieldSo.ApplyModifiedPropertiesWithoutUndo();

            var debugKeys = farmGo.AddComponent<FarmDebugKeys>();
            var debugSo = new SerializedObject(debugKeys);
            debugSo.FindProperty("farm").objectReferenceValue = farm;
            debugSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildWell()
        {
            var well = new GameObject("Well");
            well.transform.position = new Vector3(WellCell.x + 0.5f, WellCell.y + 0.5f, 0f);
            var renderer = well.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderArt.Well();
            renderer.sortingOrder = 2;
            well.AddComponent<BoxCollider2D>().size = Vector2.one;
            well.AddComponent<WaterSource>();
        }

        // ------------------------------------------------------------------ dormir e luz

        // Itens espalhados na clareira para testar coleta, pilhas, qualidades e inventário cheio.
        private static void BuildWorldItems(ItemDatabase database)
        {
            var go = new GameObject("WorldItems");
            var spawner = go.AddComponent<WorldItemSpawner>();

            var drops = new[]
            {
                ("wood", 12, ItemQuality.Normal, new Vector2(27.5f, 19.5f)),
                ("stone", 8, ItemQuality.Normal, new Vector2(28.5f, 18.5f)),
                ("lettuce_seed", 20, ItemQuality.Normal, new Vector2(26.5f, 15.5f)),
                ("lettuce", 4, ItemQuality.Silver, new Vector2(21.5f, 15.5f)),
                ("lettuce", 2, ItemQuality.Gold, new Vector2(20.5f, 16.5f)),
                ("corn", 3, ItemQuality.Normal, new Vector2(25.5f, 21.5f)),
                ("axe", 1, ItemQuality.Normal, new Vector2(23.5f, 21.5f)),
                ("cassava_seed", 5, ItemQuality.Normal, new Vector2(25.5f, 14.5f)),
                ("bean_seed", 5, ItemQuality.Normal, new Vector2(26.5f, 14.5f)),
                ("corn_seed", 5, ItemQuality.Normal, new Vector2(27.5f, 14.5f)),
                ("strawberry_seed", 5, ItemQuality.Normal, new Vector2(28.5f, 14.5f))
            };

            var so = new SerializedObject(spawner);
            var array = so.FindProperty("initialDrops");
            array.arraySize = drops.Length;
            for (var i = 0; i < drops.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = ItemSetup.Find(database, drops[i].Item1);
                element.FindPropertyRelative("quantity").intValue = drops[i].Item2;
                element.FindPropertyRelative("quality").enumValueIndex = (int)drops[i].Item3;
                element.FindPropertyRelative("position").vector2Value = drops[i].Item4;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DayTransitionController BuildSleepSystem(Transform player, ClockDriver clockDriver)
        {
            var bedSprite = PlaceholderArt.Bed();
            var bed = new GameObject("Bed");
            bed.transform.position = new Vector3(BedCell.x + 0.5f, BedCell.y + 0.5f, 0f);
            var renderer = bed.AddComponent<SpriteRenderer>();
            renderer.sprite = bedSprite;
            renderer.sortingOrder = 2;
            bed.AddComponent<BoxCollider2D>().size = Vector2.one;

            var wake = new GameObject("WakePoint");
            wake.transform.position = new Vector3(WakeCell.x + 0.5f, WakeCell.y + 0.5f, 0f);

            var demoEnded = SetupUtil.EnsureAsset<VoidEventChannel>(DemoEndedPath);

            var controllerGo = new GameObject("DayTransition");
            var transition = controllerGo.AddComponent<DayTransitionController>();
            var so = new SerializedObject(transition);
            so.FindProperty("playerTransform").objectReferenceValue = player;
            so.FindProperty("wakePoint").objectReferenceValue = wake.transform;
            so.FindProperty("demoEnded").objectReferenceValue = demoEnded;
            so.FindProperty("passOutRequested").objectReferenceValue = FarmSetup.EnsurePassOutChannel();
            so.ApplyModifiedPropertiesWithoutUndo();

            var sleep = bed.AddComponent<SleepInteractable>();
            var sleepSo = new SerializedObject(sleep);
            sleepSo.FindProperty("transition").objectReferenceValue = transition;
            sleepSo.ApplyModifiedPropertiesWithoutUndo();

            return transition;
        }

        private static void BuildDayNightLighting(Component globalLight)
        {
            var config = SetupUtil.EnsureAsset<DayNightConfig>(DayNightConfigPath, c => c.SetGradient(DefaultDayGradient()));

            var go = new GameObject("DayNightLighting");
            var lighting = go.AddComponent<DayNightLighting>();
            var so = new SerializedObject(lighting);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("globalLight").objectReferenceValue = globalLight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 0 = 6h, 1 = 2h da madrugada (20 horas de jogo).
        private static Gradient DefaultDayGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color32(255, 190, 150, 255), 0.00f), // 6h  amanhecer
                    new GradientColorKey(new Color32(255, 240, 220, 255), 0.08f), // 7h30
                    new GradientColorKey(Color.white, 0.20f),                      // 10h dia
                    new GradientColorKey(new Color32(255, 246, 232, 255), 0.55f), // 17h
                    new GradientColorKey(new Color32(255, 160, 110, 255), 0.68f), // 19h30 pôr do sol
                    new GradientColorKey(new Color32(90, 100, 170, 255), 0.78f),  // 21h30 anoitecer
                    new GradientColorKey(new Color32(60, 70, 130, 255), 0.85f),   // 23h
                    new GradientColorKey(new Color32(40, 50, 100, 255), 1.00f)    // 2h madrugada
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static void AddDebugKeys(ClockDriver clockDriver, DayTransitionController transition)
        {
            var go = new GameObject("DebugKeys");
            var keys = go.AddComponent<ClockDebugKeys>();
            var so = new SerializedObject(keys);
            so.FindProperty("driver").objectReferenceValue = clockDriver;
            so.FindProperty("transition").objectReferenceValue = transition;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ luz e câmera

        private static Component BuildGlobalLight()
        {
            // Light2D e PixelPerfectCamera vivem em assemblies que mudam entre versões do URP,
            // então são buscados por nome em vez de referenciados diretamente.
            var lightType = FindComponentType("UnityEngine.Rendering.Universal.Light2D");
            if (lightType == null)
            {
                Debug.LogWarning("[Setup] Light2D não encontrado; a cena ficará sem luz global.");
                return null;
            }

            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent(lightType);
            var property = lightType.GetProperty("lightType");
            if (property != null && property.CanWrite)
                property.SetValue(light, Enum.Parse(property.PropertyType, "Global"));
            else
                Debug.LogWarning("[Setup] Não consegui definir o tipo da Light2D como Global; ajuste no Inspector.");

            Debug.Log($"[Setup] Light2D encontrada no assembly '{lightType.Assembly.GetName().Name}'.");
            return light;
        }

        private static void BuildCamera(Transform player)
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.position = new Vector3(Spawn.x + 0.5f, Spawn.y + 0.5f, -10f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<CinemachineBrain>();
            AddPixelPerfect(cameraGo);

            var boundsGo = new GameObject("CameraBounds");
            var bounds = boundsGo.AddComponent<PolygonCollider2D>();
            bounds.isTrigger = true;
            bounds.SetPath(0, new[]
            {
                new Vector2(0f, 0f), new Vector2(MapWidth, 0f),
                new Vector2(MapWidth, MapHeight), new Vector2(0f, MapHeight)
            });

            var rigGo = new GameObject("CM Player Camera");
            var vcam = rigGo.AddComponent<CinemachineCamera>();
            vcam.Follow = player;
            var lens = LensSettings.FromCamera(camera);
            lens.OrthographicSize = OrthographicSize;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            vcam.Lens = lens;

            rigGo.AddComponent<CinemachinePositionComposer>();
            var confiner = rigGo.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = bounds;

            // Extensão que mantém a Cinemachine alinhada ao Pixel Perfect Camera (opcional).
            var pixelPerfectExtension = FindComponentType("Unity.Cinemachine.CinemachinePixelPerfect");
            if (pixelPerfectExtension != null)
                rigGo.AddComponent(pixelPerfectExtension);
        }

        private static void AddPixelPerfect(GameObject cameraGo)
        {
            var type = FindComponentType("UnityEngine.Rendering.Universal.PixelPerfectCamera");
            if (type == null)
            {
                Debug.LogWarning("[Setup] PixelPerfectCamera não encontrado; o pixel perfect não foi ativado.");
                return;
            }

            var component = cameraGo.AddComponent(type);
            var so = new SerializedObject(component);
            SetInt(so, "m_AssetsPPU", PlaceholderArt.PixelsPerUnit);
            SetInt(so, "m_RefResolutionX", 320);
            SetInt(so, "m_RefResolutionY", 180);
            SetBool(so, "m_UpscaleRT", false);
            SetBool(so, "m_PixelSnapping", true);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(SerializedObject so, string field, int value)
        {
            var property = so.FindProperty(field);
            if (property != null) property.intValue = value;
            else Debug.LogWarning($"[Setup] Campo {field} não encontrado no PixelPerfectCamera.");
        }

        private static void SetBool(SerializedObject so, string field, bool value)
        {
            var property = so.FindProperty(field);
            if (property != null) property.boolValue = value;
            else Debug.LogWarning($"[Setup] Campo {field} não encontrado no PixelPerfectCamera.");
        }

        private static Type FindComponentType(string fullName) =>
            TypeCache.GetTypesDerivedFrom<Component>().FirstOrDefault(t => t.FullName == fullName);

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
