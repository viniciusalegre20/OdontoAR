using System.IO;
using OdontoAR.Model;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Prévia das telas sem aparelho (menu OdontoAR/Gerar prévias da interface): abre a cena AR, liga as
    /// partes de cada estado à mão (os scripts não rodam fora do Play) e salva PNGs de 1320 × 2868 em
    /// Builds/Previas. A câmera é um fundo cinza; a área segura simula o iPhone 16 Pro Max.
    /// Por terminal: -executeMethod OdontoAR.Editor.UIPreview.Capture
    /// </summary>
    public static class UIPreview
    {
        const int Width = 1320;
        const int Height = 2868;
        const float SafeTop = 62f;
        const float SafeBottom = 34f;
        const string OutputFolder = "Builds/Previas";

        [MenuItem("OdontoAR/Gerar prévias da interface")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ARSceneBuilder.ScenePath);
            Directory.CreateDirectory(OutputFolder);

            var canvasGo = GameObject.Find("Canvas");
            var canvas = canvasGo.GetComponent<Canvas>();
            var root = canvasGo.transform;

            var cameraGo = new GameObject("CameraPrevia", typeof(Camera));
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.42f, 0.36f, 0.30f); // "mesa de madeira"
            camera.orthographic = true;
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            // O CanvasScaler não roda fora do Play: aplica a escala de 440 × 956 pt à mão.
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.enabled = false;
            canvas.scaleFactor = Width / scaler.referenceResolution.x;

            foreach (var safe in root.GetComponentsInChildren<OdontoAR.UI.SafeArea>(true))
            {
                var rect = (RectTransform)safe.transform;
                rect.offsetMin = new Vector2(0f, SafeBottom);
                rect.offsetMax = new Vector2(0f, -SafeTop);
            }

            GameObject Find(string path) => root.Find(path).gameObject;
            var screens = new[] { "Tela01_Abertura", "Tela02_Codigo", "Tela03_Permissao", "Tela_RA", "Tela10_Registro" };
            void Only(string screen)
            {
                foreach (var s in screens)
                    Find(s).SetActive(s == screen);
            }

            const string hud = "Tela_RA/HUD";
            var searching = Find($"{hud}/Status/Procurando");
            var found = Find($"{hud}/Status/Encontrada");
            var unstable = Find($"{hud}/Status/Instavel");
            var segment = Find($"{hud}/BarraSuperior/SeletorArcada");
            var panel = Find($"{hud}/PainelArea/PainelCamadas");
            var expanded = Find($"{hud}/PainelArea/PainelCamadas/Aberto");
            var dots = Find($"{hud}/PainelArea/PainelCamadas/Cabecalho/Bolinhas");
            var hint = Find($"{hud}/PainelArea/PainelCamadas/DicaGestos");
            var card = Find("Tela_RA/CamadaCartao/CartaoEstrutura");
            var line = Find("Tela_RA/CamadaCartao/Linha");
            var anchor = Find("Tela_RA/CamadaCartao/Ancora");
            var help = Find("Tela_RA/Ajuda/Folha");

            void AR(bool search, bool find, bool placed, bool withCard, bool withHelp)
            {
                Only("Tela_RA");
                searching.SetActive(search);
                found.SetActive(find);
                unstable.SetActive(false);
                segment.SetActive(placed);
                panel.SetActive(placed);
                expanded.SetActive(placed && !withCard);
                dots.SetActive(withCard);
                hint.SetActive(placed && !withCard && !withHelp);
                card.SetActive(withCard);
                line.SetActive(withCard);
                anchor.SetActive(withCard);
                help.SetActive(withHelp);
            }

            Only("Tela01_Abertura");
            Shot(camera, target, "01_abertura");
            Only("Tela02_Codigo");
            Shot(camera, target, "02_codigo");
            Only("Tela03_Permissao");
            Find("Tela03_Permissao/AreaSegura/Conteudo").transform.Find("Texto").gameObject.SetActive(true);
            Shot(camera, target, "03_permissao");
            AR(search: true, find: false, placed: false, withCard: false, withHelp: false);
            Shot(camera, target, "04_procurando");
            AR(search: false, find: true, placed: false, withCard: false, withHelp: false);
            Shot(camera, target, "05_encontrada");
            AR(search: false, find: false, placed: true, withCard: false, withHelp: false);
            Shot(camera, target, "06_visualizacao");

            AR(search: false, find: false, placed: true, withCard: true, withHelp: false);
            var cardRect = (RectTransform)card.transform;
            cardRect.anchoredPosition = new Vector2(0f, 60f);
            ((RectTransform)anchor.transform).anchoredPosition = new Vector2(30f, -60f);
            var lineRect = (RectTransform)line.transform;
            lineRect.anchoredPosition = new Vector2(30f, -60f);
            lineRect.sizeDelta = new Vector2(2f, 70f);
            Shot(camera, target, "08_estrutura");

            AR(search: false, find: false, placed: true, withCard: false, withHelp: true);
            Shot(camera, target, "09_ajuda");

            Only("Tela10_Registro");
            Shot(camera, target, "10_registro");

            Object.DestroyImmediate(cameraGo);
            Debug.Log($"[OdontoAR] Prévias salvas em {OutputFolder}.");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static void Shot(Camera camera, RenderTexture target, string name)
        {
            // Várias passadas: o texto só sabe a altura depois de saber a largura.
            for (var pass = 0; pass < 4; pass++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var layout in Object.FindObjectsByType<LayoutGroup>(FindObjectsInactive.Exclude))
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes($"{OutputFolder}/{name}.png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
    }
}
