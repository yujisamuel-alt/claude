using System;
using System.Collections.Generic;
using Enxada.Calendar;

namespace Enxada.Farming
{
    /// <summary>
    /// Regras de crescimento de uma cultura (lógica pura; o ScriptableObject CropDefinition converte para isto).
    /// StageDays diz quantos dias REGADOS cada estágio dura; a soma é o tempo até amadurecer.
    /// Há um sprite a mais que os estágios: o último é a planta madura.
    /// </summary>
    public sealed class CropSpec
    {
        private readonly int[] _stageDays;

        public CropSpec(string id, string seedItemId, string harvestItemId, IReadOnlyList<int> stageDays,
            int regrowDays, SeasonFlags seasons, int harvestAmount = 1)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A cultura precisa de um id.", nameof(id));
            if (string.IsNullOrEmpty(seedItemId) || string.IsNullOrEmpty(harvestItemId))
                throw new ArgumentException("A cultura precisa de item de semente e de colheita.");
            if (stageDays == null || stageDays.Count == 0)
                throw new ArgumentException("A cultura precisa de ao menos um estágio.", nameof(stageDays));
            if (seasons == SeasonFlags.None)
                throw new ArgumentException("A cultura precisa crescer em alguma estação.", nameof(seasons));
            if (harvestAmount < 1)
                throw new ArgumentOutOfRangeException(nameof(harvestAmount));

            _stageDays = new int[stageDays.Count];
            var total = 0;
            for (var i = 0; i < _stageDays.Length; i++)
            {
                if (stageDays[i] < 1)
                    throw new ArgumentException("Cada estágio dura ao menos 1 dia.", nameof(stageDays));

                _stageDays[i] = stageDays[i];
                total += stageDays[i];
            }

            if (regrowDays < 0 || regrowDays >= total)
                throw new ArgumentException("A rebrota precisa ser menor que o tempo total de crescimento.", nameof(regrowDays));

            Id = id;
            SeedItemId = seedItemId;
            HarvestItemId = harvestItemId;
            TotalDays = total;
            RegrowDays = regrowDays;
            Seasons = seasons;
            HarvestAmount = harvestAmount;
        }

        public string Id { get; }
        public string SeedItemId { get; }
        public string HarvestItemId { get; }

        /// <summary>Dias regados até amadurecer.</summary>
        public int TotalDays { get; }

        /// <summary>0 = colhe uma vez só. Maior que 0 = depois de colher, volta a amadurecer em tantos dias.</summary>
        public int RegrowDays { get; }

        public SeasonFlags Seasons { get; }
        public int HarvestAmount { get; }

        public bool Regrows => RegrowDays > 0;

        /// <summary>Quantidade de sprites: um por estágio mais o da planta madura.</summary>
        public int SpriteCount => _stageDays.Length + 1;

        public bool GrowsIn(Season season) => Seasons.Includes(season);

        public bool IsMature(int daysGrown) => daysGrown >= TotalDays;

        /// <summary>Qual sprite mostrar: 0 é a semente recém-plantada e SpriteCount - 1 é a planta madura.</summary>
        public int StageIndex(int daysGrown)
        {
            var stage = 0;
            var accumulated = 0;
            for (var i = 0; i < _stageDays.Length; i++)
            {
                accumulated += _stageDays[i];
                if (daysGrown >= accumulated)
                    stage++;
            }

            return stage;
        }
    }

    /// <summary>De onde o jogo tira as regras das culturas (o CropDatabase implementa).</summary>
    public interface ICropCatalog
    {
        bool TryGet(string cropId, out CropSpec spec);

        /// <summary>Qual cultura nasce de determinada semente.</summary>
        bool TryGetBySeed(string seedItemId, out CropSpec spec);
    }
}
