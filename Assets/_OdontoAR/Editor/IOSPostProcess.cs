#if UNITY_IOS
using System.Globalization;
using System.IO;
using OdontoAR.Theme;
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

            WriteLaunchScreen(path);
        }

        /// <summary>
        /// Troca o storyboard da tela de abertura que o Unity gera (vistas de 4896 pt deslocadas e máscaras
        /// por classe de tamanho, em que o símbolo não aparecia) por um simples: fundo bg-900 e a imagem do
        /// símbolo (LaunchScreen-iPhonePortrait.png, gerada pelo AppBranding) centralizada.
        /// </summary>
        static void WriteLaunchScreen(string path)
        {
            const string image = "LaunchScreen-iPhonePortrait.png";
            if (!File.Exists(Path.Combine(path, image)))
            {
                Debug.LogWarning("[OdontoAR] Imagem da tela de abertura ausente; storyboard do Unity mantido.");
                return;
            }

            var c = OdontoARTheme.Bg900;
            string F(float v) => v.ToString("0.########", CultureInfo.InvariantCulture);
            var storyboard = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<document type=""com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB"" version=""3.0"" toolsVersion=""21701"" targetRuntime=""iOS.CocoaTouch"" propertyAccessControl=""none"" useAutolayout=""YES"" launchScreen=""YES"" useTraitCollections=""YES"" useSafeAreas=""YES"" colorMatched=""YES"" initialViewController=""OAR-LS-VC"">
    <device id=""retina6_12"" orientation=""portrait"" appearance=""light""/>
    <dependencies>
        <plugIn identifier=""com.apple.InterfaceBuilder.IBCocoaTouchPlugin"" version=""21679""/>
        <capability name=""documents saved in the Xcode 8 format"" minToolsVersion=""8.0""/>
    </dependencies>
    <scenes>
        <scene sceneID=""OAR-LS-Scene"">
            <objects>
                <viewController id=""OAR-LS-VC"" sceneMemberID=""viewController"">
                    <view key=""view"" contentMode=""scaleToFill"" id=""OAR-LS-Root"">
                        <rect key=""frame"" x=""0.0"" y=""0.0"" width=""393"" height=""852""/>
                        <autoresizingMask key=""autoresizingMask"" widthSizable=""YES"" heightSizable=""YES""/>
                        <subviews>
                            <imageView clipsSubviews=""YES"" userInteractionEnabled=""NO"" contentMode=""scaleAspectFit"" image=""{image}"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""OAR-LS-Image"">
                                <rect key=""frame"" x=""88.5"" y=""318"" width=""216"" height=""216""/>
                            </imageView>
                        </subviews>
                        <color key=""backgroundColor"" red=""{F(c.r)}"" green=""{F(c.g)}"" blue=""{F(c.b)}"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
                        <constraints>
                            <constraint firstItem=""OAR-LS-Image"" firstAttribute=""centerX"" secondItem=""OAR-LS-Root"" secondAttribute=""centerX"" id=""OAR-LS-CX""/>
                            <constraint firstItem=""OAR-LS-Image"" firstAttribute=""centerY"" secondItem=""OAR-LS-Root"" secondAttribute=""centerY"" id=""OAR-LS-CY""/>
                            <constraint firstItem=""OAR-LS-Image"" firstAttribute=""width"" secondItem=""OAR-LS-Root"" secondAttribute=""width"" multiplier=""0.55"" id=""OAR-LS-W""/>
                            <constraint firstItem=""OAR-LS-Image"" firstAttribute=""height"" secondItem=""OAR-LS-Image"" secondAttribute=""width"" id=""OAR-LS-H""/>
                        </constraints>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier=""IBFirstResponder"" id=""OAR-LS-FR"" userLabel=""First Responder"" sceneMemberID=""firstResponder""/>
            </objects>
            <point key=""canvasLocation"" x=""0"" y=""0""/>
        </scene>
    </scenes>
    <resources>
        <image name=""{image}"" width=""768"" height=""768""/>
    </resources>
</document>
";
            File.WriteAllText(Path.Combine(path, "LaunchScreen-iPhone.storyboard"), storyboard);
            Debug.Log("[OdontoAR] Xcode: tela de abertura com o símbolo centralizado.");
        }

        static void DisableScriptSandboxing(PBXProject project, string guid)
        {
            project.SetBuildProperty(guid, "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
        }
    }
}
#endif
