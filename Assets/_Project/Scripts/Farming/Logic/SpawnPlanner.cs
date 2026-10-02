using System;
using System.Collections.Generic;
using Enxada.Core;

namespace Enxada.Farming
{
    /// <summary>Sorteios do surgimento de mato, pedras e galhos. O sorteador é injetado para os testes serem exatos.</summary>
    public static class SpawnPlanner
    {
        /// <summary>Escolhe até <paramref name="count"/> tiles diferentes entre os candidatos.</summary>
        public static List<CellPosition> PickCells(IReadOnlyList<CellPosition> candidates, int count, Func<double> random)
        {
            var picked = new List<CellPosition>();
            if (candidates == null || count <= 0 || candidates.Count == 0)
                return picked;

            var pool = new List<CellPosition>(candidates);
            var take = Math.Min(count, pool.Count);
            for (var i = 0; i < take; i++)
            {
                var index = i + Math.Min((int)(random() * (pool.Count - i)), pool.Count - i - 1);
                var chosen = pool[index];
                pool[index] = pool[i];
                pool[i] = chosen;
                picked.Add(chosen);
            }

            return picked;
        }

        /// <summary>Sorteia um índice pelo peso. Devolve -1 se não houver peso nenhum.</summary>
        public static int PickWeightedIndex(IReadOnlyList<float> weights, Func<double> random)
        {
            var total = 0.0;
            for (var i = 0; i < weights.Count; i++)
                total += Math.Max(0f, weights[i]);

            if (total <= 0.0)
                return -1;

            var roll = random() * total;
            var running = 0.0;
            var lastValid = -1;
            for (var i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f)
                    continue;

                lastValid = i;
                running += weights[i];
                if (roll < running)
                    return i;
            }

            return lastValid;
        }

        /// <summary>Quantidade esperada vira inteiro: a parte inteira sempre, e a fração como chance de +1.</summary>
        public static int RollCount(float expected, Func<double> random)
        {
            if (expected <= 0f)
                return 0;

            var whole = (int)Math.Floor(expected);
            var fraction = expected - whole;
            return whole + (random() < fraction ? 1 : 0);
        }
    }
}
