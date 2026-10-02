using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Enxada.EditorTools
{
    /// <summary>
    /// Gera sprites e tiles placeholder (16x16, pixel art simples) por código.
    /// Só cria o que ainda não existe, então arte final colocada no lugar nunca é sobrescrita.
    /// </summary>
    public static class PlaceholderArt
    {
        public const int PixelsPerUnit = 16;
        private const string SpritesFolder = "Assets/_Project/Art/Sprites/Placeholders";
        private const string TilesFolder = "Assets/_Project/Art/Tiles";

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public static Sprite Grass() => EnsureSprite("grass", (px, s) =>
        {
            Fill(px, new Color32(88, 160, 64, 255));
            for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
            {
                var h = Hash(x, y, 1);
                if (h % 11 == 0) px[y * s + x] = new Color32(70, 140, 52, 255);
                else if (h % 17 == 0) px[y * s + x] = new Color32(112, 182, 82, 255);
            }
        });

        public static Sprite Dirt() => EnsureSprite("dirt", (px, s) =>
        {
            Fill(px, new Color32(146, 104, 62, 255));
            for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
                if (Hash(x, y, 2) % 9 == 0) px[y * s + x] = new Color32(124, 86, 50, 255);
        });

        public static Sprite Water() => EnsureSprite("water", (px, s) =>
        {
            Fill(px, new Color32(58, 118, 202, 255));
            for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
                if (y % 5 == 1 && (x + y) % 8 < 3) px[y * s + x] = new Color32(116, 168, 232, 255);
        });

        public static Sprite Tree() => EnsureSprite("tree", (px, s) =>
        {
            Fill(px, Clear);
            Rect(px, s, 7, 0, 9, 5, new Color32(110, 72, 40, 255)); // tronco
            Disc(px, s, 8f, 10f, 6.5f, new Color32(34, 102, 48, 255)); // copa
            Disc(px, s, 6f, 12f, 2.5f, new Color32(52, 130, 62, 255)); // brilho
        });

        public static Sprite Rock() => EnsureSprite("rock", (px, s) =>
        {
            Fill(px, Clear);
            Disc(px, s, 8f, 6f, 6f, new Color32(128, 128, 134, 255));
            Disc(px, s, 6.5f, 8f, 2.5f, new Color32(168, 168, 174, 255));
        });

        public static Sprite Bed() => EnsureSprite("bed", (px, s) =>
        {
            Fill(px, Clear);
            Rect(px, s, 1, 1, 15, 15, new Color32(120, 78, 44, 255));   // moldura de madeira
            Rect(px, s, 2, 2, 14, 11, new Color32(196, 70, 70, 255));   // cobertor
            Rect(px, s, 2, 11, 14, 14, new Color32(240, 240, 240, 255)); // travesseiro
        });

        public static Sprite Highlight() => EnsureSprite("tile_highlight", (px, s) =>
        {
            Fill(px, new Color32(255, 235, 80, 48));
            for (var i = 0; i < s; i++)
            {
                var edge = new Color32(255, 235, 80, 255);
                px[i] = edge;
                px[(s - 1) * s + i] = edge;
                px[i * s] = edge;
                px[i * s + s - 1] = edge;
            }
        });

        /// <summary>Sprites do jogador na ordem do enum FacingDirection: Baixo, Cima, Esquerda, Direita.</summary>
        public static Sprite[] PlayerFacings() => new[]
        {
            PlayerSprite("player_down", FacingKind.Down),
            PlayerSprite("player_up", FacingKind.Up),
            PlayerSprite("player_left", FacingKind.Left),
            PlayerSprite("player_right", FacingKind.Right)
        };

        public static Tile EnsureTile(string name, Sprite sprite, Tile.ColliderType collider)
        {
            var path = $"{TilesFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null)
                return tile;

            EnsureFolder(TilesFolder);
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = collider;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private enum FacingKind { Down, Up, Left, Right }

        private static Sprite PlayerSprite(string name, FacingKind kind) => EnsureSprite(name, (px, s) =>
        {
            var skin = new Color32(240, 196, 150, 255);
            var hair = new Color32(86, 52, 30, 255);
            var shirt = new Color32(60, 110, 190, 255);
            var pants = new Color32(70, 70, 100, 255);
            var eye = new Color32(30, 30, 40, 255);

            Fill(px, Clear);
            Rect(px, s, 5, 0, 11, 4, pants);   // pernas
            Rect(px, s, 4, 4, 12, 9, shirt);   // tronco
            Rect(px, s, 5, 9, 11, 15, skin);   // cabeça
            Rect(px, s, 5, 13, 11, 15, hair);  // cabelo no topo

            switch (kind)
            {
                case FacingKind.Down:
                    px[11 * s + 6] = eye;
                    px[11 * s + 9] = eye;
                    break;
                case FacingKind.Up:
                    Rect(px, s, 5, 9, 11, 15, hair); // de costas: só cabelo
                    break;
                case FacingKind.Left:
                    px[11 * s + 6] = eye;
                    Rect(px, s, 9, 9, 11, 13, hair);
                    break;
                case FacingKind.Right:
                    px[11 * s + 9] = eye;
                    Rect(px, s, 5, 9, 7, 13, hair);
                    break;
            }
        });

        private static Sprite EnsureSprite(string name, Action<Color32[], int> paint, int size = 16) =>
            EnsureSpriteIn(SpritesFolder, name, paint, size);

        /// <summary>Gera (se ainda não existir) um PNG 16x16 pintado por código e o importa como sprite pixel perfect.</summary>
        public static Sprite EnsureSpriteIn(string folder, string name, Action<Color32[], int> paint, int size = 16)
        {
            var path = $"{folder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            EnsureFolder(folder);

            var pixels = new Color32[size * size];
            paint(pixels, size);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void Fill(Color32[] px, Color32 color)
        {
            for (var i = 0; i < px.Length; i++) px[i] = color;
        }

        // Retângulo [x0,x1) x [y0,y1), origem embaixo à esquerda.
        public static void Rect(Color32[] px, int s, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
                px[y * s + x] = color;
        }

        public static void Disc(Color32[] px, int s, float cx, float cy, float radius, Color32 color)
        {
            for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
            {
                var dx = x + 0.5f - cx;
                var dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= radius * radius)
                    px[y * s + x] = color;
            }
        }

        /// <summary>Linha de 1 pixel (Bresenham), útil para cabos de ferramenta.</summary>
        public static void Line(Color32[] px, int s, int x0, int y0, int x1, int y1, Color32 color)
        {
            var dx = Math.Abs(x1 - x0);
            var dy = -Math.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx + dy;

            while (true)
            {
                if (x0 >= 0 && x0 < s && y0 >= 0 && y0 < s)
                    px[y0 * s + x0] = color;
                if (x0 == x1 && y0 == y1)
                    break;

                var e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h ^= h >> 13;
                return h & 0x7fffffff;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
