namespace Enxada.Core
{
    /// <summary>
    /// Fonte de textos para o jogador. Nenhum texto visível fica no código: tudo vem de uma chave.
    /// Hoje a implementação lê uma tabela PT-BR; na Etapa 10 outra implementação (Unity Localization)
    /// entra no lugar sem mudar quem usa a interface.
    /// </summary>
    public interface ITextProvider
    {
        string Get(string key);
        string Format(string key, params object[] args);
    }
}
