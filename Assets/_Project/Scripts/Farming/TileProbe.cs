using Enxada.Core;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Consulta física de um tile: há algo ocupando o lugar (mato, pedra, poço...)?</summary>
    public sealed class TileProbe
    {
        private readonly Collider2D[] _buffer = new Collider2D[8];
        private ContactFilter2D _filter;

        public TileProbe()
        {
            // Sem filtro de camada, e incluindo triggers: o mato é "trigger" (dá para andar sobre ele), mas ocupa o tile.
            _filter = new ContactFilter2D { useTriggers = true };
        }

        /// <summary>Verdadeiro se algum collider, que não seja o do jogador, cobre o centro do tile.</summary>
        public bool IsBlocked(Vector2 cellCenter)
        {
            var count = Physics2D.OverlapPoint(cellCenter, _filter, _buffer);
            for (var i = 0; i < count; i++)
            {
                if (_buffer[i].GetComponentInParent<IPlayerAnchor>() == null)
                    return true;
            }

            return false;
        }
    }
}
