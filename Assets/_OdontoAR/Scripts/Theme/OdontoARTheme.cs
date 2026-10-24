using UnityEngine;

namespace OdontoAR.Theme
{
    /// <summary>
    /// Tokens de cor e opacidade do design system. Único lugar do projeto com valores de cor.
    /// PROVISÓRIO: valores neutros até a T8 trazer os tokens do design system (Figma).
    /// </summary>
    public static class OdontoARTheme
    {
        // Superfícies sobre a câmera (dicas, barras).
        public static readonly Color SurfaceOverlay = new(0f, 0f, 0f, 0.55f);

        // Botões.
        public static readonly Color ButtonBackground = new(1f, 1f, 1f, 0.92f);
        public static readonly Color ButtonText = new(0.1f, 0.12f, 0.16f, 1f);

        // Texto sobre superfícies escuras.
        public static readonly Color TextOnOverlay = Color.white;

        // Planos detectados pelo ARKit (preenchimento translúcido).
        public static readonly Color PlaneFill = new(0.13f, 0.83f, 0.93f, 0.25f);
    }
}
