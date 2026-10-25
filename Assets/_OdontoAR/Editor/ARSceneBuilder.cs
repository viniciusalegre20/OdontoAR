using System.Collections.Generic;
using System.IO;
using System.Linq;
using OdontoAR.AR;
using OdontoAR.Diagnostics;
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
    /// T3–T7: aplica os materiais dos modelos e gera Prefabs/Arcadas.prefab (a partir dos FBX),
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
        const string ReticleInnerMaterialPath = MaterialsFolder + "/MiraPosicionamentoInterna.mat";
        const string ReticleMeshPath = ModelsFolder + "/MiraPosicionamento.asset";
        const string ShadowMaterialPath = MaterialsFolder + "/SombraContato.mat";
        const string ShadowTexturePath = MaterialsFolder + "/SombraContato.png";
        const string DataFolder = Root + "/Data";
        const string CatalogPath = DataFolder + "/Estruturas.asset";

        // A sombra cobre a projeção das arcadas na mesa com uma folga, para o degradê sumir fora dela.
        const float ContactShadowMargin = 1.4f;

        // Os côndilos ficam no topo de trás da mandíbula; o eixo da articulação fica um pouco para dentro.
        const float CondyleInset = 0.01f;

        // Anel da mira, em metros.
        // Retículo (design system): anel branco, anel interno tracejado ciano e ponto central. Em metros.
        const float ReticleInnerRadius = 0.045f;
        const float ReticleOuterRadius = 0.055f;
        const float ReticleDashInner = 0.030f;
        const float ReticleDashOuter = 0.034f;
        const int ReticleDashes = 24;
        const float ReticleDotRadius = 0.004f;
        const int ReticleSegments = 64;


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
            EnsureFolder(DataFolder);

            // Material sem correspondência só gera aviso: a cena pode ser montada assim mesmo.
            ModelImportSettings.Apply();
            ModelMaterials.Apply();

            var arcadasPrefab = BuildArcadasPrefab();
            if (arcadasPrefab == null)
                return false;

            var planePrefab = BuildPlanePrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLight();
            new GameObject("MonitorFPS", typeof(FrameRateMonitor));
            // A sessão (e a câmera) só liga na tela de RA, depois da permissão: quem liga é o ScreenRouter.
            var session = new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager)).GetComponent<ARSession>();
            session.enabled = false;

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

            var tapToPlace = origin.gameObject.AddComponent<ARTapToPlace>();
            SetReference(tapToPlace, "raycastManager", raycastManager);
            SetReference(tapToPlace, "planeManager", planeManager);
            SetReference(tapToPlace, "anchorManager", anchorManager);
            SetReference(tapToPlace, "holder", holder);
            SetReference(tapToPlace, "arCamera", arCamera);
            SetReference(tapToPlace, "reticle", reticle);

            var gestures = origin.gameObject.AddComponent<ModelGestures>();
            SetReference(gestures, "holder", holder);

            var selection = origin.gameObject.AddComponent<SelectionController>();
            SetReference(selection, "holder", holder);
            SetReference(selection, "arCamera", arCamera);
            SetReference(selection, "outlineMaterial", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.OutlinePath));
            SetArray(selection, "structures", arcadas.GetComponentsInChildren<SelectableStructure>(true));

            ARUIBuilder.Build(new ARUIBuilder.Context
            {
                Holder = holder,
                TapToPlace = tapToPlace,
                Selection = selection,
                Camera = arCamera,
                Session = session,
                Catalog = AssetDatabase.LoadAssetAtPath<StructureCatalog>(CatalogPath),
            });

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

                var layers = root.AddComponent<LayerController>();
                SetReference(layers, "holder", holder);
                SetReference(layers, "crown", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.CrownPath));
                SetReference(layers, "crownTranslucent",
                    AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.CrownTranslucentPath));
                SetReference(layers, "root", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.RootPath));
                SetReference(layers, "rootTranslucent",
                    AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.RootTranslucentPath));
                SetReference(layers, "hidden", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.HiddenPath));
                SetReference(layers, "bone", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.BonePath));
                SetReference(layers, "nerve", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.NervePath));
                SetReference(layers, "crownDimmed", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.CrownDimmedPath));
                SetReference(layers, "rootDimmed", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.RootDimmedPath));
                SetReference(layers, "boneDimmed", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.BoneDimmedPath));
                SetReference(layers, "nerveDimmed", AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.NerveDimmedPath));
                var teeth = RenderersOf(superior, "Dentes_Superiores").Concat(RenderersOf(inferior, "Dentes_Inferiores")).ToList();
                var boneRenderers = RenderersOf(superior, "Maxila").Concat(RenderersOf(inferior, "Mandibula")).ToList();
                var nerveRenderers = RenderersOf(modelo, "Nervos").ToList();
                SetArray(layers, "teeth", teeth);
                SetArray(layers, "boneRenderers", boneRenderers);
                SetArray(layers, "nerveRenderers", nerveRenderers);

                BuildCatalog(AddSelectable(teeth, tooth: true)
                    .Concat(AddSelectable(boneRenderers, tooth: false))
                    .Concat(AddSelectable(nerveRenderers, tooth: false)));

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

        /// <summary>
        /// Põe um SelectableStructure em cada malha, com a chave (FDI nos dentes, nome do objeto nas demais)
        /// e o primeiro triângulo e a camada de cada submesh. Devolve chave e nome para o catálogo.
        /// </summary>
        static List<StructureCatalog.Entry> AddSelectable(IEnumerable<Renderer> renderers, bool tooth)
        {
            var crown = AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.CrownPath);
            var nerve = AssetDatabase.LoadAssetAtPath<Material>(ModelMaterials.NervePath);
            var entries = new List<StructureCatalog.Entry>();

            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;

                // FBX de uma malha só (a mandíbula) vira o próprio objeto raiz, que é renomeado no prefab;
                // aí o nome do atlas fica só na malha.
                var objectName = renderer.gameObject.name;
                if (!tooth && !StructureNames.TryOther(objectName, out _))
                    objectName = mesh.name;

                string key, displayName;
                if (tooth && FdiCode.TryParse(objectName, out var code) && StructureNames.TryTooth(code, out displayName))
                {
                    key = code.ToString();
                }
                else if (!tooth && StructureNames.TryOther(objectName, out displayName))
                {
                    key = objectName;
                }
                else
                {
                    Debug.LogWarning($"[OdontoAR] \"{objectName}\" sem nome em português; não será selecionável.");
                    continue;
                }

                var materials = renderer.sharedMaterials;
                var firstTriangles = new int[mesh.subMeshCount];
                var layers = new Layer[mesh.subMeshCount];
                for (var i = 0; i < mesh.subMeshCount; i++)
                {
                    firstTriangles[i] = (int)(mesh.GetSubMesh(i).indexStart / 3);
                    var material = i < materials.Length ? materials[i] : null;
                    layers[i] = tooth
                        ? material == crown ? Layer.Crown : Layer.Root
                        : material == nerve ? Layer.Nerves : Layer.Bone;
                }

                var structure = renderer.gameObject.AddComponent<SelectableStructure>();
                structure.Configure(key, tooth, firstTriangles, layers);
                entries.Add(new StructureCatalog.Entry { key = key, displayName = displayName });
            }

            return entries;
        }

        static void BuildCatalog(IEnumerable<StructureCatalog.Entry> structures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StructureCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StructureCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            // Dentes do modelo e também os ausentes (terceiros molares), para quando existirem.
            var entries = structures.GroupBy(e => e.key).Select(g => g.First()).ToList();
            foreach (var code in FdiCode.AllPermanent)
            {
                var key = code.ToString();
                if (entries.All(e => e.key != key) && StructureNames.TryTooth(code, out var name))
                    entries.Add(new StructureCatalog.Entry { key = key, displayName = name });
            }
            foreach (Layer layer in System.Enum.GetValues(typeof(Layer)))
                entries.Add(new StructureCatalog.Entry
                    { key = StructureCatalog.LayerKey(layer), displayName = StructureNames.LayerName(layer) });

            catalog.SetEntries(entries.OrderBy(e => e.key, System.StringComparer.Ordinal));
            EditorUtility.SetDirty(catalog);
        }

        static IEnumerable<Renderer> RenderersOf(GameObject parent, string child) =>
            parent.transform.Find(child).GetComponentsInChildren<Renderer>(true);

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
            var go = new GameObject("MiraPosicionamento", typeof(MeshFilter), typeof(MeshRenderer), typeof(ReticlePulse));
            go.GetComponent<MeshFilter>().sharedMesh = BuildReticleMesh();
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = new[]
            {
                BuildTransparentUnlit(ReticleMaterialPath, OdontoARTheme.PlacementReticle, null),
                BuildTransparentUnlit(ReticleInnerMaterialPath, OdontoARTheme.PlacementReticleInner, null),
            };
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            go.SetActive(false); // aparece quando encontra a mesa
            return go.transform;
        }

        /// <summary>
        /// Malha plana no XZ, um pouco acima da mesa. Submesh 0: anel branco. Submesh 1: traços ciano
        /// do anel interno e o ponto central.
        /// </summary>
        static Mesh BuildReticleMesh()
        {
            const float lift = 0.001f;
            var vertices = new List<Vector3>();
            var outer = new List<int>();
            var inner = new List<int>();

            void Arc(List<int> triangles, float r0, float r1, float from, float to, int steps)
            {
                var start = vertices.Count;
                for (var i = 0; i <= steps; i++)
                {
                    var angle = Mathf.Lerp(from, to, i / (float)steps);
                    var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    vertices.Add(dir * r0 + Vector3.up * lift);
                    vertices.Add(dir * r1 + Vector3.up * lift);
                }
                for (var i = 0; i < steps; i++)
                {
                    var a = start + i * 2;
                    triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
            }

            Arc(outer, ReticleInnerRadius, ReticleOuterRadius, 0f, Mathf.PI * 2f, ReticleSegments);

            var dash = Mathf.PI * 2f / ReticleDashes;
            for (var d = 0; d < ReticleDashes; d++)
                Arc(inner, ReticleDashInner, ReticleDashOuter, d * dash, d * dash + dash * 0.55f, 3);

            // Ponto central: leque de triângulos.
            var center = vertices.Count;
            vertices.Add(Vector3.up * lift);
            for (var i = 0; i < 16; i++)
            {
                var angle = i * Mathf.PI * 2f / 16;
                vertices.Add(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ReticleDotRadius + Vector3.up * lift);
            }
            for (var i = 0; i < 16; i++)
                inner.AddRange(new[] { center, center + 1 + (i + 1) % 16, center + 1 + i });

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ReticleMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, ReticleMeshPath);
            }

            mesh.Clear();
            mesh.name = "MiraPosicionamento";
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(outer, 0);
            mesh.SetTriangles(inner, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

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

        static void SetArray(Object target, string field, IEnumerable<Object> values)
        {
            var list = values.ToList();
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = list.Count;
            for (var i = 0; i < list.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
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
