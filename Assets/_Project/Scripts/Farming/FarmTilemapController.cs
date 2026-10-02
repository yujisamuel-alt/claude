using Enxada.Calendar;
using Enxada.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Enxada.Farming
{
    /// <summary>
    /// Liga o estado da terra (FarmGrid) aos tilemaps: arar com a enxada, regar com o regador,
    /// encher o regador na água, e a virada do dia (a terra seca e a terra abandonada volta a ser grama).
    /// </summary>
    public sealed class FarmTilemapController : MonoBehaviour, ITileToolHandler
    {
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap ground;
        [SerializeField] private Tilemap soil;
        [SerializeField] private Tilemap obstacles;
        [SerializeField] private TileBase tilledTile;
        [SerializeField] private TileBase wateredTile;
        [SerializeField] private TileBase waterTile;

        [Tooltip("Tiles de chão onde se pode arar (grama, terra batida...).")]
        [SerializeField] private TileBase[] tillableGroundTiles;

        private readonly TileProbe _probe = new TileProbe();
        private FarmGrid _farm;
        private WateringCanState _can;
        private FarmingConfig _config;
        private GameClock _clock;

        // Tudo no Awake: o FarmObjectField (mesmo objeto) usa IsFreeGround no próprio Start, e os serviços
        // já existem porque o GameBootstrap roda antes de todos (execution order -1000).
        private void Awake()
        {
            ServiceLocator.Replace<ITileToolHandler>(this);

            _farm = ServiceLocator.Get<FarmGrid>();
            _can = ServiceLocator.Get<WateringCanState>();
            _config = ServiceLocator.Get<FarmingConfig>();
            _clock = ServiceLocator.Get<GameClock>();

            _clock.DayEnded += OnDayEnded;
            RenderAll(); // a terra arada sobrevive à troca de cena porque o estado vive no serviço
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ITileToolHandler>(this);
            if (_clock != null)
                _clock.DayEnded -= OnDayEnded;
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
                default: return ToolUseOutcome.None;
            }
        }

        private ToolUseOutcome UseHoe(CellPosition cell)
        {
            var position = ToVector3Int(cell);
            if (!IsTillableGround(position) || obstacles.GetTile(position) != null || _farm.IsTilled(cell)
                || _probe.IsBlocked(grid.GetCellCenterWorld(position)))
                return ToolUseOutcome.None;

            _farm.Till(cell);
            soil.SetTile(position, tilledTile);
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
            soil.SetTile(position, wateredTile);
            return ToolUseOutcome.Used;
        }

        private void OnDayEnded(DayEndedInfo info)
        {
            // Chuva e plantas chegam na Etapa 5; por enquanto nunca chove.
            _farm.AdvanceDay(false, () => Random.value, _config.TilledRevertChance);
            RenderAll();
        }

        private void RenderAll()
        {
            soil.ClearAllTiles();
            foreach (var pair in _farm.Tiles)
                soil.SetTile(ToVector3Int(pair.Key), pair.Value.Watered ? wateredTile : tilledTile);
        }

        private bool IsTillableGround(Vector3Int position)
        {
            var tile = ground.GetTile(position);
            return tile != null && System.Array.IndexOf(tillableGroundTiles, tile) >= 0;
        }

        private static Vector3Int ToVector3Int(CellPosition cell) => new Vector3Int(cell.X, cell.Y, 0);
    }
}
