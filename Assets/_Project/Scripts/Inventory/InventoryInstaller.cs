using Enxada.Core;
using UnityEngine;

namespace Enxada.Inventory
{
    /// <summary>Cria o InventoryModel com os itens iniciais e registra inventário, catálogo e config.</summary>
    public sealed class InventoryInstaller : ServiceInstaller
    {
        [SerializeField] private InventoryConfig config;
        [SerializeField] private ItemDatabase database;

        public override void Install()
        {
            if (config == null || database == null)
            {
                Debug.LogError("[InventoryInstaller] Config ou ItemDatabase não atribuídos.", this);
                return;
            }

            var model = new InventoryModel(database, config.HotbarSize, config.BackpackSize);
            if (config.StartingItems != null)
            {
                foreach (var amount in config.StartingItems)
                {
                    var leftover = model.TryAdd(amount.ToStack());
                    if (!leftover.IsEmpty)
                        Debug.LogWarning($"[InventoryInstaller] Não coube no inventário inicial: {leftover}", this);
                }
            }

            ServiceLocator.Register(model);
            ServiceLocator.Register(database);
            ServiceLocator.Register(config);
        }
    }
}
