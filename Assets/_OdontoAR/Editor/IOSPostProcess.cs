#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Ajustes no projeto Xcode gerado, reaplicados a cada build (inclusive em "append").
    /// </summary>
    public static class IOSPostProcess
    {
        // Alvo do IL2CPP que o Unity 6 gera além de Unity-iPhone e UnityFramework.
        const string GameAssemblyTarget = "GameAssembly";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
                return;

            var projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            // O sandbox de scripts do Xcode impede os scripts do IL2CPP de ler a pasta do projeto
            // ("file system sandbox blocked open()"), e o build falha em PhaseScriptExecution.
            DisableScriptSandboxing(project, project.ProjectGuid());
            DisableScriptSandboxing(project, project.GetUnityMainTargetGuid());
            DisableScriptSandboxing(project, project.GetUnityFrameworkTargetGuid());
            var gameAssembly = project.TargetGuidByName(GameAssemblyTarget);
            if (!string.IsNullOrEmpty(gameAssembly))
                DisableScriptSandboxing(project, gameAssembly);

            project.WriteToFile(projectPath);
            Debug.Log("[OdontoAR] Xcode: User Script Sandboxing desligado.");
        }

        static void DisableScriptSandboxing(PBXProject project, string guid)
        {
            project.SetBuildProperty(guid, "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
        }
    }
}
#endif
