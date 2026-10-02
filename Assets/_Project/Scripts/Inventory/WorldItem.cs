using UnityEngine;

namespace Enxada.Inventory
{
    /// <summary>Um item físico no chão. Só guarda dados e o visual; quem o move e recolhe é o WorldItemSpawner.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WorldItem : MonoBehaviour
    {
        public ItemStack Stack;
        public float Age;
        public float PickupDelay;
        public Vector2 Velocity;

        private SpriteRenderer _renderer;

        public SpriteRenderer Renderer => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());
    }
}
