using System.Collections.Generic;
using System.Linq;
using OdontoAR.Theme;
using UnityEditor;
using UnityEngine;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Troca os materiais que vêm nos FBX (Teeth, Bone-5, Suture-3, Nerve-8...) por quatro materiais
    /// URP Lit com as cores do <see cref="OdontoARTheme"/>: coroa, raiz, osso e nervo.
    /// Usa o remapeamento do importador, então vale para todas as instâncias dos modelos.
    /// </summary>
    public static class ModelMaterials
    {
        const string ModelsFolder = "Assets/_OdontoAR/Models";
        const string MaterialsFolder = "Assets/_OdontoAR/Materials";

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
