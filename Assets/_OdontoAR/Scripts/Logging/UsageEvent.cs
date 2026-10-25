using System;
using System.Collections.Generic;
using OdontoAR.Model;

namespace OdontoAR.Logging
{
    /// <summary>
    /// Um evento de uso (RF10). Guardado com todos os campos (JsonUtility); na exportação só saem os campos
    /// do tipo do evento, no formato do design system (registro.md).
    /// </summary>
    [Serializable]
    public sealed class UsageEvent
    {
        public const string SurfaceDetected = "superficie_detectada";
        public const string Placed = "arcada_posicionada";
        public const string Repositioned = "reposicionar";
        public const string LayerToggled = "camada";
        public const string Selected = "selecao";
        public const string OpeningChanged = "abertura";
        public const string ArchChanged = "arcada";

        /// <summary>Segundos desde o início da sessão.</summary>
        public float t;
        public string tipo;

        public string camada;
        public bool visivel;
        public int fdi;
        public string estrutura;
        public string parte;
        public int graus;
        public string arcada;

        public static UsageEvent Simple(string type) => new() { tipo = type };

        public static UsageEvent LayerToggle(Layer layer, bool visible) =>
            new() { tipo = LayerToggled, camada = LayerKey(layer), visivel = visible };

        /// <summary>Dente: "fdi" e "parte" (coroa/raiz). Demais estruturas: "estrutura" (chave do catálogo) e "parte".</summary>
        public static UsageEvent Selection(string key, Layer layer)
        {
            var e = new UsageEvent { tipo = Selected, parte = LayerKey(layer) };
            if (int.TryParse(key, out var code))
                e.fdi = code;
            else
                e.estrutura = key;
            return e;
        }

        public static UsageEvent Opening(int degrees) => new() { tipo = OpeningChanged, graus = degrees };

        public static UsageEvent Arch(ArchSelection arch) => new()
        {
            tipo = ArchChanged,
            arcada = arch switch
            {
                ArchSelection.Upper => "superior",
                ArchSelection.Lower => "inferior",
                _ => "ambas",
            },
        };

        public static string LayerKey(Layer layer) => layer switch
        {
            Layer.Crown => "coroa",
            Layer.Root => "raiz",
            Layer.Bone => "osso",
            _ => "nervos",
        };
    }

    [Serializable]
    public sealed class UsageSession
    {
        /// <summary>Início em ISO 8601 com fuso ("2026-10-24T14:32:05-03:00").</summary>
        public string inicio;
        public float duracaoSeg;
        public bool encerrada;
        /// <summary>O app foi fechado sem encerrar a sessão; a duração é a do último registro salvo.</summary>
        public bool interrompida;
        public List<UsageEvent> eventos = new();
    }

    [Serializable]
    public sealed class UsageLogData
    {
        public string participante;
        public List<UsageSession> sessoes = new();
    }
}
