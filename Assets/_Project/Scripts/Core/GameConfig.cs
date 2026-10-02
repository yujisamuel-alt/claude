using UnityEngine;

namespace Enxada.Core
{
    /// <summary>
    /// Configuração global do jogo. Valores que não pertencem a nenhum sistema específico.
    /// </summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Tooltip("Cena carregada logo depois da Boot.")]
        [SerializeField] private string firstSceneName = "MainMenu";

        [Tooltip("Chave da tabela de Localization com o nome da moeda (Tostões). " +
                 "Trocar o nome da moeda = editar só essa entrada da tabela.")]
        [SerializeField] private string currencyNameKey = "currency.name";

        public string FirstSceneName => firstSceneName;
        public string CurrencyNameKey => currencyNameKey;
    }
}
