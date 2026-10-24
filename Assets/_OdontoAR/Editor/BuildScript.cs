using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Gera o projeto Xcode em Builds/iOS (T2). Por terminal:
    /// -executeMethod OdontoAR.Editor.BuildScript.BuildIOS
    /// Em modo batch, sai com código 0 em sucesso e 1 em erro.
    /// </summary>
    public static class BuildScript
    {
        public const string IOSOutputPath = "Builds/iOS";

        [MenuItem("OdontoAR/Build iOS (gerar Xcode)")]
        public static void BuildIOS()
        {
            var ok = Build();
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Build()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[OdontoAR] Nenhuma cena habilitada em Build Profiles.");
                return false;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("[OdontoAR] Módulo iOS Build Support não instalado.");
                return false;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = IOSOutputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };

            // Se o projeto Xcode já existe, faz "append" para preservar a assinatura feita no Xcode.
            if (Directory.Exists(Path.Combine(IOSOutputPath, "Unity-iPhone.xcodeproj")))
                options.options |= BuildOptions.AcceptExternalModificationsToPlayer;

            Debug.Log($"[OdontoAR] Build iOS: {string.Join(", ", scenes)} → {IOSOutputPath}");
            var summary = BuildPipeline.BuildPlayer(options).summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[OdontoAR] Build iOS falhou: {summary.result}, {summary.totalErrors} erro(s).");
                return false;
            }

            Debug.Log($"[OdontoAR] Build iOS concluído em {summary.totalTime.TotalSeconds:F0} s: {IOSOutputPath}");
            return true;
        }
    }
}
