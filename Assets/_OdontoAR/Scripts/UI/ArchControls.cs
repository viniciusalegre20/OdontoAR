using OdontoAR.Model;
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

            ButtonStyle.SetSelected(bothButton, state.Arch == ArchSelection.Both);
            ButtonStyle.SetSelected(upperButton, state.Arch == ArchSelection.Upper);
            ButtonStyle.SetSelected(lowerButton, state.Arch == ArchSelection.Lower);

            openingSlider.SetValueWithoutNotify(state.Opening);
            openingSlider.interactable = state.Arch == ArchSelection.Both;
        }
    }
}
