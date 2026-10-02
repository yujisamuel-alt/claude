using System;
using UnityEngine;

namespace Enxada.Core
{
    /// <summary>Canal de evento sem dados (ex.: "dia terminou", "jogo salvo").</summary>
    [CreateAssetMenu(menuName = "Enxada/Events/Void Event Channel", fileName = "NewVoidEvent")]
    public sealed class VoidEventChannel : ScriptableObject
    {
        private event Action Raised;

        public void Raise() => Raised?.Invoke();
        public void Subscribe(Action listener) => Raised += listener;
        public void Unsubscribe(Action listener) => Raised -= listener;
    }
}
