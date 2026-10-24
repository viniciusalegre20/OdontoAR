using System.Collections.Generic;
using System.Linq;
using OdontoAR.Model;
using UnityEditor;
using UnityEngine;

namespace OdontoAR.Editor
{
    /// <summary>
    /// T5 (menu OdontoAR/Validar modelo): confere a convenção dos FBX.
    /// Erros: dente que não mapeia para FDI, código repetido, dente na arcada ou no lado errado,
    /// dente sem coroa ou raiz, malha sem collider, material fora dos quatro do tema.
    /// Terceiros molares ausentes são só avisados (o atlas não os tem).
    /// Por terminal: -executeMethod OdontoAR.Editor.ModelValidator.ValidateFromCommandLine
    /// </summary>
    public static class ModelValidator
    {
        const string ModelsFolder = "Assets/_OdontoAR/Models";

        static readonly (string file, bool upper)[] ToothModels =
        {
            ("dentes-cima.fbx", true),
            ("Dentes-baixo.fbx", false),
        };

        static readonly string[] OtherModels = { "maxilla-superior.fbx", "mandibula.fbx", "nervoface2.fbx" };

        // Materiais gerados por ModelMaterials.
        const string CrownMaterial = "Coroa";
        const string RootMaterial = "Raiz";
        static readonly HashSet<string> ThemeMaterials = new() { CrownMaterial, RootMaterial, "Osso", "Nervo" };

        [MenuItem("OdontoAR/Validar modelo")]
        public static void ValidateMenu()
        {
            var ok = Validate();
            EditorUtility.DisplayDialog("Validar modelo",
                ok ? "Modelo sem erros. Detalhes no Console." : "O modelo tem erros. Veja o Console.", "OK");
        }

        public static void ValidateFromCommandLine()
        {
            var ok = Validate();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Validate()
        {
            var errors = new List<string>();
            var notes = new List<string>();
            var found = new Dictionary<int, string>();

            foreach (var (file, upper) in ToothModels)
            {
                var model = Load(file, errors);
                if (model == null)
                    continue;

                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    var name = renderer.gameObject.name;
                    var where = $"{file}: \"{name}\"";

                    CheckCollider(renderer, where, errors);
                    CheckThemeMaterials(renderer, where, errors);

                    if (!FdiCode.TryParse(name, out var code))
                    {
                        errors.Add($"{where} não mapeia para FDI.");
                        continue;
                    }

                    if (found.TryGetValue(code, out var other))
                        errors.Add($"{where} repete o código {code} de \"{other}\".");
                    else
                        found[code] = name;

                    if (FdiCode.IsUpper(code) != upper)
                        errors.Add($"{where} ({code}) está no arquivo da arcada {(upper ? "superior" : "inferior")}.");

                    // O modelo olha para +Z: o lado esquerdo do paciente fica em -X.
                    var x = renderer.bounds.center.x;
                    if (FdiCode.IsPatientLeft(code) != (x < 0f))
                        errors.Add($"{where} ({code}) está do lado {(x < 0f ? "esquerdo" : "direito")} do paciente.");

                    var materials = renderer.sharedMaterials.Where(m => m != null).Select(m => m.name).ToList();
                    if (!materials.Contains(CrownMaterial))
                        errors.Add($"{where} ({code}) sem material de coroa.");
                    if (!materials.Contains(RootMaterial))
                        errors.Add($"{where} ({code}) sem material de raiz.");
                }
            }

            foreach (var file in OtherModels)
            {
                var model = Load(file, errors);
                if (model == null)
                    continue;

                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    var where = $"{file}: \"{renderer.gameObject.name}\"";
                    CheckCollider(renderer, where, errors);
                    CheckThemeMaterials(renderer, where, errors);
                }
            }

            foreach (var code in FdiCode.AllPermanent.Where(c => !found.ContainsKey(c)))
            {
                if (FdiCode.IsThirdMolar(code))
                    notes.Add($"Dente {code} (terceiro molar) ausente no atlas.");
                else
                    errors.Add($"Dente {code} ausente.");
            }

            foreach (var note in notes)
                Debug.Log($"[OdontoAR] Validação: {note}");
            foreach (var error in errors)
                Debug.LogError($"[OdontoAR] Validação: {error}");

            Debug.Log(errors.Count == 0
                ? $"[OdontoAR] Validação concluída: {found.Count} dentes mapeados para FDI, sem erros."
                : $"[OdontoAR] Validação concluída com {errors.Count} erro(s).");
            return errors.Count == 0;
        }

        static GameObject Load(string file, List<string> errors)
        {
            var path = $"{ModelsFolder}/{file}";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
                errors.Add($"Modelo não encontrado: {path}");
            return model;
        }

        static void CheckCollider(Renderer renderer, string where, List<string> errors)
        {
            if (renderer.GetComponent<Collider>() == null)
                errors.Add($"{where} sem collider.");
        }

        static void CheckThemeMaterials(Renderer renderer, string where, List<string> errors)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || !ThemeMaterials.Contains(material.name))
                    errors.Add($"{where} usa material fora do tema: \"{(material != null ? material.name : "nenhum")}\".");
            }
        }
    }
}
