namespace Enxada.Core
{
    public enum ToolUseKind
    {
        /// <summary>A ferramenta não fez nada aqui (nenhuma energia é gasta).</summary>
        None,

        /// <summary>Funcionou e gasta energia.</summary>
        Used,

        /// <summary>Funcionou sem gastar energia (ex.: encher o regador).</summary>
        UsedFree,

        /// <summary>Não deu, e o jogador merece saber por quê (MessageKey).</summary>
        Rejected
    }

    /// <summary>Resultado de usar uma ferramenta em um alvo.</summary>
    public readonly struct ToolUseOutcome
    {
        public readonly ToolUseKind Kind;
        public readonly string MessageKey;

        private ToolUseOutcome(ToolUseKind kind, string messageKey)
        {
            Kind = kind;
            MessageKey = messageKey;
        }

        public static ToolUseOutcome None => new ToolUseOutcome(ToolUseKind.None, null);
        public static ToolUseOutcome Used => new ToolUseOutcome(ToolUseKind.Used, null);
        public static ToolUseOutcome UsedFree => new ToolUseOutcome(ToolUseKind.UsedFree, null);
        public static ToolUseOutcome Rejected(string messageKey) => new ToolUseOutcome(ToolUseKind.Rejected, messageKey);

        public bool WasHandled => Kind != ToolUseKind.None;
    }
}
