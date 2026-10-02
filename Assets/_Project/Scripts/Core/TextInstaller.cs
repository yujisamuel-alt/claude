using UnityEngine;

namespace Enxada.Core
{
    /// <summary>Registra o ITextProvider com a tabela PT-BR.</summary>
    public sealed class TextInstaller : ServiceInstaller
    {
        [SerializeField] private TextAsset portugueseTable;

        public override void Install()
        {
            if (portugueseTable == null)
                Debug.LogError("[TextInstaller] Tabela PT-BR não atribuída.", this);

            var table = StringTable.Parse(portugueseTable != null ? portugueseTable.text : string.Empty);
            ServiceLocator.Register<ITextProvider>(new StringTableTextProvider(table));
        }
    }
}
