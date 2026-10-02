using System;
using System.Collections.Generic;

namespace Enxada.Core
{
    /// <summary>
    /// Pausa do mundo (relógio e controle do jogador). Diálogos, menus e cutscenes pedem pausa
    /// com Push(this) e liberam com Pop(this); o jogo só volta quando ninguém mais está pedindo.
    /// </summary>
    public sealed class GameplayPause
    {
        private readonly HashSet<object> _owners = new HashSet<object>();

        public bool IsPaused => _owners.Count > 0;
        public event Action Changed;

        public void Push(object owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (_owners.Add(owner))
                Changed?.Invoke();
        }

        public void Pop(object owner)
        {
            if (owner != null && _owners.Remove(owner))
                Changed?.Invoke();
        }
    }
}
