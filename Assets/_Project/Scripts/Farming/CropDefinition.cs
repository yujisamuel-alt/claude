using Enxada.Calendar;
using Enxada.Inventory;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>
    /// Uma cultura: de que semente nasce, quanto cresce em cada estágio, em que estações cresce,
    /// se rebrota depois da colheita e o sprite de cada estágio. As regras vivem em CropSpec (lógica pura).
    /// </summary>
    [CreateAssetMenu(menuName = "Enxada/World/Crop", fileName = "NewCrop")]
    public sealed class CropDefinition : ScriptableObject
    {
        [Tooltip("Identificador único (ex.: lettuce). Os saves usam isto: nunca mude depois de lançar.")]
        [SerializeField] private string id;

        [SerializeField] private ItemDefinition seedItem;
        [SerializeField] private ItemDefinition harvestItem;

        [Tooltip("Dias REGADOS que cada estágio dura. A soma é o tempo até amadurecer.")]
        [SerializeField] private int[] stageDays = { 1, 1, 1, 1 };

        [Tooltip("0 = colhe uma vez só. Maior que 0 = depois de colher, amadurece de novo em tantos dias.")]
        [Min(0)] [SerializeField] private int regrowDays;

        [SerializeField] private SeasonFlags seasons = SeasonFlags.Spring;
        [Min(1)] [SerializeField] private int harvestAmount = 1;

        [Tooltip("Um sprite por estágio, mais o da planta madura no fim (estágios + 1 sprites).")]
        [SerializeField] private Sprite[] stageSprites;

        public string Id => id;
        public ItemDefinition SeedItem => seedItem;
        public ItemDefinition HarvestItem => harvestItem;

        public CropSpec ToSpec() =>
            new CropSpec(id, seedItem.Id, harvestItem.Id, stageDays, regrowDays, seasons, harvestAmount);

        /// <summary>Sprite do estágio dado (0 = recém-plantada). Índices fora do intervalo usam o mais próximo.</summary>
        public Sprite SpriteForStage(int stage)
        {
            if (stageSprites == null || stageSprites.Length == 0)
                return null;

            return stageSprites[Mathf.Clamp(stage, 0, stageSprites.Length - 1)];
        }

        private void OnValidate()
        {
            if (stageDays != null && stageSprites != null && stageSprites.Length != stageDays.Length + 1)
                Debug.LogWarning($"[CropDefinition] '{name}': são necessários {stageDays.Length + 1} sprites " +
                                 $"(um por estágio mais o da planta madura), mas há {stageSprites.Length}.", this);
        }

        /// <summary>Usado pelos scripts de setup do editor.</summary>
        public void Initialize(string newId, ItemDefinition seed, ItemDefinition harvest, int[] newStageDays,
            int newRegrowDays, SeasonFlags newSeasons, Sprite[] sprites)
        {
            id = newId;
            seedItem = seed;
            harvestItem = harvest;
            stageDays = newStageDays;
            regrowDays = newRegrowDays;
            seasons = newSeasons;
            stageSprites = sprites;
        }
    }
}
