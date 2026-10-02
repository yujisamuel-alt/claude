using System;
using System.Collections.Generic;
using Enxada.Core;

namespace Enxada.Farming
{
    /// <summary>Estado de um tile arado.</summary>
    public struct FarmTile
    {
        public bool Watered;

        /// <summary>Tem planta (Etapa 5). Terra ocupada não volta a ser grama.</summary>
        public bool Occupied;
    }

    /// <summary>O que mudou na virada do dia, para a fazenda redesenhar os tiles.</summary>
    public sealed class FarmDayResult
    {
        public readonly List<CellPosition> Reverted = new List<CellPosition>();
        public readonly List<CellPosition> Refreshed = new List<CellPosition>();
    }

    /// <summary>
    /// Terra arada do sítio: quais tiles foram arados, regados ou estão ocupados por plantas.
    /// Só guarda o estado; saber se o chão é "arável" é papel da cena (tilemaps).
    /// </summary>
    public sealed class FarmGrid
    {
        private readonly Dictionary<CellPosition, FarmTile> _tiles = new Dictionary<CellPosition, FarmTile>();

        public int TilledCount => _tiles.Count;
        public IReadOnlyDictionary<CellPosition, FarmTile> Tiles => _tiles;

        public event Action<CellPosition> TileChanged;

        public bool IsTilled(CellPosition cell) => _tiles.ContainsKey(cell);

        public bool IsWatered(CellPosition cell) => _tiles.TryGetValue(cell, out var tile) && tile.Watered;

        public bool IsOccupied(CellPosition cell) => _tiles.TryGetValue(cell, out var tile) && tile.Occupied;

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

        public bool SetOccupied(CellPosition cell, bool occupied)
        {
            if (!_tiles.TryGetValue(cell, out var tile) || tile.Occupied == occupied)
                return false;

            tile.Occupied = occupied;
            _tiles[cell] = tile;
            TileChanged?.Invoke(cell);
            return true;
        }

        /// <summary>
        /// Vira o dia: a terra seca (ou fica regada se choveu) e a terra arada sem planta pode voltar
        /// a ser grama, com a chance dada. <paramref name="random"/> devolve valores em [0, 1).
        /// </summary>
        public FarmDayResult AdvanceDay(bool rained, Func<double> random, double revertChance)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var result = new FarmDayResult();
            var cells = new List<CellPosition>(_tiles.Keys);

            foreach (var cell in cells)
            {
                var tile = _tiles[cell];
                if (!tile.Occupied && random() < revertChance)
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
