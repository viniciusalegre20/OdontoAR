using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Prévia do modelo apoiado numa mesa (menu OdontoAR/Gerar prévias do modelo): vista lateral e vista do
    /// aluno sentado (40 cm acima da mesa, 45 cm à frente), para conferir inclinação e apoio sem aparelho.
    /// PNGs em Builds/Previas. Por terminal: -executeMethod OdontoAR.Editor.ModelPreview.Capture
    /// </summary>
    public static class ModelPreview
    {
        const string PrefabPath = "Assets/_OdontoAR/Prefabs/Arcadas.prefab";
        const string OutputFolder = "Builds/Previas";
        const int Width = 1200;
        const int Height = 900;

        [MenuItem("OdontoAR/Gerar prévias do modelo")]
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(OutputFolder);

            var light = new GameObject("Luz").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var table = GameObject.CreatePrimitive(PrimitiveType.Plane);
            table.transform.localScale = Vector3.one * 0.08f; // 80 cm
            table.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.55f, 0.42f, 0.30f),
            };

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            model.SetActive(true);
            // O ModelView não roda fora do Play: aplica a altura de apoio à mão (escala 1, sem giro).
            var vista = model.transform.Find("Vista");
            var halfHeight = new SerializedObject(vista.GetComponent<Model.ModelView>()).FindProperty("baseExtents").vector3Value.y;
            vista.localPosition = new Vector3(0f, halfHeight, 0f);

            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.85f, 0.87f, 0.88f);
            camera.nearClipPlane = 0.01f;
            camera.fieldOfView = 30f;
            var target = new RenderTexture(Width, Height, 24);
            camera.targetTexture = target;

            // Lateral: olho na altura da mesa, do lado direito do paciente (-X).
            Shot(camera, target, new Vector3(-0.45f, 0.03f, 0f), new Vector3(0f, 0.03f, 0f), "modelo_lateral");
            // Aluno sentado de frente para a arcada (frente do modelo em +Z).
            Shot(camera, target, new Vector3(0f, 0.40f, 0.45f), new Vector3(0f, 0.03f, 0f), "modelo_aluno");
            // De frente, na altura da mesa.
            Shot(camera, target, new Vector3(0f, 0.03f, 0.45f), new Vector3(0f, 0.03f, 0f), "modelo_frente");

            Debug.Log($"[OdontoAR] Prévias do modelo salvas em {OutputFolder}.");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static void Shot(Camera camera, RenderTexture target, Vector3 eye, Vector3 lookAt, string name)
        {
            camera.transform.position = eye;
            camera.transform.LookAt(lookAt);
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
