using System.Collections.Generic;
using OdontoAR.Model;
using OdontoAR.UI;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace OdontoAR.AR
{
    /// <summary>
    /// RF09: com a arcada posicionada, um toque curto (sem arrastar, para não conflitar com o giro)
    /// seleciona a estrutura tocada; tocar fora dela limpa a seleção. A estrutura selecionada ganha
    /// contorno ciano; o esmaecimento do resto fica com o LayerController.
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] Camera arCamera;
        [SerializeField] Material outlineMaterial;

        [Tooltip("Todas as estruturas selecionáveis do modelo.")]
        [SerializeField] SelectableStructure[] structures;

        [SerializeField] float maxTapSeconds = 0.35f;
        [SerializeField] float maxTapMovementPixels = 30f;

        [Tooltip("Um dente até esta distância atrás do osso ou do nervo tocado ganha a preferência (m, em tamanho real): " +
                 "o osso translúcido envolve as raízes, que continuam visíveis por dentro dele.")]
        [SerializeField] float toothBehindDepth = 0.015f;

        const float MaxRayDistance = 10f;

        static readonly RaycastHit[] s_Hits = new RaycastHit[32];

        readonly List<GameObject> outlines = new();
        readonly List<SelectableStructure> selected = new();
        string outlinedKey;

        /// <summary>Caixa da estrutura selecionada no mundo, para o cartão se posicionar.</summary>
        public bool TryGetSelectionBounds(out Bounds bounds)
        {
            bounds = default;
            var any = false;
            foreach (var structure in selected)
            {
                if (!structure.IsVisible)
                    continue;
                if (any)
                    bounds.Encapsulate(structure.Renderer.bounds);
                else
                    bounds = structure.Renderer.bounds;
                any = true;
            }
            return any;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerUp += OnFingerUp;
            holder.State.Changed += RefreshOutline;
            RefreshOutline();
        }

        void OnDisable()
        {
            Touch.onFingerUp -= OnFingerUp;
            holder.State.Changed -= RefreshOutline;
            EnhancedTouchSupport.Disable();
        }

        void OnFingerUp(Finger finger)
        {
            var state = holder.State;
            if (!state.IsPlaced || Touch.activeFingers.Count > 1)
                return;

            var touch = finger.lastTouch;
            var duration = touch.time - touch.startTime;
            var movement = (touch.screenPosition - touch.startScreenPosition).magnitude;
            if (duration > maxTapSeconds || movement > maxTapMovementPixels)
                return;
            if (UIHitTest.IsOverUI(touch.startScreenPosition))
                return;

            if (TryPick(touch.screenPosition, out var structure, out var layer))
                state.Select(structure.Key, layer);
            else
                state.ClearSelection();
        }

        bool TryPick(Vector2 screenPoint, out SelectableStructure picked, out Layer pickedLayer)
        {
            picked = null;
            pickedLayer = default;

            // Os colliders acompanham a Vista (giro/escala) só depois de sincronizados.
            Physics.SyncTransforms();
            var ray = arCamera.ScreenPointToRay(screenPoint);
            var count = Physics.RaycastNonAlloc(ray, s_Hits, MaxRayDistance);

            var state = holder.State;
            var nearest = float.MaxValue;
            var nearestTooth = float.MaxValue;
            SelectableStructure tooth = null;
            var toothLayer = default(Layer);

            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                var structure = hit.collider.GetComponent<SelectableStructure>();
                if (structure == null || !structure.IsVisible)
                    continue;

                // Coroa oculta com a raiz visível: o collider é a malha inteira, mas a coroa não aparece.
                var layer = structure.LayerAt(hit.triangleIndex);
                if (!state.IsLayerVisible(layer))
                    continue;

                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    picked = structure;
                    pickedLayer = layer;
                }
                if (structure.IsTooth && hit.distance < nearestTooth)
                {
                    nearestTooth = hit.distance;
                    tooth = structure;
                    toothLayer = layer;
                }
            }

            if (picked != null && !picked.IsTooth && tooth != null
                && nearestTooth - nearest <= toothBehindDepth * state.Scale)
            {
                picked = tooth;
                pickedLayer = toothLayer;
            }

            return picked != null;
        }

        void LateUpdate()
        {
            // Estrutura que sumiu (arcada ou camada desligada) deixa de estar selecionada.
            var state = holder.State;
            if (state.HasSelection && (!state.IsLayerVisible(state.SelectedLayer) || !TryGetSelectionBounds(out _)))
                state.ClearSelection();
        }

        void RefreshOutline()
        {
            var key = holder.State.SelectedKey;
            if (key == outlinedKey)
                return;

            foreach (var outline in outlines)
                Destroy(outline);
            outlines.Clear();
            selected.Clear();
            outlinedKey = key;

            if (key == null)
                return;

            foreach (var structure in structures)
            {
                if (structure.Key != key)
                    continue;

                selected.Add(structure);
                outlines.Add(CreateOutline(structure));
            }
        }

        GameObject CreateOutline(SelectableStructure structure)
        {
            var mesh = structure.GetComponent<MeshFilter>().sharedMesh;
            var go = new GameObject("Contorno", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(structure.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.GetComponent<MeshRenderer>();
            var materials = new Material[mesh.subMeshCount];
            for (var i = 0; i < materials.Length; i++)
                materials[i] = outlineMaterial;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }
    }
}
