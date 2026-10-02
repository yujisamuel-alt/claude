using UnityEngine;
using UnityEngine.SceneManagement;

namespace Enxada.Core
{
    /// <summary>
    /// Vive na cena Boot. Registra os serviços globais, sobrevive às trocas de cena
    /// e carrega a primeira cena do jogo.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        [Tooltip("Instaladores de serviços de cada módulo, na ordem em que devem rodar.")]
        [SerializeField] private ServiceInstaller[] installers;

        [Tooltip("Desligue em cenas de teste que já contêm o seu próprio Bootstrap.")]
        [SerializeField] private bool loadFirstScene = true;

        private static bool _initialized;

        // Garante estado limpo mesmo com "Enter Play Mode Options" (sem domain reload).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _initialized = false;
            ServiceLocator.Clear();
        }

        private void Awake()
        {
            if (_initialized)
            {
                enabled = false; // evita que o Start desta cópia recarregue a cena
                Destroy(gameObject);
                return;
            }

            if (config == null)
            {
                Debug.LogError("[GameBootstrap] GameConfig não atribuído. Rode Enxada/Setup/Configurar Projeto.", this);
                enabled = false;
                return;
            }

            _initialized = true;
            DontDestroyOnLoad(gameObject);
            RegisterServices();
        }

        private void Start()
        {
            if (loadFirstScene && !string.IsNullOrEmpty(config.FirstSceneName))
                SceneManager.LoadScene(config.FirstSceneName);
        }

        private void RegisterServices()
        {
            ServiceLocator.Register(config);
            ServiceLocator.Register(new GameplayPause());

            if (installers == null)
                return;

            foreach (var installer in installers)
            {
                if (installer != null)
                    installer.Install();
            }
        }
    }
}
