using OdontoAR.Model;
using OdontoAR.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// Seletor de arcada (Ambas / Superior / Inferior) e slider de abertura, ligados ao <see cref="ModelState"/>.
    /// A abertura só faz sentido com as duas arcadas, então o slider fica desativado nos outros casos.
    /// </summary>
    public sealed class ArchControls : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] Button bothButton;
        [SerializeField] Button upperButton;
        [SerializeField] Button lowerButton;
        [SerializeField] Slider openingSlider;

        void Awake()
        {
            bothButton.onClick.AddListener(() => holder.State.SetArch(ArchSelection.Both));
            upperButton.onClick.AddListener(() => holder.State.SetArch(ArchSelection.Upper));
            lowerButton.onClick.AddListener(() => holder.State.SetArch(ArchSelection.Lower));
            openingSlider.onValueChanged.AddListener(value => holder.State.SetOpening(value));
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

            Highlight(bothButton, state.Arch == ArchSelection.Both);
            Highlight(upperButton, state.Arch == ArchSelection.Upper);
            Highlight(lowerButton, state.Arch == ArchSelection.Lower);

            openingSlider.SetValueWithoutNotify(state.Opening);
            openingSlider.interactable = state.Arch == ArchSelection.Both;
        }

        static void Highlight(Button button, bool selected)
        {
            button.image.color = selected ? OdontoARTheme.ButtonSelectedBackground : OdontoARTheme.ButtonBackground;

            // O rótulo (TMP) é um Graphic filho do botão.
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != button.image)
                    graphic.color = selected ? OdontoARTheme.ButtonSelectedText : OdontoARTheme.ButtonText;
            }
        }
    }
}
