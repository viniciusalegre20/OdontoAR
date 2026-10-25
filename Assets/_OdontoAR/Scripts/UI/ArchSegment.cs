using OdontoAR.Model;
using OdontoAR.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// Seletor segmentado (RF03): Superior / Inferior / Ambas, logo abaixo da barra superior.
    /// Aparece só com a arcada posicionada; o segmento ativo fica em branco com texto escuro.
    /// </summary>
    public sealed class ArchSegment : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] GameObject segmentRoot;
        [SerializeField] Button upperButton;
        [SerializeField] Button lowerButton;
        [SerializeField] Button bothButton;

        void Awake()
        {
            upperButton.onClick.AddListener(() => holder.State.SetArch(ArchSelection.Upper));
            lowerButton.onClick.AddListener(() => holder.State.SetArch(ArchSelection.Lower));
            bothButton.onClick.AddListener(() => holder.State.SetArch(ArchSelection.Both));
        }

        void OnEnable()
        {
            holder.State.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            holder.State.Changed -= Refresh;
        }

        void Refresh()
        {
            var state = holder.State;
            segmentRoot.SetActive(state.IsPlaced);
            Style(upperButton, state.Arch == ArchSelection.Upper);
            Style(lowerButton, state.Arch == ArchSelection.Lower);
            Style(bothButton, state.Arch == ArchSelection.Both);
        }

        static void Style(Button button, bool active)
        {
            button.image.color = active ? OdontoARTheme.SegmentActive : OdontoARTheme.WithAlpha(OdontoARTheme.SegmentActive, 0f);
            button.GetComponentInChildren<TextMeshProUGUI>(true).color = active ? OdontoARTheme.Bg900 : OdontoARTheme.Ink;
        }
    }
}
