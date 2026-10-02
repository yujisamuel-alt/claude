using Enxada.Core;
using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>A cama: pergunta se o jogador quer dormir e, se sim, encerra o dia.</summary>
    public sealed class SleepInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private DayTransitionController transition;

        public void Interact(GameObject interactor)
        {
            if (transition == null || transition.IsBusy)
                return;

            var texts = ServiceLocator.Get<ITextProvider>();
            ServiceLocator.Get<IConfirmDialog>().Show(texts.Get("sleep.confirm"), transition.RequestSleep);
        }
    }
}
