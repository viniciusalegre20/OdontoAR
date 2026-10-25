using System.IO;
using OdontoAR.Theme;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Identidade do app no iPhone, a partir do símbolo oficial do design system (UI/Logo/Simbolo.png):
    /// ícone (símbolo sobre branco, como o design system pede para o ícone; o iOS não aceita transparência),
    /// tela de abertura do iOS (símbolo com halo ciano sobre bg-900) e sem a splash "Made with Unity".
    /// Chamado por OdontoAR/Configurar projeto iOS.
    /// </summary>
    public static class AppBranding
    {
        const string LogoFolder = "Assets/_OdontoAR/UI/Logo";
        const string SymbolPath = LogoFolder + "/Simbolo.png";
        const string IconPath = LogoFolder + "/IconeApp.png";
        const string LaunchPath = LogoFolder + "/TelaAbertura.png";

        const int IconSize = 1024;
        const float IconSymbolScale = 0.86f; // o PNG do símbolo já tem respiro em volta
        const int LaunchSize = 768;
        const float LaunchSymbolScale = 0.6f;
        const float LaunchFillPercent = 45f; // largura da imagem na tela de abertura

        public static bool Apply()
        {
            if (!File.Exists(SymbolPath))
            {
                Debug.LogError($"[OdontoAR] Símbolo não encontrado: {SymbolPath}");
                return false;
            }

            var symbol = new Texture2D(2, 2);
            symbol.LoadImage(File.ReadAllBytes(SymbolPath));

            var icon = Compose(symbol, IconSize, IconSymbolScale, Color.white, halo: false);
            var launch = Compose(symbol, LaunchSize, LaunchSymbolScale, Color.clear, halo: true);
            Object.DestroyImmediate(symbol);

            Save(icon, IconPath, alpha: false);
            Save(launch, LaunchPath, alpha: true);

            // Ícone padrão: vale para o iOS, que gera os tamanhos a partir dele.
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,
                new[] { AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath) }, IconKind.Any);

            // Sem a splash do Unity (opcional no Unity 6, inclusive na licença Personal).
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.ImageAndBackgroundRelative);
            PlayerSettings.iOS.SetLaunchScreenImage(AssetDatabase.LoadAssetAtPath<Texture2D>(LaunchPath),
                iOSLaunchScreenImageType.iPhonePortraitImage);
            ConfigureLaunchBackground();

            Debug.Log("[OdontoAR] Ícone e tela de abertura com o símbolo do OdontoAR; splash do Unity desligada.");
            return true;
        }

        /// <summary>Cor de fundo e tamanho da imagem da tela de abertura: sem API pública, só serializadas.</summary>
        static void ConfigureLaunchBackground()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length == 0)
                return;

            var serialized = new SerializedObject(settings[0]);
            var color = serialized.FindProperty("iOSLaunchScreenBackgroundColor");
            if (color != null)
                color.colorValue = OdontoARTheme.Bg900;
            var fill = serialized.FindProperty("iOSLaunchScreenFillPct");
            if (fill != null)
                fill.floatValue = LaunchFillPercent;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Símbolo centralizado num quadrado, opcionalmente com o halo ciano do design system.</summary>
        static Texture2D Compose(Texture2D symbol, int size, float symbolScale, Color background, bool halo)
        {
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var symbolSize = size * symbolScale;
            var origin = center - Vector2.one * (symbolSize * 0.5f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var color = background;

                if (halo)
                {
                    var d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    var glow = Mathf.Clamp01(1f - d);
                    color = Over(OdontoARTheme.WithAlpha(OdontoARTheme.LogoHalo, OdontoARTheme.LogoHalo.a * glow * glow), color);
                }

                var u = (x + 0.5f - origin.x) / symbolSize;
                var v = (y + 0.5f - origin.y) / symbolSize;
                if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                    color = Over(symbol.GetPixelBilinear(u, v), color);

                pixels[y * size + x] = color;
            }

            output.SetPixels(pixels);
            output.Apply();
            return output;
        }

        /// <summary>Composição "source over" com alfa.</summary>
        static Color Over(Color top, Color bottom)
        {
            var a = top.a + bottom.a * (1f - top.a);
            if (a <= 0f)
                return Color.clear;
            var rgb = ((Vector4)top * top.a + (Vector4)bottom * bottom.a * (1f - top.a)) / a;
            return new Color(rgb.x, rgb.y, rgb.z, a);
        }

        static void Save(Texture2D texture, string path, bool alpha)
        {
            if (!alpha)
            {
                var pixels = texture.GetPixels();
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i].a = 1f;
                texture.SetPixels(pixels);
                texture.Apply();
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alpha;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
