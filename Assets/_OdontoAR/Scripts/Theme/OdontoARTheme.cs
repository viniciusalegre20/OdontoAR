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

        // Mira de posicionamento sobre a mesa.
        public static readonly Color PlacementReticle = new(1f, 1f, 1f, 0.9f);

        // Sombra de contato sob a arcada (o alfa é a opacidade no centro).
        public static readonly Color ContactShadow = new(0f, 0f, 0f, 0.45f);

        // Estruturas anatômicas.
        public static readonly Color ToothCrown = new(0.96f, 0.95f, 0.90f, 1f); // esmalte: marfim
        public static readonly Color ToothRoot = new(0.91f, 0.82f, 0.62f, 1f);  // raiz e dentina: amarelada
        public static readonly Color Bone = new(0.86f, 0.78f, 0.64f, 1f);       // maxila e mandíbula: bege
        public static readonly Color Nerve = new(0.18f, 0.45f, 0.92f, 1f);      // nervos: azul

        // Acabamento das estruturas (URP Lit: 0 = fosco, 1 = espelhado).
        public const float ToothSmoothness = 0.6f;
        public const float BoneSmoothness = 0.25f;
        public const float NerveSmoothness = 0.4f;
    }
}
