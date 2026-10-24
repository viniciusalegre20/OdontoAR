using OdontoAR.AR;
using OdontoAR.Model;
using TMPro;
using UnityEngine;

namespace OdontoAR.UI
{
    /// <summary>
    /// Cartão da estrutura selecionada (RF09): "Primeiro molar inferior esquerdo — 36" e "Camada: Raiz".
    /// Fica na tela logo acima da estrutura e a acompanha enquanto o aluno se move ou gira o modelo.
    /// </summary>
    public sealed class StructureCard : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] StructureCatalog catalog;
        [SerializeField] SelectionController selection;
        [SerializeField] Camera arCamera;
        [SerializeField] RectTransform card;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] TextMeshProUGUI layer;

        [Tooltip("Distância entre o topo da estrutura e a base do cartão (unidades da interface).")]
        [SerializeField] float gap = 40f;

        [Tooltip("Margem mínima até as bordas da tela (unidades da interface).")]
        [SerializeField] float margin = 24f;

        RectTransform area;

        void Awake()
        {
            area = (RectTransform)card.parent;
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
            card.gameObject.SetActive(state.HasSelection);
            if (!state.HasSelection)
                return;

            title.text = catalog.Title(state.SelectedKey);
            layer.text = $"Camada: {catalog.LayerName(state.SelectedLayer)}";
        }

        void LateUpdate()
        {
            if (!card.gameObject.activeSelf || !selection.TryGetSelectionBounds(out var bounds))
                return;

            var top = bounds.center + Vector3.up * bounds.extents.y;
            var screen = arCamera.WorldToScreenPoint(top);
            if (screen.z <= 0f)
                return; // atrás da câmera: mantém a última posição

            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var local);
            local.y += gap;

            // Mantém o cartão inteiro dentro da tela (pivô na base, centro).
            var rect = area.rect;
            var size = card.rect.size;
            local.x = Mathf.Clamp(local.x, rect.xMin + margin + size.x * 0.5f, rect.xMax - margin - size.x * 0.5f);
            local.y = Mathf.Clamp(local.y, rect.yMin + margin, rect.yMax - margin - size.y);
            card.anchoredPosition = local;
        }
    }
}
