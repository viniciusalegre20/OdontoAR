using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// Estrutura que pode ser tocada (RF09): chave no <see cref="StructureCatalog"/> e a camada de cada submesh.
    /// Nos dentes, coroa e raiz são submeshes da mesma malha; o primeiro triângulo de cada uma é gravado
    /// pelo menu OdontoAR/Criar cena AR (a malha não fica legível em tempo de execução).
    /// </summary>
    public sealed class SelectableStructure : MonoBehaviour
    {
        [SerializeField] string key;
        [SerializeField] bool isTooth;
        [SerializeField] int[] submeshFirstTriangle = { 0 };
        [SerializeField] Layer[] submeshLayers = { Layer.Bone };

        Renderer cachedRenderer;

        public string Key => key;
        public bool IsTooth => isTooth;
        public Renderer Renderer => cachedRenderer != null ? cachedRenderer : cachedRenderer = GetComponent<Renderer>();

        public void Configure(string structureKey, bool tooth, int[] firstTriangles, Layer[] layers)
        {
            key = structureKey;
            isTooth = tooth;
            submeshFirstTriangle = firstTriangles;
            submeshLayers = layers;
        }

        /// <summary>Camada do triângulo atingido por um raycast (RaycastHit.triangleIndex).</summary>
        public Layer LayerAt(int triangleIndex)
        {
            var layer = submeshLayers[0];
            for (var i = 1; i < submeshFirstTriangle.Length; i++)
            {
                if (triangleIndex < submeshFirstTriangle[i])
                    break;
                layer = submeshLayers[i];
            }
            return layer;
        }

        public bool IsVisible => Renderer != null && Renderer.enabled && gameObject.activeInHierarchy;
    }
}
