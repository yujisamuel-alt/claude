using System;
using System.Collections.Generic;
using System.Globalization;

namespace Enxada.Core
{
    /// <summary>
    /// Tabela chave=valor lida de texto simples. Linhas vazias e iniciadas por # são ignoradas.
    /// Dentro do valor, \n vira quebra de linha e \\ vira uma barra.
    /// </summary>
    public sealed class StringTable
    {
        private readonly Dictionary<string, string> _entries;

        private StringTable(Dictionary<string, string> entries) => _entries = entries;

        public int Count => _entries.Count;

        public static StringTable Parse(string text)
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(text))
                return new StringTable(entries);

            text = text.TrimStart('﻿');
            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = line.Substring(0, separator).Trim();
                if (key.Length == 0)
                    continue;

                entries[key] = Unescape(line.Substring(separator + 1).Trim());
            }

            return new StringTable(entries);
        }

        public bool TryGet(string key, out string value) => _entries.TryGetValue(key ?? string.Empty, out value);

        private static string Unescape(string value)
        {
            if (value.IndexOf('\\') < 0)
                return value;

            var result = new System.Text.StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '\\' && i + 1 < value.Length)
                {
                    var next = value[i + 1];
                    if (next == 'n') { result.Append('\n'); i++; continue; }
                    if (next == '\\') { result.Append('\\'); i++; continue; }
                }

                result.Append(value[i]);
            }

            return result.ToString();
        }
    }

    /// <summary>Provedor de textos baseado em uma StringTable. Chave ausente aparece como [chave].</summary>
    public sealed class StringTableTextProvider : ITextProvider
    {
        private readonly StringTable _table;

        public StringTableTextProvider(StringTable table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
        }

        public string Get(string key) => _table.TryGet(key, out var value) ? value : "[" + key + "]";

        public string Format(string key, params object[] args)
        {
            var template = Get(key);
            if (args == null || args.Length == 0)
                return template;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }
    }
}
