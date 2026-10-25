using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OdontoAR.Model;
using UnityEngine;

namespace OdontoAR.Logging
{
    /// <summary>
    /// Registro de uso (RF10), sem MonoBehaviour: sessões e eventos de um participante, identificado só
    /// pelo código. O relógio entra como parâmetro (segundos), para os testes.
    /// Armazenamento: JsonUtility. Exportação: JSON do design system (registro.md).
    /// </summary>
    public sealed class UsageLog
    {
        public const string AppName = "OdontoAR";

        /// <summary>
        /// Sessão sem nenhum evento e mais curta que isto é descartada: são as idas e voltas de foco do app
        /// (abrir, compartilhar), não uso de verdade.
        /// </summary>
        public const float MinEmptySessionSeconds = 5f;

        public UsageLogData Data { get; }
        public string Participant => Data.participante;
        public bool InSession => current != null;

        UsageSession current;
        double sessionStart;

        public UsageLog(string participant)
        {
            Data = new UsageLogData { participante = participant };
        }

        UsageLog(UsageLogData data)
        {
            Data = data;
            data.sessoes ??= new List<UsageSession>();

            // Sessão que ficou aberta: o app foi fechado sem encerrar. Vale a duração do último salvamento.
            foreach (var session in data.sessoes.Where(s => !s.encerrada))
            {
                session.encerrada = true;
                session.interrompida = true;
            }
            data.sessoes.RemoveAll(IsNoise);
        }

        /// <summary>Lê o registro salvo; JSON vazio ou inválido começa um registro novo.</summary>
        public static UsageLog FromStorageJson(string participant, string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<UsageLogData>(json);
                    if (data != null && data.participante == participant)
                        return new UsageLog(data);
                }
                catch (ArgumentException)
                {
                    // arquivo corrompido: começa de novo
                }
            }
            return new UsageLog(participant);
        }

        public string ToStorageJson() => JsonUtility.ToJson(Data);

        public void StartSession(DateTimeOffset now, double clock)
        {
            if (current != null)
                return;

            current = new UsageSession { inicio = FormatDate(now) };
            sessionStart = clock;
            Data.sessoes.Add(current);
        }

        public void Record(UsageEvent usageEvent, double clock)
        {
            if (current == null || usageEvent == null)
                return;

            usageEvent.t = Seconds(clock);
            current.eventos.Add(usageEvent);
            current.duracaoSeg = usageEvent.t;
        }

        /// <summary>Atualiza a duração da sessão aberta (antes de salvar).</summary>
        public void Touch(double clock)
        {
            if (current != null)
                current.duracaoSeg = Seconds(clock);
        }

        public void EndSession(double clock)
        {
            if (current == null)
                return;

            current.duracaoSeg = Seconds(clock);
            current.encerrada = true;
            if (IsNoise(current))
                Data.sessoes.Remove(current);
            current = null;
        }

        static bool IsNoise(UsageSession session) =>
            (session.eventos == null || session.eventos.Count == 0) && session.duracaoSeg < MinEmptySessionSeconds;

        float Seconds(double clock) => (float)Math.Round(Math.Max(0d, clock - sessionStart), 1);

        // ---------- Resumo (tela 10) ----------

        public int SessionCount => Data.sessoes.Count;

        public float TotalSeconds => Data.sessoes.Sum(s => s.duracaoSeg);

        public string LastSessionStart => Data.sessoes.Count > 0 ? Data.sessoes[^1].inicio : null;

        /// <summary>Quantas vezes cada camada foi ligada.</summary>
        public int Activations(Layer layer)
        {
            var key = UsageEvent.LayerKey(layer);
            return Data.sessoes.Sum(s => s.eventos.Count(e =>
                e.tipo == UsageEvent.LayerToggled && e.visivel && e.camada == key));
        }

        public int SelectionCount =>
            Data.sessoes.Sum(s => s.eventos.Count(e => e.tipo == UsageEvent.Selected));

        // ---------- Exportação ----------

        public static string ExportFileName(string participant, DateTimeOffset date) =>
            $"odontoar-{participant}-{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.json";

        public static string FormatDate(DateTimeOffset date) =>
            date.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

        static readonly Layer[] Layers = { Layer.Root, Layer.Crown, Layer.Bone, Layer.Nerves };

        /// <summary>JSON do design system: só os campos de cada tipo de evento, sem dados pessoais.</summary>
        public string ToExportJson(string appVersion)
        {
            var json = new StringBuilder();
            json.Append("{\n");
            json.Append("  \"app\": ").Append(Quote(AppName)).Append(", \"versao\": ").Append(Quote(appVersion))
                .Append(", \"participante\": ").Append(Quote(Participant)).Append(",\n");

            json.Append("  \"resumo\": { \"sessoes\": ").Append(SessionCount)
                .Append(", \"tempoTotalSeg\": ").Append(Mathf.RoundToInt(TotalSeconds))
                .Append(", \"selecoes\": ").Append(SelectionCount)
                .Append(",\n    \"camadas\": { ");
            json.Append(string.Join(", ", Layers.Select(l => $"{Quote(UsageEvent.LayerKey(l))}: {Activations(l)}")));
            json.Append(" } },\n");

            json.Append("  \"sessoes\": [");
            for (var i = 0; i < Data.sessoes.Count; i++)
            {
                var session = Data.sessoes[i];
                json.Append(i == 0 ? "\n" : ",\n");
                json.Append("    { \"inicio\": ").Append(Quote(session.inicio))
                    .Append(", \"duracaoSeg\": ").Append(Mathf.RoundToInt(session.duracaoSeg));
                if (session.interrompida)
                    json.Append(", \"interrompida\": true");
                json.Append(",\n      \"eventos\": [");
                for (var j = 0; j < session.eventos.Count; j++)
                {
                    json.Append(j == 0 ? "\n" : ",\n");
                    json.Append("        ").Append(EventJson(session.eventos[j]));
                }
                json.Append(session.eventos.Count > 0 ? "\n      ]\n    }" : "]\n    }");
            }
            json.Append(Data.sessoes.Count > 0 ? "\n  ]\n" : "]\n");
            json.Append("}\n");
            return json.ToString();
        }

        static string EventJson(UsageEvent e)
        {
            var fields = new List<string>
            {
                $"\"t\": {e.t.ToString("0.0", CultureInfo.InvariantCulture)}",
                $"\"tipo\": {Quote(e.tipo)}",
            };

            switch (e.tipo)
            {
                case UsageEvent.LayerToggled:
                    fields.Add($"\"camada\": {Quote(e.camada)}");
                    fields.Add($"\"visivel\": {(e.visivel ? "true" : "false")}");
                    break;
                case UsageEvent.Selected:
                    fields.Add(string.IsNullOrEmpty(e.estrutura) ? $"\"fdi\": {e.fdi}" : $"\"estrutura\": {Quote(e.estrutura)}");
                    fields.Add($"\"parte\": {Quote(e.parte)}");
                    break;
                case UsageEvent.OpeningChanged:
                    fields.Add($"\"graus\": {e.graus}");
                    break;
                case UsageEvent.ArchChanged:
                    fields.Add($"\"arcada\": {Quote(e.arcada)}");
                    break;
            }

            return "{ " + string.Join(", ", fields) + " }";
        }

        static string Quote(string value)
        {
            if (value == null)
                return "null";

            var s = new StringBuilder("\"");
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': s.Append("\\\""); break;
                    case '\\': s.Append("\\\\"); break;
                    case '\n': s.Append("\\n"); break;
                    case '\r': s.Append("\\r"); break;
                    case '\t': s.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            s.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            s.Append(c);
                        break;
                }
            }
            return s.Append('"').ToString();
        }
    }
}
