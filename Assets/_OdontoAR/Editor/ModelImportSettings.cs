using UnityEditor;
using UnityEngine;

namespace OdontoAR.Editor
{
    /// <summary>
    /// Configura a importação dos FBX: gera MeshCollider em cada malha, usado pela seleção por toque (T7).
    /// </summary>
    public static class ModelImportSettings
    {
        const string ModelsFolder = "Assets/_OdontoAR/Models";

        public static void Apply()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer.addCollider)
                    continue;

                importer.addCollider = true;
                importer.SaveAndReimport();
                Debug.Log($"[OdontoAR] Colliders ligados em {path}.");
            }
        }
    }
}
