using Enxada.Calendar;
using Enxada.Core;
using Enxada.Farming;
using Enxada.Inventory;
using Enxada.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enxada.EditorTools
{
    /// <summary>
    /// Monta o objeto GameBootstrap com todos os instaladores de serviços e o ClockDriver.
    /// Usado na cena Boot e também em cenas de teste (que rodam sozinhas, sem passar pela Boot).
    /// </summary>
    public static class BootstrapBuilder
    {
        public const string ClockConfigPath = "Assets/_Project/Data/Config/ClockConfig.asset";
        public const string InputActionsPath = "Assets/_Project/Settings/Input/EnxadaControls.inputactions";
        public const string TextTablePath = "Assets/_Project/Data/Localization/pt-BR.txt";

        public static GameObject Build(GameConfig gameConfig, bool loadFirstScene, out ClockDriver clockDriver)
        {
            var clockConfig = SetupUtil.EnsureAsset<ClockConfig>(ClockConfigPath);
            var textTable = AssetDatabase.LoadAssetAtPath<TextAsset>(TextTablePath);
            if (textTable == null)
                Debug.LogError($"[Setup] {TextTablePath} não encontrado.");

            var go = new GameObject("GameBootstrap");

            var text = go.AddComponent<TextInstaller>();
            Wire(text, "portugueseTable", textTable);

            var calendar = go.AddComponent<CalendarInstaller>();
            Wire(calendar, "clockConfig", clockConfig);
            Wire(calendar, "weatherConfig", FarmSetup.EnsureWeatherConfig());

            var database = ItemSetup.EnsureDatabase();
            var inventoryConfig = ItemSetup.EnsureConfig(database);
            var inventory = go.AddComponent<InventoryInstaller>();
            Wire(inventory, "config", inventoryConfig);
            Wire(inventory, "database", database);

            var player = go.AddComponent<PlayerInstaller>();
            Wire(player, "energyConfig", FarmSetup.EnsureEnergyConfig());
            Wire(player, "toolConfig", FarmSetup.EnsureToolConfig());

            var farming = go.AddComponent<FarmingInstaller>();
            Wire(farming, "config", FarmSetup.EnsureFarmingConfig());
            Wire(farming, "cropDatabase", FarmSetup.EnsureCropDatabase(database));

            var energyController = go.AddComponent<EnergyController>();
            Wire(energyController, "passOutRequested", FarmSetup.EnsurePassOutChannel());

            clockDriver = go.AddComponent<ClockDriver>();

            var hotbarSelector = go.AddComponent<HotbarSelector>();
            Wire(hotbarSelector, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));

            // O Bootstrap vai por último: seu Awake (ordem -1000) é o que chama os instaladores,
            // e os componentes acima já existem no mesmo objeto.
            var bootstrap = go.AddComponent<GameBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("config").objectReferenceValue = gameConfig;
            so.FindProperty("loadFirstScene").boolValue = loadFirstScene;
            var installers = so.FindProperty("installers");
            installers.arraySize = 5;
            installers.GetArrayElementAtIndex(0).objectReferenceValue = text;
            installers.GetArrayElementAtIndex(1).objectReferenceValue = calendar;
            installers.GetArrayElementAtIndex(2).objectReferenceValue = inventory;
            installers.GetArrayElementAtIndex(3).objectReferenceValue = player;
            installers.GetArrayElementAtIndex(4).objectReferenceValue = farming;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
