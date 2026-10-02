using System.IO;
using System.Linq;
using Enxada.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Enxada.EditorTools
{
    /// <summary>
    /// Menu Enxada/Setup: monta configurações, assets e cenas por código,
    /// para nunca precisarmos editar .unity/.prefab à mão.
    /// Todos os passos são idempotentes: rodar de novo não apaga nada que já existe.
    /// </summary>
    public static class ProjectSetup
    {
        private const string Root = "Assets/_Project";
        private const string ConfigPath = Root + "/Data/Config/GameConfig.asset";
        private const string RenderingFolder = Root + "/Settings/Rendering";
        private const string PipelinePath = RenderingFolder + "/URP-2D.asset";
        private const string RendererPath = RenderingFolder + "/URP-2D-Renderer.asset";
        private const string ScenesFolder = Root + "/Scenes";

        public const string BootScenePath = ScenesFolder + "/Boot.unity";
        public const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";

        // Resolução de referência 320x180 com tiles de 16 px => 11,25 unidades de altura.
        private const float ReferenceOrthographicSize = 180f / 16f / 2f;

        [MenuItem("Enxada/Setup/Configurar Projeto (tudo)", priority = 0)]
        public static void SetupAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ConfigureEditorSettings();
            var config = EnsureGameConfig();
            var pipelineOk = ConfigureRenderPipeline();
            CreateScenes(config);
            AssetDatabase.SaveAssets();

            var message = pipelineOk
                ? "Projeto configurado! Cenas Boot e MainMenu criadas.\nAgora rode os testes em Window > General > Test Runner."
                : "Projeto configurado, mas o Render Pipeline 2D falhou. Veja o Console.";
            EditorUtility.DisplayDialog("Enxada & Encrenca", message, "Ok");
        }

        [MenuItem("Enxada/Setup/Passos/1. Configurações do Editor", priority = 20)]
        public static void ConfigureEditorSettings()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.productName = "Enxada & Encrenca";
            Debug.Log("[Setup] Configurações do editor aplicadas (modo 2D, texto, metas visíveis).");
        }

        [MenuItem("Enxada/Setup/Passos/2. Render Pipeline 2D (URP)", priority = 21)]
        public static void ConfigureRenderPipelineMenu() => ConfigureRenderPipeline();

        [MenuItem("Enxada/Setup/Passos/4. Regerar cena Boot", priority = 23)]
        public static void RebuildBootSceneMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            RebuildBootScene(EnsureGameConfig());
        }

        [MenuItem("Enxada/Setup/Passos/3. Cenas Boot e MainMenu", priority = 22)]
        public static void CreateScenesMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            CreateScenes(EnsureGameConfig());
        }

        private static GameConfig EnsureGameConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null)
                return config;

            EnsureFolder(Path.GetDirectoryName(ConfigPath));
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            Debug.Log($"[Setup] Criado {ConfigPath}");
            return config;
        }

        private static bool ConfigureRenderPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                // Procurado por reflexão para não depender do nome do assembly 2D do URP,
                // que muda entre versões.
                var rendererType = TypeCache.GetTypesDerivedFrom<ScriptableRendererData>()
                    .FirstOrDefault(t => t.Name == "Renderer2DData");
                if (rendererType == null)
                {
                    Debug.LogError("[Setup] Renderer2DData não encontrado. O pacote URP está instalado?");
                    return false;
                }

                EnsureFolder(RenderingFolder);
                var rendererData = (ScriptableRendererData)ScriptableObject.CreateInstance(rendererType);
                AssetDatabase.CreateAsset(rendererData, RendererPath);

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                Debug.Log($"[Setup] Criados {PipelinePath} e {RendererPath}");
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                var levelPipeline = QualitySettings.GetRenderPipelineAssetAt(i);
                if (levelPipeline != null && levelPipeline != pipeline)
                    Debug.LogWarning($"[Setup] O nível de qualidade '{QualitySettings.names[i]}' usa outro " +
                                     "Render Pipeline. Limpe o campo em Project Settings > Quality.");
            }

            Debug.Log("[Setup] URP 2D definido como Render Pipeline padrão.");
            return true;
        }

        private static void CreateScenes(GameConfig config)
        {
            EnsureFolder(ScenesFolder);

            RebuildBootScene(config);

            CreateSceneIfMissing(MainMenuScenePath, () => CreateCamera());

            var required = new[] { BootScenePath, MainMenuScenePath };
            var others = EditorBuildSettings.scenes.Where(s => !required.Contains(s.path));
            EditorBuildSettings.scenes = required
                .Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(others)
                .ToArray();

            EditorSceneManager.OpenScene(BootScenePath);
            Debug.Log("[Setup] Build Settings: Boot (0), MainMenu (1).");
        }

        /// <summary>
        /// A cena Boot é gerada: contém só o GameBootstrap com os instaladores de serviços,
        /// que crescem a cada etapa. Por isso é recriada toda vez (não edite à mão).
        /// </summary>
        public static void RebuildBootScene(GameConfig config)
        {
            EnsureFolder(ScenesFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BootstrapBuilder.Build(config, true, out _);
            EditorSceneManager.SaveScene(scene, BootScenePath);
            Debug.Log($"[Setup] Cena Boot (re)criada em {BootScenePath}");
        }

        public static GameConfig LoadOrCreateGameConfig() => EnsureGameConfig();

        private static void CreateSceneIfMissing(string path, System.Action populate)
        {
            if (File.Exists(path))
            {
                Debug.Log($"[Setup] {path} já existe, mantido como está.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Setup] Criada {path}");
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = ReferenceOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.27f, 0.16f);
            go.AddComponent<UniversalAdditionalCameraData>();
            return camera;
        }

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
