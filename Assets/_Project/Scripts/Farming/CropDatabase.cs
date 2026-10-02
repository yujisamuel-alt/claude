using System.Collections.Generic;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Todas as culturas do jogo. É o catálogo que a fazenda consulta (ICropCatalog).</summary>
    [CreateAssetMenu(menuName = "Enxada/World/Crop Database", fileName = "CropDatabase")]
    public sealed class CropDatabase : ScriptableObject, ICropCatalog
    {
        [SerializeField] private List<CropDefinition> crops = new List<CropDefinition>();

        private Dictionary<string, CropDefinition> _definitionsById;
        private Dictionary<string, CropSpec> _specsById;
        private Dictionary<string, CropSpec> _specsBySeed;

        public IReadOnlyList<CropDefinition> Crops => crops;

        public bool TryGet(string cropId, out CropSpec spec)
        {
            EnsureIndex();
            return _specsById.TryGetValue(cropId ?? string.Empty, out spec);
        }

        public bool TryGetBySeed(string seedItemId, out CropSpec spec)
        {
            EnsureIndex();
            return _specsBySeed.TryGetValue(seedItemId ?? string.Empty, out spec);
        }

        public bool TryGetDefinition(string cropId, out CropDefinition definition)
        {
            EnsureIndex();
            return _definitionsById.TryGetValue(cropId ?? string.Empty, out definition);
        }

        private void OnEnable() => Invalidate();
        private void OnValidate() => Invalidate();

        private void Invalidate()
        {
            _definitionsById = null;
            _specsById = null;
            _specsBySeed = null;
        }

        private void EnsureIndex()
        {
            if (_specsById != null)
                return;

            _definitionsById = new Dictionary<string, CropDefinition>();
            _specsById = new Dictionary<string, CropSpec>();
            _specsBySeed = new Dictionary<string, CropSpec>();

            foreach (var crop in crops)
            {
                if (crop == null || crop.SeedItem == null || crop.HarvestItem == null)
                    continue;

                try
                {
                    var spec = crop.ToSpec();
                    if (!_specsById.TryAdd(spec.Id, spec))
                    {
                        Debug.LogError($"[CropDatabase] Id de cultura duplicado: '{spec.Id}'.", this);
                        continue;
                    }

                    _definitionsById[spec.Id] = crop;
                    _specsBySeed[spec.SeedItemId] = spec;
                }
                catch (System.ArgumentException exception)
                {
                    Debug.LogError($"[CropDatabase] Cultura '{crop.name}' inválida: {exception.Message}", crop);
                }
            }
        }
    }
}
