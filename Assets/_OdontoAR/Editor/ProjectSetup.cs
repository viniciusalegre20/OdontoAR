using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.ARKit;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARKit;
using UnityEngine.XR.Management;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Configura o projeto para iOS + ARKit (T1). Idempotente: pode ser rodado várias vezes.
    /// Também pode ser chamado por terminal:
    /// -executeMethod OdontoAR.Editor.ProjectSetup.ConfigureIOS
    /// </summary>
    public static class ProjectSetup
    {
        public const string ProductName = "OdontoAR";
        public const string CompanyName = "Aura Works";
        public const string BundleId = "com.auraworks.odontoar";
        public const string MinimumIOSVersion = "15.0";

        // Personal Team do Apple ID usado para assinar (Xcode → Settings → Accounts).
        // Outro membro da equipe que for assinar troca pelo próprio Team ID.
        public const string AppleTeamId = "9BVJCCNT6Z";
        public const string AppVersion = "1.0";

        public const string CameraUsageDescription =
            "A câmera é usada para posicionar o modelo 3D sobre a mesa. Nenhuma imagem é gravada.";

        const string XRFolder = "Assets/XR";
        const string XRSettingsPath = XRFolder + "/XRGeneralSettingsPerBuildTarget.asset";
        const string ARKitSettingsPath = XRFolder + "/Settings/ARKitSettings.asset";

        // Renderers do URP que precisam desenhar a imagem da câmera (Mobile = iPhone, PC = Editor).
        static readonly string[] RendererPaths =
        {
            "Assets/Settings/Mobile_Renderer.asset",
            "Assets/Settings/PC_Renderer.asset",
        };

        // Valor de "Active Input Handling" que corresponde a "Input System Package (New)".
        const int InputSystemOnly = 1;

        [MenuItem("OdontoAR/Configurar projeto iOS")]
        public static void ConfigureIOS()
        {
            var ok = true;

            ConfigurePlayer();
            ConfigureInputHandling();
            ok &= ConfigureXRManagement();
            ConfigureARKit();
            ok &= ConfigureURPForAR();
            ok &= AppBranding.Apply();
            AssetDatabase.SaveAssets();

            // Por último, porque reimporta o projeto.
            ok &= SwitchToIOS();

            if (ok)
                Debug.Log("[OdontoAR] Projeto configurado para iOS + ARKit.");
            else
                Debug.LogError("[OdontoAR] Configuração concluída com erros. Veja as mensagens acima.");

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = AppVersion; // vai no JSON do registro de uso
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.iOS.cameraUsageDescription = CameraUsageDescription;
            PlayerSettings.iOS.targetOSVersionString = MinimumIOSVersion;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = AppleTeamId;
            Debug.Log($"[OdontoAR] Player: {ProductName} ({BundleId}), iOS mínimo {MinimumIOSVersion}, " +
                      $"assinatura automática (time {AppleTeamId}).");
        }

        static void ConfigureInputHandling()
        {
            // Não há API pública para "Active Input Handling"; altera a propriedade serializada.
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings.Length == 0)
            {
                Debug.LogWarning("[OdontoAR] ProjectSettings.asset não encontrado; Active Input Handling não alterado.");
                return;
            }

            var serialized = new SerializedObject(playerSettings[0]);
            var property = serialized.FindProperty("activeInputHandler");
            if (property == null || property.intValue == InputSystemOnly)
                return;

            property.intValue = InputSystemOnly;
            serialized.ApplyModifiedProperties();
            Debug.Log("[OdontoAR] Active Input Handling: Input System. Reinicie o Unity para aplicar.");
        }

        static bool ConfigureXRManagement()
        {
            var perTarget = GetOrCreateXRSettings();

            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

            var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.iOS);
            settings.InitManagerOnStart = true;

            var manager = settings.Manager;
            var loaderName = typeof(ARKitLoader).FullName;
            foreach (var loader in manager.activeLoaders)
            {
                if (loader != null && loader.GetType().FullName == loaderName)
                    return true;
            }

            if (!XRPackageMetadataStore.AssignLoader(manager, loaderName, BuildTargetGroup.iOS))
            {
                Debug.LogError("[OdontoAR] Não foi possível ligar o ARKit no XR Plug-in Management (iOS).");
                return false;
            }

            EditorUtility.SetDirty(perTarget);
            Debug.Log("[OdontoAR] ARKit ligado no XR Plug-in Management (iOS).");
            return true;
        }

        static XRGeneralSettingsPerBuildTarget GetOrCreateXRSettings()
        {
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey,
                    out XRGeneralSettingsPerBuildTarget existing) && existing != null)
                return existing;

            var guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
            {
                existing = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (existing == null)
            {
                Directory.CreateDirectory(XRFolder);
                existing = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(existing, XRSettingsPath);
            }

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, existing, true);
            return existing;
        }

        static void ConfigureARKit()
        {
            var settings = ARKitSettings.currentSettings;
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ARKitSettings>();
                Directory.CreateDirectory(Path.GetDirectoryName(ARKitSettingsPath));
                AssetDatabase.CreateAsset(settings, ARKitSettingsPath);
                ARKitSettings.currentSettings = settings;
            }

            settings.requirement = ARKitSettings.Requirement.Required;
            settings.faceTracking = false;
            EditorUtility.SetDirty(settings);
            Debug.Log("[OdontoAR] ARKit: Required, sem face tracking.");
        }

        static bool ConfigureURPForAR()
        {
            var ok = true;
            foreach (var path in RendererPaths)
            {
                var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (data == null)
                {
                    Debug.LogError($"[OdontoAR] Renderer do URP não encontrado: {path}");
                    ok = false;
                    continue;
                }

                if (data.rendererFeatures.Any(f => f is ARBackgroundRendererFeature))
                    continue;

                // Mesmo procedimento do botão "Add Renderer Feature" do Inspector:
                // sub-asset + lista de features + mapa de IDs locais.
                var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                feature.name = nameof(ARBackgroundRendererFeature);
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

                var serialized = new SerializedObject(data);
                var features = serialized.FindProperty("m_RendererFeatures");
                var map = serialized.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);

                Debug.Log($"[OdontoAR] AR Background Renderer Feature adicionada em {path}.");
            }

            return ok;
        }

        static bool SwitchToIOS()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                return true;

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("[OdontoAR] Módulo iOS Build Support não instalado. " +
                               "Instale pelo Unity Hub (Installs → 6000.6.4f1 → Add modules) e rode o menu de novo.");
                return false;
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("[OdontoAR] Falha ao trocar a plataforma para iOS.");
                return false;
            }

            Debug.Log("[OdontoAR] Plataforma ativa: iOS.");
            return true;
        }
    }
}
