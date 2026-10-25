using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Recursos da interface (T8): sprites arredondados gerados aqui (9-slice, 4 px por pt), ícones e
    /// logo do design system importados como sprite e as fontes TMP (Inter e JetBrains Mono, OFL).
    /// O design system pede SF Pro, que não pode ser embutida: Inter é a alternativa que ele mesmo lista.
    /// </summary>
    public sealed class UIAssets
    {
        const string UIFolder = "Assets/_OdontoAR/UI";
        const string SpritesFolder = UIFolder + "/Sprites";
        const string FontsFolder = UIFolder + "/Fonts";
        const string IconsFolder = UIFolder + "/Icons";
        const string LogoPath = UIFolder + "/Logo/Simbolo.png";

        /// <summary>
        /// Pixels de textura por pt. Os sprites gerados são importados com 100 × isso pixels por unidade
        /// (o Canvas usa 100): a borda de 9-slice e o tamanho mínimo que a Image informa ao layout já
        /// saem em pt. Com pixelsPerUnitMultiplier o layout continuaria vendo o tamanho em pixels.
        /// </summary>
        public const float PixelsPerPoint = 4f;

        // Português e símbolos usados na interface; o resto entra no atlas dinâmico se aparecer.
        const string Characters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            "ÁÀÂÃÉÊÍÓÔÕÚÇáàâãéêíóôõúüç .,;:!?¿¡'\"()[]{}<>/\\|-–—_+=*&%$#@°·•…›‹©®";

        public TMP_FontAsset Regular { get; private set; }
        public TMP_FontAsset Medium { get; private set; }
        public TMP_FontAsset SemiBold { get; private set; }
        public TMP_FontAsset Bold { get; private set; }
        public TMP_FontAsset Mono { get; private set; }
        public Sprite Logo { get; private set; }
        public Sprite Glow { get; private set; }
        public Sprite Circle { get; private set; }

        readonly Dictionary<string, Sprite> rounded = new();
        readonly Dictionary<string, Sprite> icons = new();

        public static UIAssets Load()
        {
            EnsureFolder(SpritesFolder);
            var assets = new UIAssets
            {
                Regular = Font("Inter-Regular"),
                Medium = Font("Inter-Medium"),
                SemiBold = Font("Inter-SemiBold"),
                Bold = Font("Inter-Bold"),
                Mono = Font("JetBrainsMono-Bold"),
                Logo = ImportSprite(LogoPath, Vector4.zero, mipmaps: true),
            };
            assets.Glow = assets.Generate("Halo", 256, 256, 0, (x, y) =>
            {
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(128f, 128f)) / 128f;
                var a = Mathf.Clamp01(1f - d);
                return a * a;
            });
            assets.Circle = assets.Generate("Circulo", 128, 128, 0, (x, y) =>
                Coverage(64f - Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f))));
            return assets;
        }

        /// <summary>Retângulo arredondado preenchido (9-slice) com raio em pt.</summary>
        public Sprite Rounded(float radius) => RoundedSprite(radius, 0f);

        /// <summary>Só o contorno de um retângulo arredondado, para bordas de 1–2 pt.</summary>
        public Sprite Stroke(float radius, float width) => RoundedSprite(radius, width);

        /// <summary>Anel (contorno de círculo) para a amostra de camada desligada.</summary>
        public Sprite Ring(float diameter, float width)
        {
            var px = Mathf.RoundToInt(diameter * PixelsPerPoint);
            var r = px * 0.5f;
            var w = width * PixelsPerPoint;
            return Generate($"Anel_{diameter}_{width}", px, px, 0, (x, y) =>
            {
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                return Mathf.Min(Coverage(r - d), Coverage(d - (r - w)));
            });
        }

        /// <summary>Anel que esmaece ao longo da volta: o indicador giratório de "Procurando superfície…".</summary>
        public Sprite Spinner(float diameter, float width)
        {
            var px = Mathf.RoundToInt(diameter * PixelsPerPoint);
            var r = px * 0.5f;
            var w = width * PixelsPerPoint;
            return Generate($"Indicador_{diameter}_{width}", px, px, 0, (x, y) =>
            {
                var offset = new Vector2(x - r, y - r);
                var d = offset.magnitude;
                var ring = Mathf.Min(Coverage(r - d), Coverage(d - (r - w)));
                var turn = Mathf.Repeat(Mathf.Atan2(offset.y, offset.x) / (2f * Mathf.PI), 1f);
                return ring * turn;
            });
        }

        public Sprite Icon(string name)
        {
            if (icons.TryGetValue(name, out var sprite))
                return sprite;

            sprite = ImportSprite($"{IconsFolder}/{name}.png", Vector4.zero, mipmaps: true);
            if (sprite == null)
                Debug.LogError($"[OdontoAR] Ícone não encontrado: {name}");
            icons[name] = sprite;
            return sprite;
        }

        Sprite RoundedSprite(float radius, float strokeWidth)
        {
            var key = $"{radius}_{strokeWidth}";
            if (rounded.TryGetValue(key, out var cached))
                return cached;

            var r = radius * PixelsPerPoint;
            var border = Mathf.CeilToInt(r) + 2;
            var size = border * 2 + 4;
            var w = strokeWidth * PixelsPerPoint;
            var name = strokeWidth > 0f ? $"Contorno_{radius}_{strokeWidth}" : $"Arredondado_{radius}";

            var sprite = Generate(name, size, size, border, (x, y) =>
            {
                var outside = RoundedRectDistance(x, y, size, r);
                var fill = Coverage(-outside);
                return w > 0f ? Mathf.Min(fill, Coverage(outside + w)) : fill;
            });
            rounded[key] = sprite;
            return sprite;
        }

        /// <summary>Distância com sinal até a borda de um quadrado de lado size com cantos de raio r.</summary>
        static float RoundedRectDistance(float x, float y, float size, float r)
        {
            var half = size * 0.5f;
            var px = Mathf.Abs(x - half) - (half - r);
            var py = Mathf.Abs(y - half) - (half - r);
            var outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(px, py), 0f) - r;
        }

        static float Coverage(float signedInside) => Mathf.Clamp01(signedInside + 0.5f);

        Sprite Generate(string name, int width, int height, int border, System.Func<float, float, float> alpha)
        {
            var path = $"{SpritesFolder}/{name}.png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var a = (byte)Mathf.RoundToInt(alpha(x + 0.5f, y + 0.5f) * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, a);
            }
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            return ImportSprite(path, new Vector4(border, border, border, border), mipmaps: false,
                pixelsPerUnit: 100f * PixelsPerPoint);
        }

        static Sprite ImportSprite(string path, Vector4 border, bool mipmaps, float pixelsPerUnit = 100f)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
                return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelsPerUnit;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static TMP_FontAsset Font(string file)
        {
            var path = $"{FontsFolder}/{file} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
                return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontsFolder}/{file}.ttf");
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = $"{file} SDF";
            asset.TryAddCharacters(Characters);

            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = $"{file} SDF Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = $"{file} SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
