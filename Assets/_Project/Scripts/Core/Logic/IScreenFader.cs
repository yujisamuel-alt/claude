using System.Collections;

namespace Enxada.Core
{
    /// <summary>Escurece e clareia a tela. Usado nas transições de dia e (depois) de cena.</summary>
    public interface IScreenFader
    {
        IEnumerator FadeOut();
        IEnumerator FadeIn();
    }
}
