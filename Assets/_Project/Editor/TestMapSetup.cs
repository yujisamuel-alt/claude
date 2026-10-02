using System;
using System.IO;
using System.Linq;
using Enxada.Calendar;
using Enxada.Core;
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
            BuildTilemaps(grid);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(Spawn.x + 0.5f, Spawn.y + 0.5f, 0f);

            BuildHighlight(config, player.GetComponent<PlayerController>(), grid, inputActions);
            var globalLight = BuildGlobalLight();
            BuildCamera(player.transform);

            HudBuilder.Build(inputActions);
            var transition = BuildSleepSystem(player.transform, clockDriver);
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

        private static void BuildTilemaps(Grid grid)
        {
            var grass = PlaceholderArt.EnsureTile("Grass", PlaceholderArt.Grass(), Tile.ColliderType.None);
            var dirt = PlaceholderArt.EnsureTile("Dirt", PlaceholderArt.Dirt(), Tile.ColliderType.None);
            var water = PlaceholderArt.EnsureTile("Water", PlaceholderArt.Water(), Tile.ColliderType.Grid);
            var tree = PlaceholderArt.EnsureTile("Tree", PlaceholderArt.Tree(), Tile.ColliderType.Grid);
            var rock = PlaceholderArt.EnsureTile("Rock", PlaceholderArt.Rock(), Tile.ColliderType.Grid);

            var ground = CreateTilemap(grid.transform, "Ground", 0);
            var obstacles = CreateTilemap(grid.transform, "Obstacles", 1);

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

                var obstacle = ObstacleAt(x, y, water, tree, rock);
                if (obstacle != null)
                    obstacles.SetTile(cell, obstacle);
            }
        }

        private static TileBase ObstacleAt(int x, int y, TileBase water, TileBase tree, TileBase rock)
        {
            if (x == 0 || y == 0 || x == MapWidth - 1 || y == MapHeight - 1)
                return tree; // borda do mapa

            if (x >= 30 && x <= 38 && y >= 8 && y <= 14)
                return water; // lagoa

            var nearSpawn = Mathf.Abs(x - Spawn.x) <= 4 && Mathf.Abs(y - Spawn.y) <= 4;
            if (nearSpawn)
                return null; // clareira para começar

            var h = PlaceholderArt.Hash(x, y, 7);
            if (h % 19 != 0)
                return null;
            return h % 2 == 0 ? tree : rock;
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
        }

        // ------------------------------------------------------------------ dormir e luz

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
