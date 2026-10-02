using System.Collections.Generic;
using Enxada.Calendar;
using Enxada.Core;
using Enxada.Inventory;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Enxada.Farming
{
    /// <summary>
    /// Liga o estado da terra (FarmGrid) aos tilemaps: arar com a enxada, regar com o regador, plantar sementes,
    /// colher com a foice, encher o regador na água e a virada do dia (plantas crescem, a terra seca,
    /// a chuva rega tudo e a terra abandonada volta a ser grama).
    /// </summary>
    public sealed class FarmTilemapController : MonoBehaviour, ITileToolHandler, ISeedPlanter
    {
        private const float HarvestOverflowSpeed = 2f;
        private const float HarvestOverflowDelay = 0.3f;

        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap ground;
        [SerializeField] private Tilemap soil;
        [SerializeField] private Tilemap crops;
        [SerializeField] private Tilemap obstacles;
        [SerializeField] private TileBase tilledTile;
        [SerializeField] private TileBase wateredTile;
        [SerializeField] private TileBase waterTile;

        [Tooltip("Tiles de chão onde se pode arar (grama, terra batida...).")]
        [SerializeField] private TileBase[] tillableGroundTiles;

        private readonly TileProbe _probe = new TileProbe();
        private readonly Dictionary<(string cropId, int stage), Tile> _cropTiles = new Dictionary<(string, int), Tile>();
        private FarmGrid _farm;
        private WateringCanState _can;
        private FarmingConfig _config;
        private CropDatabase _cropDatabase;
        private InventoryModel _inventory;
        private WeatherModel _weather;
        private GameClock _clock;

        // Tudo no Awake: o FarmObjectField (mesmo objeto) usa IsFreeGround no próprio Start, e os serviços
        // já existem porque o GameBootstrap roda antes de todos (execution order -1000).
        private void Awake()
        {
            ServiceLocator.Replace<ITileToolHandler>(this);
            ServiceLocator.Replace<ISeedPlanter>(this);

            _farm = ServiceLocator.Get<FarmGrid>();
            _can = ServiceLocator.Get<WateringCanState>();
            _config = ServiceLocator.Get<FarmingConfig>();
            _cropDatabase = ServiceLocator.Get<CropDatabase>();
            _inventory = ServiceLocator.Get<InventoryModel>();
            _weather = ServiceLocator.Get<WeatherModel>();
            _clock = ServiceLocator.Get<GameClock>();

            _clock.DayEnded += OnDayEnded;
            RenderAll(); // a terra e as plantas sobrevivem à troca de cena porque o estado vive nos serviços
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ITileToolHandler>(this);
            ServiceLocator.Unregister<ISeedPlanter>(this);
            if (_clock != null)
                _clock.DayEnded -= OnDayEnded;

            foreach (var tile in _cropTiles.Values)
                Destroy(tile);
            _cropTiles.Clear();
        }

        /// <summary>Chão que aceita arar (e receber objetos): arável, sem obstáculo e ainda sem arar.</summary>
        public bool IsFreeGround(CellPosition cell)
        {
            var position = ToVector3Int(cell);
            return IsTillableGround(position) && obstacles.GetTile(position) == null && !_farm.IsTilled(cell);
        }

        public ToolUseOutcome TryUse(ToolType tool, int tier, CellPosition cell)
        {
            switch (tool)
            {
                case ToolType.Hoe: return UseHoe(cell);
                case ToolType.WateringCan: return UseWateringCan(cell);
                case ToolType.Scythe: return UseScythe(cell);
                default: return ToolUseOutcome.None;
            }
        }

        public ToolUseOutcome TryPlant(string seedItemId, CellPosition cell)
        {
            if (!_cropDatabase.TryGetBySeed(seedItemId, out var spec))
                return ToolUseOutcome.None;

            switch (_farm.TryPlant(cell, spec, _clock.Date.Season))
            {
                case PlantResult.Planted:
                    RenderCell(cell);
                    return ToolUseOutcome.Used;

                case PlantResult.WrongSeason:
                    return ToolUseOutcome.Rejected("toast.wrong_season");

                default:
                    return ToolUseOutcome.None; // sem terra arada, ou já tem planta: nada a dizer
            }
        }

        private ToolUseOutcome UseHoe(CellPosition cell)
        {
            var position = ToVector3Int(cell);
            if (!IsTillableGround(position) || obstacles.GetTile(position) != null || _farm.IsTilled(cell)
                || _probe.IsBlocked(grid.GetCellCenterWorld(position)))
                return ToolUseOutcome.None;

            _farm.Till(cell);
            RenderCell(cell);
            return ToolUseOutcome.Used;
        }

        private ToolUseOutcome UseWateringCan(CellPosition cell)
        {
            var position = ToVector3Int(cell);

            // Água do rio ou da lagoa: enche o regador sem gastar energia.
            if (waterTile != null && obstacles.GetTile(position) == waterTile)
            {
                _can.Refill();
                return ToolUseOutcome.UsedFree;
            }

            if (!_farm.IsTilled(cell) || _farm.IsWatered(cell))
                return ToolUseOutcome.None;

            if (!_can.TryUse())
                return ToolUseOutcome.Rejected("toast.can_empty");

            _farm.Water(cell);
            RenderCell(cell);
            return ToolUseOutcome.Used;
        }

        private ToolUseOutcome UseScythe(CellPosition cell)
        {
            var harvest = _farm.TryHarvest(cell, _cropDatabase);
            if (!harvest.Success)
                return ToolUseOutcome.None;

            GiveHarvest(harvest, cell);
            RenderCell(cell);
            return ToolUseOutcome.Used;
        }

        // A colheita vai direto para a mochila; o que não couber cai no chão ao lado da planta.
        private void GiveHarvest(HarvestResult harvest, CellPosition cell)
        {
            var leftover = _inventory.TryAdd(new ItemStack(harvest.ItemId, harvest.Amount));
            if (leftover.IsEmpty || !ServiceLocator.TryGet<WorldItemSpawner>(out var spawner))
                return;

            spawner.Spawn(leftover, grid.GetCellCenterWorld(ToVector3Int(cell)),
                Random.insideUnitCircle.normalized * HarvestOverflowSpeed, HarvestOverflowDelay);
        }

        private void OnDayEnded(DayEndedInfo info)
        {
            // O clima do novo dia já foi sorteado (o CalendarInstaller se inscreve antes de todos).
            _farm.AdvanceDay(_cropDatabase, info.NewDate.Season, _weather.IsRainingToday, () => Random.value,
                _config.TilledRevertChance);
            RenderAll();
        }

        /// <summary>Só para testes (tecla F5): rega todas as terras aradas de uma vez.</summary>
        public void DebugWaterAll()
        {
            var cells = new List<CellPosition>(_farm.Tiles.Keys);
            foreach (var cell in cells)
            {
                if (_farm.Water(cell))
                    RenderCell(cell);
            }
        }

        // ------------------------------------------------------------------ desenho

        private void RenderAll()
        {
            soil.ClearAllTiles();
            crops.ClearAllTiles();
            foreach (var pair in _farm.Tiles)
                RenderCell(pair.Key);
        }

        private void RenderCell(CellPosition cell)
        {
            var position = ToVector3Int(cell);
            if (!_farm.Tiles.TryGetValue(cell, out var tile))
            {
                soil.SetTile(position, null);
                crops.SetTile(position, null);
                return;
            }

            soil.SetTile(position, tile.Watered ? wateredTile : tilledTile);
            crops.SetTile(position, tile.HasCrop ? CropTile(tile.Crop) : null);
        }

        // Os tiles das plantas são criados na hora (um por cultura e estágio), sem precisar de assets.
        private TileBase CropTile(CropState crop)
        {
            if (!_cropDatabase.TryGet(crop.CropId, out var spec)
                || !_cropDatabase.TryGetDefinition(crop.CropId, out var definition))
                return null;

            var stage = spec.StageIndex(crop.DaysGrown);
            var key = (crop.CropId, stage);
            if (_cropTiles.TryGetValue(key, out var cached))
                return cached;

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = definition.SpriteForStage(stage);
            tile.colliderType = Tile.ColliderType.None;
            _cropTiles[key] = tile;
            return tile;
        }

        private bool IsTillableGround(Vector3Int position)
        {
            var tile = ground.GetTile(position);
            return tile != null && System.Array.IndexOf(tillableGroundTiles, tile) >= 0;
        }

        private static Vector3Int ToVector3Int(CellPosition cell) => new Vector3Int(cell.X, cell.Y, 0);
    }
}
