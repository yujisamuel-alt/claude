using System;

namespace Enxada.Core
{
    /// <summary>Caixa "Sim / Não". Pausa o mundo enquanto está aberta.</summary>
    public interface IConfirmDialog
    {
        void Show(string question, Action onYes, Action onNo = null);
    }
}
