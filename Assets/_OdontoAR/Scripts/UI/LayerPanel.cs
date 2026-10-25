using OdontoAR.Model;
using OdontoAR.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// PainelCamadas: painel inferior da RA com os chips de camada, a abertura (RF06) e as ações
    /// "Restaurar vista" e "Reposicionar". Recolhido, mostra só o título e uma bolinha por camada;
    /// recolhe sozinho enquanto o cartão de uma estrutura está aberto.
    /// </summary>
    public sealed class LayerPanel : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] GameObject panelRoot;
        [SerializeField] GameObject expanded;
        [SerializeField] GameObject collapsedDots;
        [SerializeField] Image[] dots;
        [SerializeField] Button toggleButton;
        [SerializeField] Image toggleIcon;
        [SerializeField] Sprite chevronDown;
        [SerializeField] Sprite chevronUp;
        [SerializeField] TextMeshProUGUI countLabel;
        [SerializeField] Slider openingSlider;
        [SerializeField] TextMeshProUGUI openingValue;
        [SerializeField] CanvasGroup openingGroup;

        static readonly Layer[] Layers = { Layer.Crown, Layer.Root, Layer.Bone, Layer.Nerves };

        bool userCollapsed;

        void Awake()
        {
            toggleButton.onClick.AddListener(() =>
            {
                var expand = IsCollapsed;
                userCollapsed = !expand;
                if (expand)
                    holder.State.ClearSelection(); // abrir o painel fecha o cartão
                Refresh();
            });
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

        bool IsCollapsed => userCollapsed || holder.State.HasSelection;

        void Refresh()
        {
            var state = holder.State;
            panelRoot.SetActive(state.IsPlaced);
            if (!state.IsPlaced)
                return;

            var collapsed = IsCollapsed;
            expanded.SetActive(!collapsed);
            collapsedDots.SetActive(collapsed);
            toggleIcon.sprite = collapsed ? chevronUp : chevronDown;

            var visible = 0;
            for (var i = 0; i < Layers.Length; i++)
            {
                var on = state.IsLayerVisible(Layers[i]);
                if (on)
                    visible++;
                dots[i].color = OdontoARTheme.WithAlpha(LayerChip.ColorOf(Layers[i]), on ? 1f : 0.3f);
            }
            countLabel.text = $"{visible} de 4 visíveis";

            openingSlider.SetValueWithoutNotify(state.Opening);
            openingValue.text = $"{Mathf.RoundToInt(state.Opening * ModelState.MaxOpeningDegrees)}°";
            var bothArches = state.Arch == ArchSelection.Both;
            openingSlider.interactable = bothArches;
            openingGroup.alpha = bothArches ? 1f : 0.4f;
        }
    }
}
