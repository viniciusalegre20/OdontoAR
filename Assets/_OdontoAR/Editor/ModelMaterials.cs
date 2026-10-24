using System.Collections.Generic;
using System.Linq;
using OdontoAR.Theme;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Troca os materiais que vêm nos FBX (Teeth, Bone-5, Suture-3, Nerve-8...) por quatro materiais
    /// URP Lit com as cores do <see cref="OdontoARTheme"/>: coroa, raiz, osso e nervo.
    /// Usa o remapeamento do importador, então vale para todas as instâncias dos modelos.
    /// O osso é sempre translúcido; também cria as variantes translúcidas de coroa e raiz e o material
    /// "Oculto", que o LayerController troca em tempo de execução (T6).
    /// </summary>
    public static class ModelMaterials
    {
        const string ModelsFolder = "Assets/_OdontoAR/Models";
        const string MaterialsFolder = "Assets/_OdontoAR/Materials";

        public const string CrownPath = MaterialsFolder + "/Coroa.mat";
        public const string RootPath = MaterialsFolder + "/Raiz.mat";
        public const string CrownTranslucentPath = MaterialsFolder + "/Coroa_Translucida.mat";
        public const string RootTranslucentPath = MaterialsFolder + "/Raiz_Translucida.mat";
        public const string HiddenPath = MaterialsFolder + "/Oculto.mat";
        public const string BonePath = MaterialsFolder + "/Osso.mat";
        public const string NervePath = MaterialsFolder + "/Nervo.mat";
        public const string CrownDimmedPath = MaterialsFolder + "/Coroa_Esmaecida.mat";
        public const string RootDimmedPath = MaterialsFolder + "/Raiz_Esmaecida.mat";
        public const string BoneDimmedPath = MaterialsFolder + "/Osso_Esmaecido.mat";
        public const string NerveDimmedPath = MaterialsFolder + "/Nervo_Esmaecido.mat";
        public const string OutlinePath = MaterialsFolder + "/Contorno.mat";

        // Ordem dos transparentes: dentes, depois o osso por cima (o osso envolve as raízes).
        const int TeethQueue = (int)RenderQueue.Transparent;
        const int BoneQueue = TeethQueue + 1;

        enum Kind { Crown, Root, Bone, Nerve }

        // Ordem importa: "Teeth-roots" precisa ser testado antes de "Teeth".
        static readonly (string prefix, Kind kind)[] Rules =
        {
            ("Teeth-roots", Kind.Root),
            ("Dentine", Kind.Root),
            ("Teeth", Kind.Crown),
            ("Bone", Kind.Bone),
            ("Suture", Kind.Bone),
            ("Cartilage", Kind.Bone),
            ("Nerve", Kind.Nerve),
            ("Nucleus", Kind.Nerve),
        };

        public static bool Apply()
        {
            var materials = new Dictionary<Kind, Material>
            {
                [Kind.Crown] = GetOrCreate("Coroa", OdontoARTheme.ToothCrown, OdontoARTheme.ToothSmoothness),
                [Kind.Root] = GetOrCreate("Raiz", OdontoARTheme.ToothRoot, OdontoARTheme.ToothSmoothness),
                [Kind.Bone] = GetOrCreate("Osso", OdontoARTheme.Bone, OdontoARTheme.BoneSmoothness),
                [Kind.Nerve] = GetOrCreate("Nervo", OdontoARTheme.Nerve, OdontoARTheme.NerveSmoothness),
            };
            MakeTransparent(materials[Kind.Bone], OdontoARTheme.BoneOpacity, writeDepth: false, BoneQueue);

            // Dentes translúcidos escrevem profundidade: coroa e raiz do mesmo dente não se misturam.
            MakeTransparent(GetOrCreate("Coroa_Translucida", OdontoARTheme.ToothCrown, OdontoARTheme.ToothSmoothness),
                OdontoARTheme.IntegratedCrownOpacity, writeDepth: true, TeethQueue);
            MakeTransparent(GetOrCreate("Raiz_Translucida", OdontoARTheme.ToothRoot, OdontoARTheme.ToothSmoothness),
                OdontoARTheme.IntegratedRootOpacity, writeDepth: true, TeethQueue);
            var hidden = GetOrCreate("Oculto", OdontoARTheme.Hidden, 0f);
            MakeTransparent(hidden, OdontoARTheme.Hidden.a, writeDepth: false, TeethQueue);
            hidden.SetShaderPassEnabled("ShadowCaster", false);

            // Esmaecidos (RF09): tudo o que não está selecionado. Sem escrever profundidade, para não
            // esconder a estrutura selecionada que esteja atrás.
            MakeTransparent(GetOrCreate("Coroa_Esmaecida", OdontoARTheme.ToothCrown, OdontoARTheme.ToothSmoothness),
                OdontoARTheme.DimmedOpacity, writeDepth: false, TeethQueue);
            MakeTransparent(GetOrCreate("Raiz_Esmaecida", OdontoARTheme.ToothRoot, OdontoARTheme.ToothSmoothness),
                OdontoARTheme.DimmedOpacity, writeDepth: false, TeethQueue);
            MakeTransparent(GetOrCreate("Osso_Esmaecido", OdontoARTheme.Bone, OdontoARTheme.BoneSmoothness),
                OdontoARTheme.DimmedOpacity, writeDepth: false, BoneQueue);
            MakeTransparent(GetOrCreate("Nervo_Esmaecido", OdontoARTheme.Nerve, OdontoARTheme.NerveSmoothness),
                OdontoARTheme.DimmedOpacity, writeDepth: false, TeethQueue);

            var outline = AssetDatabase.LoadAssetAtPath<Material>(OutlinePath);
            if (outline == null)
            {
                outline = new Material(Shader.Find("OdontoAR/Contorno"));
                AssetDatabase.CreateAsset(outline, OutlinePath);
            }
            outline.SetColor("_BaseColor", OdontoARTheme.SelectionOutline);
            outline.SetFloat("_Width", OdontoARTheme.SelectionOutlineWidth);
            EditorUtility.SetDirty(outline);

            var ok = true;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                var changed = false;

                foreach (var name in SourceMaterialNames(importer, path))
                {
                    if (!TryClassify(name, out var kind))
                    {
                        Debug.LogWarning($"[OdontoAR] Material \"{name}\" de {path} sem regra de cor; ficou o original.");
                        ok = false;
                        continue;
                    }

                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), name);
                    importer.GetExternalObjectMap().TryGetValue(id, out var current);
                    if (current == materials[kind])
                        continue;

                    importer.AddRemap(id, materials[kind]);
                    changed = true;
                }

                if (changed)
                    importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[OdontoAR] Materiais dos modelos: coroa, raiz, osso (bege) e nervo (azul).");
            return ok;
        }

        /// <summary>Nomes dos materiais do FBX: os já remapeados e os ainda embutidos.</summary>
        static IEnumerable<string> SourceMaterialNames(ModelImporter importer, string path)
        {
            var remapped = importer.GetExternalObjectMap().Keys
                .Where(k => k.type == typeof(Material))
                .Select(k => k.name);
            var embedded = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Material>()
                .Select(m => m.name);
            return remapped.Concat(embedded).Distinct().ToList();
        }

        static bool TryClassify(string materialName, out Kind kind)
        {
            foreach (var (prefix, k) in Rules)
            {
                if (materialName.StartsWith(prefix))
                {
                    kind = k;
                    return true;
                }
            }

            kind = default;
            return false;
        }

        /// <summary>URP Lit transparente (alpha blend) com a opacidade dada.</summary>
        static void MakeTransparent(Material material, float opacity, bool writeDepth, int queue)
        {
            var color = material.GetColor("_BaseColor");
            color.a = opacity;
            material.SetColor("_BaseColor", color);

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", writeDepth ? 1f : 0f);
            material.SetFloat("_ZWriteControl", writeDepth ? 1f : 2f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetShaderPassEnabled("DepthOnly", false);
            material.renderQueue = queue;
            EditorUtility.SetDirty(material);
        }

        static Material GetOrCreate(string name, Color color, float smoothness)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
