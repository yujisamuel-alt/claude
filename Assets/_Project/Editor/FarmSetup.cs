using Enxada.Calendar;
using Enxada.Core;
using Enxada.Farming;
using Enxada.Inventory;
using Enxada.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Enxada.EditorTools
{
    /// <summary>
    /// Cria os dados da Etapa 4: configs de energia, ferramentas e fazenda, os tipos de objeto do sítio
    /// (mato, galho, pedra, árvore) e os tiles de terra arada. Só cria o que não existe,
    /// então números ajustados no Inspector são preservados.
    /// </summary>
    public static class FarmSetup
    {
        private const string ConfigFolder = "Assets/_Project/Data/Config";
        private const string WorldFolder = "Assets/_Project/Data/World";
        private const string PassOutChannelPath = "Assets/_Project/Data/Events/PassOutRequested.asset";

        public static FarmingConfig EnsureFarmingConfig() =>
            SetupUtil.EnsureAsset<FarmingConfig>(ConfigFolder + "/FarmingConfig.asset");

        public static EnergyConfig EnsureEnergyConfig() =>
            SetupUtil.EnsureAsset<EnergyConfig>(ConfigFolder + "/EnergyConfig.asset");

        public static VoidEventChannel EnsurePassOutChannel() =>
            SetupUtil.EnsureAsset<VoidEventChannel>(PassOutChannelPath);

        // Custos de energia dentro do intervalo 2–8 do design; são placeholders de balanceamento.
        public static ToolConfig EnsureToolConfig()
        {
            return SetupUtil.EnsureAsset<ToolConfig>(ConfigFolder + "/ToolConfig.asset", config =>
            {
                var entries = new[]
                {
                    (ToolType.Hoe, 3, 0.30f),
                    (ToolType.WateringCan, 2, 0.30f),
                    (ToolType.Scythe, 2, 0.30f),
                    (ToolType.Axe, 6, 0.35f),
                    (ToolType.Pickaxe, 5, 0.35f)
                };

                var so = new SerializedObject(config);
                var array = so.FindProperty("entries");
                array.arraySize = entries.Length;
                for (var i = 0; i < entries.Length; i++)
                {
                    var element = array.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("tool").enumValueIndex = (int)entries[i].Item1;
                    element.FindPropertyRelative("energyCost").intValue = entries[i].Item2;
                    element.FindPropertyRelative("useDuration").floatValue = entries[i].Item3;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        public static WeatherConfig EnsureWeatherConfig() =>
            SetupUtil.EnsureAsset<WeatherConfig>(ConfigFolder + "/WeatherConfig.asset");

        private struct CropEntry
        {
            public string Id;
            public int[] StageDays;
            public int RegrowDays;
            public Color32 Leaf;
            public Color32 Produce;
        }

        // Dias totais e rebrota seguem a tabela da Primavera; como os dias se dividem entre os estágios é placeholder.
        private static CropEntry[] SpringCrops() => new[]
        {
            new CropEntry { Id = "lettuce", StageDays = new[] { 1, 1, 1, 1 }, RegrowDays = 0,
                Leaf = new Color32(96, 176, 80, 255), Produce = new Color32(150, 214, 110, 255) },
            new CropEntry { Id = "cassava", StageDays = new[] { 1, 2, 2, 2 }, RegrowDays = 0,
                Leaf = new Color32(70, 140, 60, 255), Produce = new Color32(150, 104, 62, 255) },
            new CropEntry { Id = "bean", StageDays = new[] { 2, 2, 3, 3 }, RegrowDays = 3,
                Leaf = new Color32(60, 140, 70, 255), Produce = new Color32(130, 200, 90, 255) },
            new CropEntry { Id = "corn", StageDays = new[] { 2, 3, 3, 4 }, RegrowDays = 0,
                Leaf = new Color32(90, 160, 70, 255), Produce = new Color32(244, 208, 70, 255) },
            new CropEntry { Id = "strawberry", StageDays = new[] { 1, 2, 2, 3 }, RegrowDays = 4,
                Leaf = new Color32(70, 160, 70, 255), Produce = new Color32(220, 50, 70, 255) }
        };

        public static CropDatabase EnsureCropDatabase(ItemDatabase items)
        {
            var definitions = new System.Collections.Generic.List<CropDefinition>();
            foreach (var entry in SpringCrops())
            {
                var lastStage = entry.StageDays.Length;
                var sprites = new Sprite[lastStage + 1];
                for (var stage = 0; stage <= lastStage; stage++)
                    sprites[stage] = PlaceholderArt.CropStage(entry.Id, stage, lastStage, entry.Leaf, entry.Produce);

                var seed = ItemSetup.Find(items, entry.Id + "_seed");
                var harvest = ItemSetup.Find(items, entry.Id);
                var local = entry;
                definitions.Add(SetupUtil.EnsureAsset<CropDefinition>($"{WorldFolder}/crop_{entry.Id}.asset",
                    asset => asset.Initialize(local.Id, seed, harvest, local.StageDays, local.RegrowDays,
                        SeasonFlags.Spring, sprites)));
            }

            var database = SetupUtil.EnsureAsset<CropDatabase>(WorldFolder + "/CropDatabase.asset");
            var so = new SerializedObject(database);
            var list = so.FindProperty("crops");
            list.arraySize = definitions.Count;
            for (var i = 0; i < definitions.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        public static Tile TilledTile() =>
            PlaceholderArt.EnsureTile("SoilTilled", PlaceholderArt.SoilTilled(), Tile.ColliderType.None);

        public static Tile WateredTile() =>
            PlaceholderArt.EnsureTile("SoilWatered", PlaceholderArt.SoilWatered(), Tile.ColliderType.None);

        public static FarmObjectConfig EnsureObjectConfig(ItemDatabase items)
        {
            var weed = Node("weed", PlaceholderArt.Weed(), ToolType.Scythe, 1, null, 0, 0, false);
            var branch = Node("branch", PlaceholderArt.Branch(), ToolType.Axe, 1, ItemSetup.Find(items, "wood"), 1, 2, true);
            var rock = Node("rock", PlaceholderArt.Rock(), ToolType.Pickaxe, 2, ItemSetup.Find(items, "stone"), 1, 2, true);
            var tree = Node("tree", PlaceholderArt.Tree(), ToolType.Axe, 5, ItemSetup.Find(items, "wood"), 6, 10, true);

            return SetupUtil.EnsureAsset<FarmObjectConfig>(ConfigFolder + "/FarmObjectConfig.asset", config =>
            {
                var entries = new[]
                {
                    (weed, 6f, 70),
                    (branch, 2f, 25),
                    (rock, 2f, 25),
                    (tree, 0.5f, 8)
                };

                var so = new SerializedObject(config);
                var array = so.FindProperty("entries");
                array.arraySize = entries.Length;
                for (var i = 0; i < entries.Length; i++)
                {
                    var element = array.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("definition").objectReferenceValue = entries[i].Item1;
                    element.FindPropertyRelative("weight").floatValue = entries[i].Item2;
                    element.FindPropertyRelative("initialCount").intValue = entries[i].Item3;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static ResourceNodeDefinition Node(string id, Sprite sprite, ToolType tool, int hits,
            ItemDefinition drop, int dropMin, int dropMax, bool blocks)
        {
            return SetupUtil.EnsureAsset<ResourceNodeDefinition>($"{WorldFolder}/{id}.asset",
                asset => asset.Initialize(sprite, tool, hits, drop, dropMin, dropMax, blocks));
        }
    }
}
