using System.IO;
using System.Linq;
using OdontoAR.AR;
using OdontoAR.Model;
using OdontoAR.Theme;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OdontoAR.Editor
{
    /// <summary>
    /// T3: aplica os materiais dos modelos e gera Prefabs/Arcadas.prefab (a partir dos FBX),
    /// Prefabs/PlanoAR.prefab (invisível), a mira de posicionamento e Scenes/AR.unity já ligados
    /// entre si, e coloca a cena AR como a única do build.
    /// Recria tudo a cada execução. Por terminal:
    /// -executeMethod OdontoAR.Editor.ARSceneBuilder.CreateARScene
    /// </summary>
    public static class ARSceneBuilder
    {
        const string Root = "Assets/_OdontoAR";
        const string ModelsFolder = Root + "/Models";
        const string PrefabsFolder = Root + "/Prefabs";
        const string MaterialsFolder = Root + "/Materials";
        const string ScenesFolder = Root + "/Scenes";

        public const string ScenePath = ScenesFolder + "/AR.unity";
        const string ArcadasPrefabPath = PrefabsFolder + "/Arcadas.prefab";
        const string PlanePrefabPath = PrefabsFolder + "/PlanoAR.prefab";
        const string ReticleMaterialPath = MaterialsFolder + "/MiraPosicionamento.mat";
        const string ReticleMeshPath = ModelsFolder + "/MiraPosicionamento.asset";

        // Anel da mira, em metros.
        const float ReticleInnerRadius = 0.045f;
        const float ReticleOuterRadius = 0.055f;
        const int ReticleSegments = 64;

        // Resolução de referência da interface: iPhone em retrato (pixels).
        static readonly Vector2 ReferenceResolution = new(1170f, 2532f);

        [MenuItem("OdontoAR/Criar cena AR")]
        public static void CreateARScene()
        {
            if (!Application.isBatchMode && File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Criar cena AR",
                    "A cena AR, o prefab Arcadas e o plano AR serão recriados. Alterações feitas à mão neles serão perdidas.",
                    "Recriar", "Cancelar"))
                return;

            // A importação dos recursos do TextMeshPro é assíncrona: a cena só é montada depois dela,
            // senão os textos ficam sem fonte. (Em modo batch, não use -quit: o método chama Exit.)
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0)
                ImportTMPEssentialsThen(BuildAndReport);
            else
                BuildAndReport();
        }

        static void BuildAndReport()
        {
            var ok = Build();
            if (ok)
                Debug.Log($"[OdontoAR] Cena criada: {ScenePath} (única cena do build).");
            else
                Debug.LogError("[OdontoAR] Cena AR não foi criada. Veja as mensagens acima.");

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Build()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(ScenesFolder);

            // Material sem correspondência só gera aviso: a cena pode ser montada assim mesmo.
            ModelMaterials.Apply();

            var arcadasPrefab = BuildArcadasPrefab();
            if (arcadasPrefab == null)
                return false;

            var planePrefab = BuildPlanePrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLight();
            new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager));

            var (origin, arCamera) = CreateXROrigin();
            var planeManager = origin.gameObject.AddComponent<ARPlaneManager>();
            planeManager.planePrefab = planePrefab;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            var raycastManager = origin.gameObject.AddComponent<ARRaycastManager>();
            var anchorManager = origin.gameObject.AddComponent<ARAnchorManager>();

            var arcadas = (GameObject)PrefabUtility.InstantiatePrefab(arcadasPrefab, scene);
            arcadas.SetActive(false); // aparece ao tocar na mesa
            var holder = arcadas.GetComponent<ModelStateHolder>();

            var reticle = CreateReticle();

            CreateEventSystem();
            var ui = CreateCanvas();

            var tapToPlace = origin.gameObject.AddComponent<ARTapToPlace>();
            SetReference(tapToPlace, "raycastManager", raycastManager);
            SetReference(tapToPlace, "planeManager", planeManager);
            SetReference(tapToPlace, "anchorManager", anchorManager);
            SetReference(tapToPlace, "holder", holder);
            SetReference(tapToPlace, "arCamera", arCamera);
            SetReference(tapToPlace, "reticle", reticle);
            SetReference(tapToPlace, "placementHint", ui.Hint);
            SetReference(tapToPlace, "placedControls", ui.PlacedControls);

            var gestures = origin.gameObject.AddComponent<ModelGestures>();
            SetReference(gestures, "holder", holder);

            UnityEventTools.AddPersistentListener(ui.ResetButton.onClick, holder.ResetView);
            UnityEventTools.AddPersistentListener(ui.RepositionButton.onClick, tapToPlace.Reposition);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[OdontoAR] Falha ao salvar {ScenePath}.");
                return false;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            return true;
        }

        // ---------- Prefabs e material ----------

        /// <summary>
        /// Arcadas (ModelStateHolder; ARTapToPlace move este objeto)
        ///   Vista (ModelView; gira e escala em torno do centro do modelo)
        ///     Modelo (deslocado para o centro das arcadas ficar na origem)
        ///       Arcada_Superior: Maxila, Dentes_Superiores
        ///       Arcada_Inferior: Mandibula, Dentes_Inferiores
        ///       Nervos
        /// </summary>
        static GameObject BuildArcadasPrefab()
        {
            var root = new GameObject("Arcadas");
            try
            {
                var holder = root.AddComponent<ModelStateHolder>();
                var vista = CreateChild(root, "Vista");
                var view = vista.AddComponent<ModelView>();
                var modelo = CreateChild(vista, "Modelo");
                var superior = CreateChild(modelo, "Arcada_Superior");
                var inferior = CreateChild(modelo, "Arcada_Inferior");

                var ok = AddModel(superior, "maxilla-superior.fbx", "Maxila")
                         & AddModel(superior, "dentes-cima.fbx", "Dentes_Superiores")
                         & AddModel(inferior, "mandibula.fbx", "Mandibula")
                         & AddModel(inferior, "Dentes-baixo.fbx", "Dentes_Inferiores")
                         & AddModel(modelo, "nervoface2.fbx", "Nervos");
                if (!ok)
                    return null;

                // Os FBX vêm de um corpo inteiro (~1,5 m de altura). Centraliza pelas arcadas;
                // os nervos ficam de fora da conta porque se estendem bem além delas.
                var renderers = superior.GetComponentsInChildren<Renderer>()
                    .Concat(inferior.GetComponentsInChildren<Renderer>())
                    .ToArray();
                var bounds = renderers[0].bounds;
                foreach (var r in renderers.Skip(1))
                    bounds.Encapsulate(r.bounds);

                modelo.transform.localPosition = -bounds.center;
                SetReference(view, "holder", holder);
                SetFloat(view, "baseHalfHeight", bounds.extents.y);

                return PrefabUtility.SaveAsPrefabAsset(root, ArcadasPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static bool AddModel(GameObject parent, string file, string name)
        {
            var path = $"{ModelsFolder}/{file}";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError($"[OdontoAR] Modelo não encontrado: {path}");
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.transform);
            instance.name = name;
            return true;
        }

        /// <summary>
        /// Plano invisível: só o ARPlane, que o raycast usa. Desenhar os planos do ARKit deixava a tela
        /// poluída (planos sobrepostos e piscando); quem mostra onde a arcada vai é a mira.
        /// </summary>
        static GameObject BuildPlanePrefab()
        {
            var go = new GameObject("PlanoAR", typeof(ARPlane));
            try
            {
                return PrefabUtility.SaveAsPrefabAsset(go, PlanePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static Transform CreateReticle()
        {
            var go = new GameObject("MiraPosicionamento", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = BuildRingMesh();
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = BuildReticleMaterial();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            go.SetActive(false); // aparece quando encontra a mesa
            return go.transform;
        }

        /// <summary>Anel plano no XZ, virado para cima, com um leve afastamento da mesa.</summary>
        static Mesh BuildRingMesh()
        {
            const float lift = 0.001f;
            var vertices = new Vector3[ReticleSegments * 2];
            var triangles = new int[ReticleSegments * 6];
            for (var i = 0; i < ReticleSegments; i++)
            {
                var angle = i * Mathf.PI * 2f / ReticleSegments;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = dir * ReticleInnerRadius + Vector3.up * lift;
                vertices[i * 2 + 1] = dir * ReticleOuterRadius + Vector3.up * lift;

                var next = (i + 1) % ReticleSegments;
                var t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = next * 2;
                triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2 + 1;
                triangles[t + 4] = next * 2;
                triangles[t + 5] = next * 2 + 1;
            }

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ReticleMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, ReticleMeshPath);
            }

            mesh.Clear();
            mesh.name = "MiraPosicionamento";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Material BuildReticleMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ReticleMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, ReticleMaterialPath);
            }

            // URP Unlit transparente (Alpha blend), sem escrita de profundidade.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.SetColor("_BaseColor", OdontoARTheme.PlacementReticle);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- Cena ----------

        static void CreateLight()
        {
            var go = new GameObject("Luz");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        /// <summary>Mesmo que GameObject → XR → XR Origin (Mobile AR), com plano de corte próximo menor.</summary>
        static (XROrigin origin, Camera camera) CreateXROrigin()
        {
            var originGo = new GameObject("XR Origin", typeof(XROrigin));
            var offset = CreateChild(originGo, "Camera Offset");

            var cameraGo = new GameObject("Main Camera",
                typeof(Camera), typeof(AudioListener),
                typeof(ARCameraManager), typeof(ARCameraBackground), typeof(TrackedPoseDriver));
            cameraGo.transform.SetParent(offset.transform, false);
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black; // coberto pela imagem da câmera
            camera.nearClipPlane = 0.01f;          // o aluno chega bem perto de uma arcada de ~10 cm
            camera.farClipPlane = 20f;

            var poseDriver = cameraGo.GetComponent<TrackedPoseDriver>();
            var position = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            position.AddBinding("<HandheldARInputDevice>/devicePosition");
            var rotation = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotation.AddBinding("<HandheldARInputDevice>/deviceRotation");
            poseDriver.positionInput = new InputActionProperty(position);
            poseDriver.rotationInput = new InputActionProperty(rotation);

            var origin = originGo.GetComponent<XROrigin>();
            origin.CameraFloorOffsetObject = offset;
            origin.Camera = camera;
            return (origin, camera);
        }

        static void CreateEventSystem()
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
        }

        struct UIRefs
        {
            public GameObject Hint;
            public GameObject PlacedControls;
            public Button ResetButton;
            public Button RepositionButton;
        }

        static UIRefs CreateCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            var resources = new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            };

            // Dica no topo, abaixo da Dynamic Island.
            var hint = new GameObject("DicaPosicionar", typeof(RectTransform), typeof(Image));
            hint.transform.SetParent(canvasGo.transform, false);
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -220f);
            hintRect.sizeDelta = new Vector2(1000f, 220f);
            var hintImage = hint.GetComponent<Image>();
            hintImage.sprite = resources.standard;
            hintImage.type = Image.Type.Sliced;
            hintImage.color = OdontoARTheme.SurfaceOverlay;
            hintImage.raycastTarget = false;

            var hintText = CreateText(hint.transform,
                "Aponte para a mesa e mova o iPhone devagar. Quando o círculo aparecer, toque na tela para posicionar a arcada.", 44f,
                OdontoARTheme.TextOnOverlay);
            var hintTextRect = (RectTransform)hintText.transform;
            hintTextRect.anchorMin = Vector2.zero;
            hintTextRect.anchorMax = Vector2.one;
            hintTextRect.offsetMin = new Vector2(40f, 20f);
            hintTextRect.offsetMax = new Vector2(-40f, -20f);

            // Botões na base, acima do indicador de início do iPhone.
            var controls = new GameObject("ControlesVista", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            controls.transform.SetParent(canvasGo.transform, false);
            var controlsRect = (RectTransform)controls.transform;
            controlsRect.anchorMin = controlsRect.anchorMax = new Vector2(0.5f, 0f);
            controlsRect.pivot = new Vector2(0.5f, 0f);
            controlsRect.anchoredPosition = new Vector2(0f, 160f);
            controlsRect.sizeDelta = new Vector2(1000f, 140f);
            var layout = controls.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            var reset = CreateButton(controls.transform, resources, "BotaoRestaurarVista", "Restaurar vista");
            var reposition = CreateButton(controls.transform, resources, "BotaoReposicionar", "Reposicionar");
            controls.SetActive(false); // aparece com a arcada posicionada

            return new UIRefs
            {
                Hint = hint,
                PlacedControls = controls,
                ResetButton = reset,
                RepositionButton = reposition,
            };
        }

        static Button CreateButton(Transform parent, TMP_DefaultControls.Resources resources, string name, string label)
        {
            var go = TMP_DefaultControls.CreateButton(resources);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = OdontoARTheme.ButtonBackground;

            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = label;
            text.fontSize = 42f;
            text.color = OdontoARTheme.ButtonText;
            return go.GetComponent<Button>();
        }

        static TextMeshProUGUI CreateText(Transform parent, string content, float size, Color color)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        // ---------- Utilitários ----------

        static void ImportTMPEssentialsThen(System.Action next)
        {
            void Completed(string packageName)
            {
                Unsubscribe();
                Debug.Log($"[OdontoAR] {packageName} importado.");
                next();
            }

            void Failed(string packageName, string error)
            {
                Unsubscribe();
                Debug.LogError($"[OdontoAR] Falha ao importar {packageName}: {error}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }

            void Unsubscribe()
            {
                AssetDatabase.importPackageCompleted -= Completed;
                AssetDatabase.importPackageFailed -= Failed;
            }

            AssetDatabase.importPackageCompleted += Completed;
            AssetDatabase.importPackageFailed += Failed;

            var ugui = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            var package = Path.Combine(ugui.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            UnityEditor.AssetPackage.Package.Import(package, false);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
