using System;
using System.Linq;
using OdontoAR.App;
using OdontoAR.Logging;
using OdontoAR.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// Tela 10 (Registro de uso, RF10): código, última sessão, tempo total, sessões e as camadas mais
    /// ativadas, com "Exportar registro (JSON)" pela folha de compartilhamento do iOS.
    /// </summary>
    public sealed class UsageLogScreen : MonoBehaviour
    {
        [SerializeField] UsageRecorder recorder;
        [SerializeField] TextMeshProUGUI code;
        [SerializeField] TextMeshProUGUI lastSession;
        [SerializeField] TextMeshProUGUI totalTime;
        [SerializeField] TextMeshProUGUI sessions;

        [Header("Barras, de cima para baixo (reordenadas pela contagem)")]
        [SerializeField] TextMeshProUGUI[] barLabels;
        [SerializeField] RectTransform[] barFills;
        [SerializeField] Image[] barImages;
        [SerializeField] TextMeshProUGUI[] barCounts;

        [SerializeField] Button exportButton;

        static readonly Layer[] Layers = { Layer.Crown, Layer.Root, Layer.Bone, Layer.Nerves };

        void Awake()
        {
            exportButton.onClick.AddListener(() => ShareSheet.ShareFile(recorder.WriteExport()));
        }

        void OnEnable()
        {
            var log = recorder.Log;
            code.text = log.Participant;
            lastSession.text = UsageFormat.LastSession(log.LastSessionStart, DateTimeOffset.Now);
            totalTime.text = UsageFormat.Duration(log.TotalSeconds);
            sessions.text = log.SessionCount.ToString();

            var counts = Layers.Select(l => (layer: l, count: log.Activations(l)))
                .OrderByDescending(c => c.count)
                .ToArray();
            var max = Mathf.Max(1, counts.Max(c => c.count));
            for (var i = 0; i < counts.Length; i++)
            {
                barLabels[i].text = StructureNames.LayerName(counts[i].layer);
                barCounts[i].text = counts[i].count.ToString();
                barImages[i].color = LayerChip.ColorOf(counts[i].layer);
                barFills[i].anchorMax = new Vector2(counts[i].count / (float)max, 1f);
            }
        }
    }
}
