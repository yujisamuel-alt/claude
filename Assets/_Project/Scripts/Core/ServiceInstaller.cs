using UnityEngine;

namespace Enxada.Core
{
    /// <summary>
    /// Cada módulo cria um instalador que registra seus serviços no ServiceLocator.
    /// O GameBootstrap chama todos na ordem da lista, assim o Core não precisa conhecer os outros módulos.
    /// </summary>
    public abstract class ServiceInstaller : MonoBehaviour
    {
        public abstract void Install();
    }
}
