using System.Collections.Generic;
using System.IO;
using System.Linq;
using OdontoAR.AR;
using OdontoAR.Model;
using OdontoAR.Theme;
using OdontoAR.UI;
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
    /// T3/T4: aplica os materiais dos modelos e gera Prefabs/Arcadas.prefab (a partir dos FBX),
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
        const string ShadowMaterialPath = MaterialsFolder + "/SombraContato.mat";
        const string ShadowTexturePath = MaterialsFolder + "/SombraContato.png";

        // A sombra cobre a projeção das arcadas na mesa com uma folga, para o degradê sumir fora dela.
        const float ContactShadowMargin = 1.4f;

        // Os côndilos ficam no topo de trás da mandíbula; o eixo da articulação fica um pouco para dentro.
        const float CondyleInset = 0.01f;

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
            ModelImportSettings.Apply();
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

            var archControls = ui.PlacedControls.AddComponent<ArchControls>();
            SetReference(archControls, "holder", holder);
            SetReference(archControls, "bothButton", ui.BothButton);
            SetReference(archControls, "upperButton", ui.UpperButton);
            SetReference(archControls, "lowerButton", ui.LowerButton);
            SetReference(archControls, "openingSlider", ui.OpeningSlider);

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
        ///       Articulacao (ArchController gira aqui; eixo nos côndilos)
        ///         Arcada_Superior: Maxila, Dentes_Superiores
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
                // Até aqui tudo está na origem, então as bounds (mundo) valem no espaço do Modelo.
                var bounds = RendererBounds(superior.GetComponentsInChildren<Renderer>()
                    .Concat(inferior.GetComponentsInChildren<Renderer>()));
                var mandible = RendererBounds(inferior.GetComponentsInChildren<Renderer>());

                var hinge = CreateChild(modelo, "Articulacao");
                hinge.transform.localPosition = new Vector3(
                    mandible.center.x, mandible.max.y - CondyleInset, mandible.min.z + CondyleInset);
                superior.transform.SetParent(hinge.transform, true);

                var arch = root.AddComponent<ArchController>();
                SetReference(arch, "holder", holder);
                SetReference(arch, "upperArch", superior);
                SetReference(arch, "lowerArch", inferior);
                SetReference(arch, "jawHinge", hinge.transform);

                modelo.transform.localPosition = -bounds.center;
                SetReference(view, "holder", holder);
                SetFloat(view, "baseHalfHeight", bounds.extents.y);

                var shadow = CreateContactShadow(root);
                SetReference(view, "contactShadow", shadow);
                SetVector2(view, "contactShadowBaseSize",
                    new Vector2(bounds.size.x, bounds.size.z) * ContactShadowMargin);

                return PrefabUtility.SaveAsPrefabAsset(root, ArcadasPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Bounds RendererBounds(IEnumerable<Renderer> renderers)
        {
            var list = renderers.ToList();
            var bounds = list[0].bounds;
            foreach (var r in list.Skip(1))
                bounds.Encapsulate(r.bounds);
            return bounds;
        }

        /// <summary>Quad deitado na mesa com um degradê radial escuro: "assenta" a arcada na superfície.</summary>
        static Transform CreateContactShadow(GameObject root)
        {
            var go = new GameObject("SombraContato", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.0005f, 0f);
            go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = BuildTransparentUnlit(
                ShadowMaterialPath, OdontoARTheme.ContactShadow, BuildShadowTexture());
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return go.transform;
        }

        /// <summary>Textura branca com alfa radial (1 no centro, 0 na borda), tingida pelo material.</summary>
        static Texture2D BuildShadowTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f) / size * 2f - 1f;
                var dy = (y + 0.5f) / size * 2f - 1f;
                var alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }

            File.WriteAllBytes(ShadowTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ShadowTexturePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(ShadowTexturePath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowTexturePath);
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

        static Material BuildReticleMaterial() =>
            BuildTransparentUnlit(ReticleMaterialPath, OdontoARTheme.PlacementReticle, null);

        static Material BuildTransparentUnlit(string path, Color color, Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
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
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
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
                typeof(ARCameraManager), typeof(ARCameraBackground), typeof(TrackedPoseDriver),
                typeof(AROcclusionManager));
            cameraGo.transform.SetParent(offset.transform, false);
            cameraGo.tag = "MainCamera";

            // Oclusão com a profundidade do LiDAR: mão e objetos reais à frente escondem a arcada.
            // Em aparelhos sem LiDAR o subsistema não oferece profundidade e nada acontece (T10).
            var occlusion = cameraGo.GetComponent<AROcclusionManager>();
            occlusion.requestedEnvironmentDepthMode = EnvironmentDepthMode.Best;
            occlusion.requestedOcclusionPreferenceMode = OcclusionPreferenceMode.PreferEnvironmentOcclusion;
            occlusion.requestedHumanStencilMode = HumanSegmentationStencilMode.Disabled;
            occlusion.requestedHumanDepthMode = HumanSegmentationDepthMode.Disabled;

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
            // O padrão (1,1176 m, pensado para óculos de VR) subia a câmera em relação aos planos e
            // âncoras, que ficam no espaço da sessão: a arcada parecia acompanhar o iPhone.
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            origin.CameraYOffset = 0f;
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
            public Button BothButton;
            public Button UpperButton;
            public Button LowerButton;
            public Slider OpeningSlider;
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
                "Afaste-se um pouco e aponte para a mesa, movendo o iPhone devagar. Quando o círculo aparecer, toque na tela para posicionar a arcada.", 44f,
                OdontoARTheme.TextOnOverlay);
            var hintTextRect = (RectTransform)hintText.transform;
            hintTextRect.anchorMin = Vector2.zero;
            hintTextRect.anchorMax = Vector2.one;
            hintTextRect.offsetMin = new Vector2(40f, 20f);
            hintTextRect.offsetMax = new Vector2(-40f, -20f);

            // Controles na base, acima do indicador de início do iPhone; aparecem com a arcada posicionada.
            var controls = new GameObject("ControlesPosicionado", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            controls.transform.SetParent(canvasGo.transform, false);
            var controlsRect = (RectTransform)controls.transform;
            controlsRect.anchorMin = controlsRect.anchorMax = new Vector2(0.5f, 0f);
            controlsRect.pivot = new Vector2(0.5f, 0f);
            controlsRect.anchoredPosition = new Vector2(0f, 120f);
            controlsRect.sizeDelta = new Vector2(1000f, 0f);
            var column = controls.GetComponent<VerticalLayoutGroup>();
            column.spacing = 30f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            controls.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Abertura da boca (RF06).
            var openingRow = CreateRow(controls.transform, "Abertura", 110f, 30f);
            var openingImage = openingRow.AddComponent<Image>();
            openingImage.sprite = resources.standard;
            openingImage.type = Image.Type.Sliced;
            openingImage.color = OdontoARTheme.SurfaceOverlay;
            var openingLayout = openingRow.GetComponent<HorizontalLayoutGroup>();
            openingLayout.padding = new RectOffset(40, 50, 0, 0);
            openingLayout.childForceExpandWidth = false;
            var openingLabel = CreateText(openingRow.transform, "Abertura", 42f, OdontoARTheme.TextOnOverlay);
            openingLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;
            var opening = CreateSlider(openingRow.transform);

            // Seletor de arcada (RF03).
            var selector = CreateRow(controls.transform, "SeletorArcada", 120f, 20f);
            var both = CreateButton(selector.transform, resources, "BotaoAmbas", "Ambas");
            var upper = CreateButton(selector.transform, resources, "BotaoSuperior", "Superior");
            var lower = CreateButton(selector.transform, resources, "BotaoInferior", "Inferior");

            var view = CreateRow(controls.transform, "ControlesVista", 140f, 40f);
            var reset = CreateButton(view.transform, resources, "BotaoRestaurarVista", "Restaurar vista");
            var reposition = CreateButton(view.transform, resources, "BotaoReposicionar", "Reposicionar");

            controls.SetActive(false);

            return new UIRefs
            {
                Hint = hint,
                PlacedControls = controls,
                ResetButton = reset,
                RepositionButton = reposition,
                BothButton = both,
                UpperButton = upper,
                LowerButton = lower,
                OpeningSlider = opening,
            };
        }

        /// <summary>Linha horizontal de altura fixa dentro da coluna de controles.</summary>
        static GameObject CreateRow(Transform parent, string name, float height, float spacing)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            return row;
        }

        static Slider CreateSlider(Transform parent)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            });
            go.name = "SliderAbertura";
            go.transform.SetParent(parent, false);
            var element = go.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.preferredHeight = 70f;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            go.transform.Find("Background").GetComponent<Image>().color = OdontoARTheme.SliderTrack;
            go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = OdontoARTheme.SliderFill;
            var handle = slider.handleRect;
            handle.GetComponent<Image>().color = OdontoARTheme.SliderHandle;
            handle.sizeDelta = new Vector2(70f, 0f); // alvo de toque do tamanho do dedo
            return slider;
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

        static void SetVector2(Object target, string field, Vector2 value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).vector2Value = value;
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
