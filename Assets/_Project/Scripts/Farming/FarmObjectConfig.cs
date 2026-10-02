using System;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Quais objetos (mato, galho, pedra, árvore) surgem no sítio, em que proporção e com que ritmo.</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Farm Object Config", fileName = "FarmObjectConfig")]
    public sealed class FarmObjectConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public ResourceNodeDefinition definition;

            [Tooltip("Peso no sorteio: quanto maior, mais comum.")]
            [Min(0f)] public float weight;

            [Tooltip("Quantos já existem ao começar.")]
            [Min(0)] public int initialCount;
        }

        [SerializeField] private Entry[] entries;

        [Tooltip("Quantos objetos novos surgem por dia, em média (2,5 = 2 ou 3).")]
        [Min(0f)] [SerializeField] private float dailySpawnCount = 6f;

        [Tooltip("O sítio nunca passa deste total de objetos.")]
        [Min(1)] [SerializeField] private int maxObjects = 160;

        public Entry[] Entries => entries;
        public float DailySpawnCount => dailySpawnCount;
        public int MaxObjects => maxObjects;
    }
}
