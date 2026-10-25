using OdontoAR.AR;
using OdontoAR.Model;
using OdontoAR.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// CartaoEstrutura (RF09, tela 8): selo FDI, "Primeiro molar inferior esquerdo — 36", "Camada: Raiz"
    /// e "Quadrante 3 · Dente 6". Fica acima da estrutura (ou abaixo, sem espaço) e se liga a ela por uma
    /// linha ciano com um ponto de ancoragem. "Fechar" e o X limpam a seleção.
    /// </summary>
    public sealed class StructureCard : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] StructureCatalog catalog;
        [SerializeField] SelectionController selection;
        [SerializeField] Camera arCamera;

        [Header("Cartão")]
        [SerializeField] RectTransform card;
        [SerializeField] GameObject badge;
        [SerializeField] TextMeshProUGUI badgeNumber;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] Image layerDot;
        [SerializeField] TextMeshProUGUI layerText;
        [SerializeField] TextMeshProUGUI meta;
        [SerializeField] Button[] closeButtons;

        [Header("Ligação com a estrutura")]
        [SerializeField] RectTransform line;
        [SerializeField] RectTransform anchorDot;

        [Tooltip("Distância entre a estrutura e o cartão (pt).")]
        [SerializeField] float gap = 56f;

        [Tooltip("Faixa reservada no topo (barra superior e seletor) e na base (painel recolhido), em pt.")]
        [SerializeField] float topReserved = 170f;
        [SerializeField] float bottomReserved = 130f;
        [SerializeField] float sideMargin = 12f;

        RectTransform area;

        void Awake()
        {
            area = (RectTransform)card.parent;
            foreach (var button in closeButtons)
                button.onClick.AddListener(() => holder.State.ClearSelection());
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

        void SetVisible(bool visible)
        {
            card.gameObject.SetActive(visible);
            line.gameObject.SetActive(visible);
            anchorDot.gameObject.SetActive(visible);
        }

        void Refresh()
        {
            var state = holder.State;
            SetVisible(state.HasSelection);
            if (!state.HasSelection)
                return;

            var key = state.SelectedKey;
            var isTooth = int.TryParse(key, out var fdi);
            badge.SetActive(isTooth);
            meta.gameObject.SetActive(isTooth);
            if (isTooth)
            {
                badgeNumber.text = key;
                meta.text = $"Quadrante {FdiCode.Quadrant(fdi)} · Dente {FdiCode.ToothNumber(fdi)}";
            }

            title.text = catalog.Title(key);
            layerText.text = $"Camada: {catalog.LayerName(state.SelectedLayer)}";
            layerDot.color = LayerChip.ColorOf(state.SelectedLayer);
        }

        void LateUpdate()
        {
            if (!card.gameObject.activeSelf || !selection.TryGetSelectionBounds(out var bounds))
                return;

            if (!TryToLocal(bounds.center + Vector3.up * bounds.extents.y, out var top)
                || !TryToLocal(bounds.center - Vector3.up * bounds.extents.y, out var bottom))
                return; // atrás da câmera: mantém a última posição

            var rect = area.rect;
            var size = card.rect.size;
            var maxCenterY = rect.yMax - topReserved - size.y * 0.5f;
            var minCenterY = rect.yMin + bottomReserved + size.y * 0.5f;

            // Acima da estrutura; sem espaço, abaixo dela.
            var above = top.y + gap + size.y * 0.5f <= maxCenterY;
            var anchor = above ? top : bottom;
            var center = new Vector2(anchor.x, above ? top.y + gap + size.y * 0.5f : bottom.y - gap - size.y * 0.5f);
            center.x = Mathf.Clamp(center.x, rect.xMin + sideMargin + size.x * 0.5f, rect.xMax - sideMargin - size.x * 0.5f);
            center.y = Mathf.Clamp(center.y, minCenterY, Mathf.Max(minCenterY, maxCenterY));
            card.anchoredPosition = center;

            // Linha do ponto de ancoragem até a borda do cartão mais próxima.
            var edge = center + new Vector2(0f, (above ? -0.5f : 0.5f) * size.y);
            var delta = edge - anchor;
            anchorDot.anchoredPosition = anchor;
            line.anchoredPosition = anchor;
            line.sizeDelta = new Vector2(line.sizeDelta.x, delta.magnitude);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
        }

        bool TryToLocal(Vector3 world, out Vector2 local)
        {
            local = default;
            var screen = arCamera.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out local);
        }
    }
}
