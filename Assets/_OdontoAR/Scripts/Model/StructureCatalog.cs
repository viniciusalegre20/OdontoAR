using System;
using System.Collections.Generic;
using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// Nomes em português das estruturas selecionáveis (RF09). Chave = código FDI do dente ("36")
    /// ou nome do objeto no atlas ("Maxilla.l"); as camadas usam "camada:Crown" etc.
    /// Gerado pelo menu OdontoAR/Criar cena AR em Data/Estruturas.asset; pode ser revisado à mão.
    /// </summary>
    [CreateAssetMenu(menuName = "OdontoAR/Catálogo de estruturas", fileName = "Estruturas")]
    public sealed class StructureCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public string displayName;
        }

        [SerializeField] List<Entry> entries = new();

        Dictionary<string, string> lookup;

        public IReadOnlyList<Entry> Entries => entries;

        public static string LayerKey(Layer layer) => $"camada:{layer}";

        public void SetEntries(IEnumerable<Entry> values)
        {
            entries = new List<Entry>(values);
            lookup = null;
        }

        public bool TryGetName(string key, out string displayName)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, string>();
                foreach (var entry in entries)
                    lookup[entry.key] = entry.displayName;
            }

            displayName = null;
            return key != null && lookup.TryGetValue(key, out displayName);
        }

        /// <summary>Nome da estrutura; dentes ganham o código FDI ("Primeiro molar inferior esquerdo — 36").</summary>
        public string Title(string key)
        {
            if (!TryGetName(key, out var name))
                return key;
            return int.TryParse(key, out _) ? $"{name} — {key}" : name;
        }

        public string LayerName(Layer layer) =>
            TryGetName(LayerKey(layer), out var name) ? name : StructureNames.LayerName(layer);
    }
}
