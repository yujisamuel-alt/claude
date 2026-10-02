using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Enxada.Tests.PlayMode
{
    public class PlayModeSmokeTests
    {
        // Só confirma que o assembly de PlayMode compila e roda. Testes reais chegam na Etapa 1.
        [UnityTest]
        public IEnumerator PlayMode_CanAdvanceOneFrame()
        {
            yield return null;
            Assert.Pass();
        }
    }
}
