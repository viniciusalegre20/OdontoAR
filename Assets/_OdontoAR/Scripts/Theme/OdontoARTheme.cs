using UnityEngine;

namespace OdontoAR.Theme
{
    /// <summary>
    /// Tokens do design system do OdontoAR (artefato "OdontoAR" do tipo Design System, tokens.json).
    /// Único lugar do projeto com valores de cor; medidas em pt (o Canvas usa 440 × 956 pt).
    /// </summary>
    public static class OdontoARTheme
    {
        // ---------- Superfícies fora da RA ----------
        public static readonly Color Bg900 = Hex(0x081519);   // fundo base das telas sem câmera
        public static readonly Color Bg800 = Hex(0x0e2228);   // campos, células
        public static readonly Color Bg700 = Hex(0x15313a);   // cartões, folhas modais
        public static readonly Color Line = Hex(0x24464f);    // divisores e contornos de campo

        // ---------- Texto ----------
        public static readonly Color Ink = Hex(0xf1f7f8);
        public static readonly Color InkMuted = Hex(0x9cb3b9);
        public static readonly Color InkFaint = Hex(0x6f8a91);

        // ---------- Ação ----------
        public static readonly Color Accent = Hex(0x3fd0e6);        // ciano clínico: ação, foco, seleção
        public static readonly Color AccentStrong = Hex(0x1fb3cc);  // primário pressionado
        public static readonly Color AccentSoft = new(63f / 255f, 208f / 255f, 230f / 255f, 0.16f);
        public static readonly Color OnAccent = Hex(0x04262d);
        public static readonly Color Danger = Hex(0xff7b7b);

        // ---------- Vidro sobre a câmera ----------
        public static readonly Color Glass = new(10f / 255f, 28f / 255f, 34f / 255f, 0.62f);
        public static readonly Color GlassLine = new(1f, 1f, 1f, 0.14f);
        public static readonly Color GlassFill = new(1f, 1f, 1f, 0.08f);
        public static readonly Color SegmentActive = new(1f, 1f, 1f, 0.92f);
        public static readonly Color Scrim = new(0f, 0f, 0f, 0.45f);    // atrás das folhas modais
        public static readonly Color LogoHalo = new(63f / 255f, 208f / 255f, 230f / 255f, 0.45f);

        // ---------- Camadas (iguais no modelo, nos chips e na legenda) ----------
        public static readonly Color ToothCrown = Hex(0xf4ecdb); // camada-coroa: branco marfim
        public static readonly Color ToothRoot = Hex(0xf0b23a);  // camada-raiz: amarelo âmbar
        public static readonly Color Bone = Hex(0xe2cca6);       // camada-osso: bege
        // O design system tem polpa no lugar dos nervos; a POC usa nervos, em azul (pedido do projeto),
        // diferente do ciano de seleção.
        public static readonly Color Nerve = Hex(0x2e73eb);

        // ---------- Modelo 3D ----------
        public const float BoneOpacity = 0.42f;             // opacity-osso
        public const float IntegratedCrownOpacity = 0.55f;  // opacity-coroa-xray
        public const float IntegratedRootOpacity = 0.70f;   // opacity-raiz-xray
        public const float DimmedOpacity = 0.2f;            // opacity-dim
        public static readonly Color Hidden = new(0f, 0f, 0f, 0f);
        public static readonly Color SelectionOutline = Accent;
        public const float SelectionOutlineWidth = 0.0008f; // m
        public static readonly Color PlacementReticle = Color.white;
        public static readonly Color PlacementReticleInner = Accent;
        public static readonly Color ContactShadow = new(0f, 0f, 0f, 0.45f);

        public const float ToothSmoothness = 0.6f;
        public const float BoneSmoothness = 0.25f;
        public const float NerveSmoothness = 0.4f;

        // ---------- Espaço (pt) ----------
        public const float Space1 = 4f;
        public const float Space2 = 8f;
        public const float Space3 = 12f;
        public const float Space4 = 16f;
        public const float Space6 = 24f;
        public const float Space8 = 32f;

        // ---------- Raios (pt) ----------
        public const float RadiusSm = 10f;
        public const float RadiusMd = 14f;
        public const float RadiusLg = 20f;
        public const float RadiusXl = 28f;

        // ---------- Medidas (pt) ----------
        public const float SizeTouch = 44f;
        public const float SizeButton = 54f;
        public const float SizeField = 64f;
        public const float SizeChip = 72f;

        // ---------- Tipografia (pt) ----------
        public const float LargeTitle = 34f;
        public const float Title2 = 22f;
        public const float Headline = 17f;
        public const float Body = 17f;
        public const float Subheadline = 15f;
        public const float Footnote = 13f;
        public const float Caption = 12f;
        public const float CodeLarge = 26f;
        public const float Fdi = 34f;

        static Color Hex(int rgb) =>
            new(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, 1f);

        /// <summary>A mesma cor com outra opacidade.</summary>
        public static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, alpha);
    }
}
