using UnityEngine;

namespace Enxada.Inventory
{
    /// <summary>Tamanhos do inventário, itens iniciais e comportamento dos itens no chão.</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Inventory Config", fileName = "InventoryConfig")]
    public sealed class InventoryConfig : ScriptableObject
    {
        [Header("Tamanho")]
        [Min(1)] [SerializeField] private int hotbarSize = InventoryModel.DefaultHotbarSize;
        [Min(0)] [SerializeField] private int backpackSize = InventoryModel.DefaultBackpackSize;

        [Header("Itens ao começar um jogo novo")]
        [SerializeField] private ItemAmount[] startingItems;

        [Header("Itens no chão")]
        [Tooltip("Distância (em tiles) em que o jogador começa a atrair o item.")]
        [Min(0.1f)] [SerializeField] private float attractRadius = 1.75f;

        [Tooltip("Distância em que o item é recolhido.")]
        [Min(0.05f)] [SerializeField] private float collectRadius = 0.4f;

        [SerializeField] private float minAttractSpeed = 3f;
        [SerializeField] private float maxAttractSpeed = 12f;

        [Tooltip("Segundos até um item descartado poder ser pego de novo.")]
        [Min(0f)] [SerializeField] private float dropPickupDelay = 1.5f;

        [Tooltip("Velocidade inicial do arremesso ao descartar (tiles por segundo).")]
        [Min(0f)] [SerializeField] private float tossSpeed = 4f;

        [Tooltip("Quanto o arremesso desacelera (tiles por segundo ao quadrado).")]
        [Min(0.1f)] [SerializeField] private float tossFriction = 14f;

        public int HotbarSize => hotbarSize;
        public int BackpackSize => backpackSize;
        public ItemAmount[] StartingItems => startingItems;
        public float AttractRadius => attractRadius;
        public float CollectRadius => collectRadius;
        public float MinAttractSpeed => minAttractSpeed;
        public float MaxAttractSpeed => maxAttractSpeed;
        public float DropPickupDelay => dropPickupDelay;
        public float TossSpeed => tossSpeed;
        public float TossFriction => tossFriction;
    }
}
