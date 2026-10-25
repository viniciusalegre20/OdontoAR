using OdontoAR.Model;
using OdontoAR.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// CamadaChip (RF08): liga/desliga uma camada. O estado vem escrito ("Visível"/"Oculta"), não só na cor.
    /// Ligado: fundo e borda na cor da camada, amostra cheia com check. Desligado: vidro, anel esmaecido.
    /// </summary>
    public sealed class LayerChip : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] Layer layer;
        [SerializeField] Button button;
        [SerializeField] Image border;
        [SerializeField] Image swatch;
        [SerializeField] Image swatchRing;
        [SerializeField] Image check;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] TextMeshProUGUI status;

        void Awake()
        {
            button.onClick.AddListener(() => holder.State.ToggleLayer(layer));
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

        public static Color ColorOf(Layer layer) => layer switch
        {
            Layer.Crown => OdontoARTheme.ToothCrown,
            Layer.Root => OdontoARTheme.ToothRoot,
            Layer.Bone => OdontoARTheme.Bone,
            _ => OdontoARTheme.Nerve,
        };

        void Refresh()
        {
            var on = holder.State.IsLayerVisible(layer);
            var color = ColorOf(layer);

            button.image.color = on ? OdontoARTheme.WithAlpha(color, 0.18f) : OdontoARTheme.GlassFill;
            border.enabled = on;
            border.color = color;
            swatch.enabled = on;
            swatch.color = color;
            check.enabled = on;
            check.color = OdontoARTheme.Bg900;
            swatchRing.enabled = !on;
            swatchRing.color = OdontoARTheme.WithAlpha(color, 0.6f);
            label.color = on ? OdontoARTheme.Ink : OdontoARTheme.InkMuted;
            status.text = on ? "Visível" : "Oculta";
            status.color = on ? OdontoARTheme.InkMuted : OdontoARTheme.InkFaint;
        }
    }
}
