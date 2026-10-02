using System;
using System.Collections.Generic;
using Enxada.Calendar;
using Enxada.Core;

namespace Enxada.Farming
{
    /// <summary>Uma planta: qual cultura e quantos dias regados ela já cresceu.</summary>
    public struct CropState
    {
        public string CropId;
        public int DaysGrown;

        public bool IsEmpty => string.IsNullOrEmpty(CropId);
    }

    /// <summary>Estado de um tile arado.</summary>
    public struct FarmTile
    {
        public bool Watered;
        public CropState Crop;

        public bool HasCrop => !Crop.IsEmpty;
    }

    public enum PlantResult
    {
        Planted,
        NotTilled,
        Occupied,
        WrongSeason
    }

    public readonly struct HarvestResult
    {
        public readonly bool Success;
        public readonly string ItemId;
        public readonly int Amount;

        /// <summary>Falso nas plantas que rebrotam: a planta continua lá, crescendo de novo.</summary>
        public readonly bool CropRemoved;

        public HarvestResult(bool success, string itemId, int amount, bool cropRemoved)
        {
            Success = success;
            ItemId = itemId;
            Amount = amount;
            CropRemoved = cropRemoved;
        }

        public static HarvestResult Failed => new HarvestResult(false, null, 0, false);
    }

    /// <summary>O que mudou na virada do dia, para a fazenda redesenhar os tiles.</summary>
    public sealed class FarmDayResult
    {
        /// <summary>Terra arada abandonada que voltou a ser grama.</summary>
        public readonly List<CellPosition> Reverted = new List<CellPosition>();

        /// <summary>Plantas que morreram por estarem fora da estação.</summary>
        public readonly List<CellPosition> Died = new List<CellPosition>();

        /// <summary>Plantas que cresceram esta noite.</summary>
        public readonly List<CellPosition> Grown = new List<CellPosition>();

        /// <summary>Todos os tiles arados que continuam existindo.</summary>
        public readonly List<CellPosition> Refreshed = new List<CellPosition>();
    }

    /// <summary>
    /// A terra do sítio: tiles arados, se estão regados e que planta têm. Só guarda estado e aplica as
    /// regras; saber se o chão é "arável" é papel da cena (tilemaps).
    /// </summary>
    public sealed class FarmGrid
    {
        private readonly Dictionary<CellPosition, FarmTile> _tiles = new Dictionary<CellPosition, FarmTile>();

        public int TilledCount => _tiles.Count;
        public IReadOnlyDictionary<CellPosition, FarmTile> Tiles => _tiles;

        public event Action<CellPosition> TileChanged;

        public bool IsTilled(CellPosition cell) => _tiles.ContainsKey(cell);

        public bool IsWatered(CellPosition cell) => _tiles.TryGetValue(cell, out var tile) && tile.Watered;

        /// <summary>Tem planta (terra ocupada não volta a ser grama).</summary>
        public bool IsOccupied(CellPosition cell) => _tiles.TryGetValue(cell, out var tile) && tile.HasCrop;

        public CropState GetCrop(CellPosition cell) =>
            _tiles.TryGetValue(cell, out var tile) ? tile.Crop : default;

        /// <summary>Ara o tile. Falso se já estava arado.</summary>
        public bool Till(CellPosition cell)
        {
            if (_tiles.ContainsKey(cell))
                return false;

            _tiles[cell] = new FarmTile();
            TileChanged?.Invoke(cell);
            return true;
        }

        /// <summary>Rega o tile. Falso se não está arado ou já está regado.</summary>
        public bool Water(CellPosition cell)
        {
            if (!_tiles.TryGetValue(cell, out var tile) || tile.Watered)
                return false;

            tile.Watered = true;
            _tiles[cell] = tile;
            TileChanged?.Invoke(cell);
            return true;
        }

        public PlantResult TryPlant(CellPosition cell, CropSpec spec, Season season)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (!_tiles.TryGetValue(cell, out var tile))
                return PlantResult.NotTilled;
            if (tile.HasCrop)
                return PlantResult.Occupied;
            if (!spec.GrowsIn(season))
                return PlantResult.WrongSeason;

            tile.Crop = new CropState { CropId = spec.Id, DaysGrown = 0 };
            _tiles[cell] = tile;
            TileChanged?.Invoke(cell);
            return PlantResult.Planted;
        }

        /// <summary>
        /// Colhe a planta madura. Se a cultura rebrota, a planta volta alguns dias no crescimento
        /// (e fica a RegrowDays de amadurecer de novo); senão, sai do tile e a terra continua arada.
        /// </summary>
        public HarvestResult TryHarvest(CellPosition cell, ICropCatalog crops)
        {
            if (!_tiles.TryGetValue(cell, out var tile) || !tile.HasCrop || !crops.TryGet(tile.Crop.CropId, out var spec)
                || !spec.IsMature(tile.Crop.DaysGrown))
                return HarvestResult.Failed;

            var removed = !spec.Regrows;
            tile.Crop = removed
                ? default
                : new CropState { CropId = spec.Id, DaysGrown = spec.TotalDays - spec.RegrowDays };
            _tiles[cell] = tile;
            TileChanged?.Invoke(cell);
            return new HarvestResult(true, spec.HarvestItemId, spec.HarvestAmount, removed);
        }

        /// <summary>
        /// Vira o dia, nesta ordem: (1) cada planta fora da estação nova morre; (2) as regadas crescem um dia;
        /// (3) terra arada sem planta pode voltar a ser grama; (4) a terra seca, ou fica regada se vai chover.
        /// <paramref name="random"/> devolve valores em [0, 1).
        /// </summary>
        public FarmDayResult AdvanceDay(ICropCatalog crops, Season newSeason, bool rained, Func<double> random,
            double revertChance)
        {
            if (crops == null)
                throw new ArgumentNullException(nameof(crops));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var result = new FarmDayResult();
            var cells = new List<CellPosition>(_tiles.Keys);

            foreach (var cell in cells)
            {
                var tile = _tiles[cell];
                var diedTonight = false;

                if (tile.HasCrop)
                {
                    if (!crops.TryGet(tile.Crop.CropId, out var spec) || !spec.GrowsIn(newSeason))
                    {
                        tile.Crop = default;
                        diedTonight = true;
                        result.Died.Add(cell);
                    }
                    else if (tile.Watered && !spec.IsMature(tile.Crop.DaysGrown))
                    {
                        tile.Crop.DaysGrown++;
                        result.Grown.Add(cell);
                    }
                }

                if (!tile.HasCrop && !diedTonight && random() < revertChance)
                {
                    _tiles.Remove(cell);
                    result.Reverted.Add(cell);
                    continue;
                }

                tile.Watered = rained;
                _tiles[cell] = tile;
                result.Refreshed.Add(cell);
            }

            return result;
        }

        /// <summary>Restaura um tile (carregar save).</summary>
        public void Restore(CellPosition cell, FarmTile tile) => _tiles[cell] = tile;

        public void Clear() => _tiles.Clear();
    }
}
