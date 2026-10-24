using OdontoAR.Theme;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>Destaque dos botões que funcionam como seleção ou liga/desliga.</summary>
    public static class ButtonStyle
    {
        public static void SetSelected(Button button, bool selected)
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
