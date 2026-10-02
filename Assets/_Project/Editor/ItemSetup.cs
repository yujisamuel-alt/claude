using System;
using System.Collections.Generic;
using Enxada.Core;
using Enxada.Inventory;
using UnityEditor;
using UnityEngine;

namespace Enxada.EditorTools
{
    /// <summary>
    /// Cria os itens do jogo (ScriptableObjects com ícones placeholder), o ItemDatabase e o InventoryConfig.
    /// Só cria o que não existe: ajustes de preço feitos no Inspector não são sobrescritos.
    /// Os nomes e descrições ficam em Data/Localization/pt-BR.txt (chaves item.&lt;id&gt;.name / .desc).
    /// </summary>
    public static class ItemSetup
    {
        public const string ItemsFolder = "Assets/_Project/Data/Items";
        public const string DatabasePath = ItemsFolder + "/ItemDatabase.asset";
        public const string ConfigPath = "Assets/_Project/Data/Config/InventoryConfig.asset";
        private const string IconsFolder = "Assets/_Project/Art/Sprites/Items";

        private static readonly Color32 Wood = new Color32(130, 88, 50, 255);
        private static readonly Color32 Metal = new Color32(176, 182, 190, 255);
        private static readonly Color32 DarkMetal = new Color32(118, 124, 134, 255);
        private static readonly Color32 Paper = new Color32(232, 214, 168, 255);

        private struct Entry
        {
            public string Id;
            public ItemCategory Category;
            public int SellPrice;
            public int MaxStack;
            public Action<Color32[], int> Paint;
            public ToolType ToolType;
        }

        // Preços de venda das colheitas vêm da tabela da Primavera; os demais são placeholders de balanceamento.
        private static List<Entry> Entries() => new List<Entry>
        {
            Tool("hoe", ToolType.Hoe, PaintHoe),
            Tool("watering_can", ToolType.WateringCan, PaintWateringCan),
            Tool("scythe", ToolType.Scythe, PaintScythe),
            Tool("axe", ToolType.Axe, PaintAxe),
            Tool("pickaxe", ToolType.Pickaxe, PaintPickaxe),
            Stackable("wood", ItemCategory.Resource, 2, PaintWood),
            Stackable("stone", ItemCategory.Resource, 2, PaintStone),
            Stackable("lettuce_seed", ItemCategory.Seed, 10, (p, s) => PaintSeed(p, s, new Color32(110, 190, 90, 255))),
            Stackable("cassava_seed", ItemCategory.Seed, 15, (p, s) => PaintSeed(p, s, new Color32(176, 130, 80, 255))),
            Stackable("bean_seed", ItemCategory.Seed, 30, (p, s) => PaintSeed(p, s, new Color32(60, 140, 70, 255))),
            Stackable("corn_seed", ItemCategory.Seed, 25, (p, s) => PaintSeed(p, s, new Color32(240, 200, 60, 255))),
            Stackable("strawberry_seed", ItemCategory.Seed, 50, (p, s) => PaintSeed(p, s, new Color32(220, 60, 70, 255))),
            Stackable("lettuce", ItemCategory.Crop, 35, PaintLettuce),
            Stackable("cassava", ItemCategory.Crop, 60, PaintCassava),
            Stackable("bean", ItemCategory.Crop, 40, PaintBean),
            Stackable("corn", ItemCategory.Crop, 110, PaintCorn),
            Stackable("strawberry", ItemCategory.Crop, 120, PaintStrawberry)
        };

        private static Entry Tool(string id, ToolType toolType, Action<Color32[], int> paint) =>
            new Entry { Id = id, Category = ItemCategory.Tool, SellPrice = 0, MaxStack = 1, Paint = paint, ToolType = toolType };

        private static Entry Stackable(string id, ItemCategory category, int price, Action<Color32[], int> paint) =>
            new Entry { Id = id, Category = category, SellPrice = price, MaxStack = 999, Paint = paint };

        public static ItemDatabase EnsureDatabase()
        {
            var created = new List<ItemDefinition>();
            foreach (var entry in Entries())
            {
                var icon = PlaceholderArt.EnsureSpriteIn(IconsFolder, "icon_" + entry.Id, entry.Paint);
                var item = SetupUtil.EnsureAsset<ItemDefinition>($"{ItemsFolder}/{entry.Id}.asset",
                    asset => asset.Initialize(entry.Id, entry.Category, entry.SellPrice, entry.MaxStack, icon, entry.ToolType));
                EnsureToolType(item, entry.ToolType);
                created.Add(item);
            }

            var database = SetupUtil.EnsureAsset<ItemDatabase>(DatabasePath);
            var so = new SerializedObject(database);
            var list = so.FindProperty("items");
            list.arraySize = created.Count;
            for (var i = 0; i < created.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = created[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        // Itens criados antes da Etapa 4 não tinham tipo de ferramenta: corrige sem mexer em preço, nível etc.
        private static void EnsureToolType(ItemDefinition item, ToolType toolType)
        {
            if (toolType == ToolType.None)
                return;

            var so = new SerializedObject(item);
            var property = so.FindProperty("toolType");
            if (property.enumValueIndex == (int)toolType)
                return;

            property.enumValueIndex = (int)toolType;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        public static InventoryConfig EnsureConfig(ItemDatabase database)
        {
            return SetupUtil.EnsureAsset<InventoryConfig>(ConfigPath, config =>
            {
                // Itens iniciais de um jogo novo: as 5 ferramentas e algumas sementes de Alface e Feijão.
                var starting = new[]
                {
                    ("hoe", 1), ("watering_can", 1), ("scythe", 1), ("axe", 1), ("pickaxe", 1),
                    ("lettuce_seed", 15), ("bean_seed", 5)
                };

                var so = new SerializedObject(config);
                var array = so.FindProperty("startingItems");
                array.arraySize = starting.Length;
                for (var i = 0; i < starting.Length; i++)
                {
                    var element = array.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("item").objectReferenceValue = Find(database, starting[i].Item1);
                    element.FindPropertyRelative("quantity").intValue = starting[i].Item2;
                    element.FindPropertyRelative("quality").enumValueIndex = (int)ItemQuality.Normal;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        public static ItemDefinition Find(ItemDatabase database, string id) =>
            database.TryGet(id, out var definition) ? definition : null;

        // ------------------------------------------------------------------ ícones placeholder 16x16

        private static void PaintHoe(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Line(px, s, 3, 2, 12, 11, Wood);
            PlaceholderArt.Line(px, s, 4, 2, 13, 11, Wood);
            PlaceholderArt.Rect(px, s, 10, 11, 15, 14, Metal);
            PlaceholderArt.Rect(px, s, 13, 9, 15, 11, DarkMetal);
        }

        private static void PaintAxe(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Line(px, s, 3, 2, 11, 12, Wood);
            PlaceholderArt.Line(px, s, 4, 2, 12, 12, Wood);
            PlaceholderArt.Rect(px, s, 9, 10, 15, 15, Metal);
            PlaceholderArt.Rect(px, s, 13, 10, 15, 13, DarkMetal);
        }

        private static void PaintPickaxe(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Line(px, s, 3, 2, 11, 11, Wood);
            PlaceholderArt.Line(px, s, 4, 2, 12, 11, Wood);
            PlaceholderArt.Line(px, s, 5, 13, 14, 13, Metal);
            PlaceholderArt.Line(px, s, 5, 12, 14, 12, Metal);
            PlaceholderArt.Rect(px, s, 4, 10, 6, 13, DarkMetal);
            PlaceholderArt.Rect(px, s, 13, 10, 15, 13, DarkMetal);
        }

        private static void PaintScythe(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Line(px, s, 3, 1, 9, 13, Wood);
            PlaceholderArt.Line(px, s, 4, 1, 10, 13, Wood);
            PlaceholderArt.Line(px, s, 9, 14, 14, 13, Metal);
            PlaceholderArt.Line(px, s, 14, 13, 14, 9, Metal);
            PlaceholderArt.Line(px, s, 9, 13, 13, 12, DarkMetal);
        }

        private static void PaintWateringCan(Color32[] px, int s)
        {
            var blue = new Color32(70, 120, 200, 255);
            var lightBlue = new Color32(120, 170, 230, 255);
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Rect(px, s, 3, 3, 11, 10, blue);
            PlaceholderArt.Rect(px, s, 3, 8, 11, 10, lightBlue);
            PlaceholderArt.Line(px, s, 11, 8, 15, 13, blue);
            PlaceholderArt.Line(px, s, 11, 7, 15, 12, blue);
            PlaceholderArt.Rect(px, s, 1, 5, 3, 12, DarkMetal);
            PlaceholderArt.Rect(px, s, 1, 11, 7, 13, DarkMetal);
        }

        private static void PaintWood(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Rect(px, s, 2, 3, 14, 8, Wood);
            PlaceholderArt.Rect(px, s, 3, 8, 13, 13, new Color32(156, 108, 62, 255));
            PlaceholderArt.Disc(px, s, 3f, 5.5f, 2.2f, new Color32(206, 160, 104, 255));
            PlaceholderArt.Disc(px, s, 4f, 10.5f, 2.2f, new Color32(206, 160, 104, 255));
        }

        private static void PaintStone(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Disc(px, s, 8f, 7f, 5.5f, DarkMetal);
            PlaceholderArt.Disc(px, s, 6.5f, 8.5f, 2.5f, Metal);
        }

        private static void PaintSeed(Color32[] px, int s, Color32 accent)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Rect(px, s, 4, 2, 12, 14, Paper);
            PlaceholderArt.Rect(px, s, 4, 12, 12, 14, new Color32(190, 168, 120, 255));
            PlaceholderArt.Disc(px, s, 8f, 7f, 2.6f, accent);
        }

        private static void PaintLettuce(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Disc(px, s, 8f, 7.5f, 6f, new Color32(96, 176, 80, 255));
            PlaceholderArt.Disc(px, s, 8f, 8f, 3.6f, new Color32(150, 214, 110, 255));
        }

        private static void PaintCassava(Color32[] px, int s)
        {
            var brown = new Color32(150, 104, 62, 255);
            PlaceholderArt.Fill(px, default);
            for (var offset = -1; offset <= 1; offset++)
                PlaceholderArt.Line(px, s, 3, 3 + offset, 13, 11 + offset, brown);
            PlaceholderArt.Rect(px, s, 12, 10, 15, 13, new Color32(236, 226, 200, 255));
        }

        private static void PaintBean(Color32[] px, int s)
        {
            var green = new Color32(60, 140, 70, 255);
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Line(px, s, 3, 12, 12, 3, green);
            PlaceholderArt.Line(px, s, 3, 11, 11, 3, green);
            PlaceholderArt.Line(px, s, 4, 13, 13, 4, new Color32(96, 176, 96, 255));
        }

        private static void PaintCorn(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Disc(px, s, 8f, 8f, 4f, new Color32(244, 208, 70, 255));
            PlaceholderArt.Rect(px, s, 5, 4, 11, 12, new Color32(244, 208, 70, 255));
            PlaceholderArt.Line(px, s, 5, 2, 8, 9, new Color32(90, 160, 70, 255));
            PlaceholderArt.Line(px, s, 11, 2, 8, 9, new Color32(90, 160, 70, 255));
        }

        private static void PaintStrawberry(Color32[] px, int s)
        {
            PlaceholderArt.Fill(px, default);
            PlaceholderArt.Disc(px, s, 8f, 7f, 5.5f, new Color32(220, 50, 70, 255));
            PlaceholderArt.Rect(px, s, 5, 11, 11, 14, new Color32(70, 160, 70, 255));
            PlaceholderArt.Disc(px, s, 6f, 6f, 0.9f, new Color32(250, 220, 120, 255));
            PlaceholderArt.Disc(px, s, 10f, 8f, 0.9f, new Color32(250, 220, 120, 255));
        }
    }
}
