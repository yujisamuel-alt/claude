using System;
using UnityEngine;

namespace Enxada.Core
{
    /// <summary>
    /// Canal de evento em ScriptableObject: quem dispara e quem escuta só conhecem o asset,
    /// nunca um ao outro. Crie subclasses concretas (não genéricas) para cada tipo de dado.
    /// </summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        private event Action<T> Raised;

        public void Raise(T value) => Raised?.Invoke(value);
        public void Subscribe(Action<T> listener) => Raised += listener;
        public void Unsubscribe(Action<T> listener) => Raised -= listener;
    }
}
