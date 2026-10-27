using System.Collections.Generic;
using OdontoAR.Model;
using OdontoAR.UI;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace OdontoAR.AR
{
    /// <summary>
    /// RF01/RF02: enquanto a arcada não está posicionada, uma mira acompanha o centro da tela sobre
    /// a superfície horizontal detectada; um toque curto em qualquer lugar coloca a arcada na mira,
    /// de frente para o aluno e ancorada à mesa. Depois disso os toques ficam livres para os gestos
    /// (e, na T7, para a seleção); "Reposicionar" volta a este modo.
    /// </summary>
    public sealed class ARTapToPlace : MonoBehaviour
    {
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ARAnchorManager anchorManager;
        [SerializeField] ModelStateHolder holder;
        [SerializeField] Camera arCamera;

        [Tooltip("Mira mostrada sobre a mesa, onde a arcada será colocada.")]
        [SerializeField] Transform reticle;

        [SerializeField] float maxTapSeconds = 0.35f;
        [SerializeField] float maxTapMovementPixels = 30f;

        [Tooltip("Quanto a normal da superfície pode se afastar da vertical (graus).")]
        [SerializeField] float maxSurfaceTiltDegrees = 15f;

        [Tooltip("A superfície precisa estar pelo menos isto abaixo da câmera (m), para ignorar tetos.")]
        [SerializeField] float minDistanceBelowCamera = 0.1f;

        [Tooltip("Fora do contorno já mapeado, a mira segue o plano da mesa até esta distância da câmera (m).")]
        [SerializeField] float maxExtendedDistance = 2f;

        [Tooltip("Quanto o ARKit pode corrigir a altura da mesa depois de posicionar (m); além disso é outra superfície.")]
        [SerializeField] float maxHeightCorrection = 0.05f;

        [Tooltip("Rapidez com que a arcada acompanha as correções de altura da mesa (1/s).")]
        [SerializeField] float heightFollowRate = 8f;

        [Tooltip("Tempo sem a âncora (s) até concluir que o ARKit reiniciou o mundo e pedir para posicionar de novo.")]
        [SerializeField] float anchorLostSeconds = 1f;

        static readonly List<ARRaycastHit> s_Hits = new();

        Transform ModelRoot => holder.transform;

        /// <summary>A mira está sobre uma superfície: um toque posiciona a arcada (telas 4 e 5).</summary>
        public bool HasTarget => hasTarget;

        bool hasTarget;
        Pose targetPose;
        ARAnchor anchor;
        bool anchored;
        float anchorLostFor;
        float tableHeight;
        int placementId;

        /// <summary>Quantas vezes o ARKit reiniciou o mundo com a arcada na mesa (para o status e o log).</summary>
        public int TrackingResets { get; private set; }

        /// <summary>Instante (Time.unscaledTime) do último reinício, para a interface avisar.</summary>
        public float LastTrackingResetTime { get; private set; } = float.NegativeInfinity;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            // No Editor, o mouse vira toque para testar sem aparelho.
            TouchSimulation.Enable();
#endif
            Touch.onFingerUp += OnFingerUp;
            MultiTouch.Track();
            holder.State.Changed += ApplyPlacedState;
            ApplyPlacedState();
        }

        void OnDisable()
        {
            Touch.onFingerUp -= OnFingerUp;
            MultiTouch.Untrack();
            holder.State.Changed -= ApplyPlacedState;
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
            EnhancedTouchSupport.Disable();
        }

        /// <summary>Ligado ao botão "Reposicionar".</summary>
        public void Reposition()
        {
            holder.State.SetPlaced(false);
        }

        void Update()
        {
            if (holder.State.IsPlaced)
                return;

            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            hasTarget = TryFindSurface(screenCenter, out targetPose);

            reticle.gameObject.SetActive(hasTarget);
            if (hasTarget)
                reticle.SetPositionAndRotation(targetPose.position, targetPose.rotation);
        }

        bool TryFindSurface(Vector2 screenPoint, out Pose pose)
        {
            pose = default;

            // Só planos já detectados: o "plano estimado" do ARKit (sem plano ainda) erra a altura em
            // alguns centímetros e a arcada flutuava ou afundava na mesa, e parecia escorregar ao andar.
            // Dentro do contorno mapeado vale primeiro; fora dele, segue a altura do mesmo plano, o que
            // mostra a mira antes de o ARKit cobrir a mesa inteira.
            if (TryFindSurface(screenPoint, TrackableType.PlaneWithinPolygon, float.PositiveInfinity, out pose))
                return true;
            return TryFindSurface(screenPoint, TrackableType.PlaneWithinInfinity, maxExtendedDistance, out pose);
        }

        bool TryFindSurface(Vector2 screenPoint, TrackableType types, float maxDistance, out Pose pose)
        {
            pose = default;
            if (!raycastManager.Raycast(screenPoint, s_Hits, types))
                return false;

            var cameraHeight = arCamera.transform.position.y;

            // Os acertos vêm do mais próximo para o mais distante: fica com a primeira superfície
            // horizontal, voltada para cima e abaixo da câmera (a mesa, não teto nem parede).
            foreach (var hit in s_Hits)
            {
                if (hit.distance > maxDistance)
                    break;
                if (!IsTable(hit, cameraHeight))
                    continue;

                pose = new Pose(hit.pose.position, FacingCamera(hit.pose.position));
                return true;
            }

            return false;
        }

        bool IsTable(ARRaycastHit hit, float cameraHeight)
        {
            var minUpDot = Mathf.Cos(maxSurfaceTiltDegrees * Mathf.Deg2Rad);
            return Vector3.Dot(hit.pose.up, Vector3.up) >= minUpDot
                   && hit.pose.position.y <= cameraHeight - minDistanceBelowCamera;
        }

        void OnFingerUp(Finger finger)
        {
            if (holder.State.IsPlaced || !hasTarget)
                return;

            var touch = finger.lastTouch;
            var duration = touch.time - touch.startTime;
            var movement = (touch.screenPosition - touch.startScreenPosition).magnitude;
            if (duration > maxTapSeconds || movement > maxTapMovementPixels)
                return;

            if (MultiTouch.Overlaps(touch))
                return; // parte de uma pinça ou do gesto de 3 dedos
            if (UIHitTest.IsOverUI(touch.startScreenPosition))
                return;

            Place(targetPose);
        }

        async void Place(Pose pose)
        {
            ModelRoot.SetPositionAndRotation(pose.position, pose.rotation);
            tableHeight = pose.position.y;
            anchorLostFor = 0f;
            holder.State.SetPlaced(true);
            var id = ++placementId;

            // Âncora de mundo, não presa ao plano: o ARKit a corrige quando refina o mapa e ela não some
            // quando dois planos da mesa se fundem (a âncora presa sumia junto e parecia um reinício).
            // A altura da mesa vem do plano, que continua sendo refinado (LateUpdate).
            var result = await anchorManager.TryAddAnchorAsync(pose);
            if (id != placementId || !holder.State.IsPlaced)
            {
                // "Reposicionar" durante a espera: esta âncora já não serve.
                if (result.status.IsSuccess())
                    Destroy(result.value.gameObject);
                return;
            }

            anchor = result.status.IsSuccess() ? result.value : null;
            anchored = anchor != null;
            if (!anchored)
                Debug.LogWarning("[OdontoAR] Não foi possível criar a âncora; a arcada fica no lugar sem correção.");
        }

        void LateUpdate()
        {
            if (!holder.State.IsPlaced || !anchored)
                return;

            // Com pouca textura (InsufficientFeatures) o ARKit pode reiniciar o mundo: cria coordenadas novas a
            // partir de onde o iPhone está e descarta as âncoras. A arcada ficaria num endereço que não é mais a
            // mesa (fora do lugar, "andando com a câmera"); então volta para "Procurando superfície…".
            if (anchor == null || anchor.trackingState == TrackingState.None)
            {
                anchorLostFor += Time.unscaledDeltaTime;
                if (anchorLostFor >= anchorLostSeconds)
                    OnTrackingReset();
                return;
            }

            anchorLostFor = 0f;
            if (anchor.trackingState != TrackingState.Tracking)
                return;

            var position = anchor.transform.position;
            if (TryTableHeightBelow(position, out var height))
            {
                // Suave: o ARKit corrige a altura do plano aos poucos, e um salto de 1 cm aparece.
                var t = 1f - Mathf.Exp(-heightFollowRate * Time.unscaledDeltaTime);
                tableHeight = Mathf.Lerp(tableHeight, height, t);
            }
            ModelRoot.position = new Vector3(position.x, tableHeight, position.z);
            LogPlacement();
        }

        float nextPlacementLog;

        /// <summary>
        /// Diagnóstico da deriva (T11): se a posição da arcada muda com o iPhone parado na mão, quem a move é
        /// a âncora ou a altura do plano; se ela fica fixa e a arcada "nada" na tela, é o rastreamento da câmera.
        /// </summary>
        void LogPlacement()
        {
            if (Time.unscaledTime < nextPlacementLog)
                return;
            nextPlacementLog = Time.unscaledTime + 1f;

            var cam = arCamera.transform;
            Debug.Log($"[OdontoAR] Posição: arcada {ModelRoot.position:F3}, âncora {anchor.transform.position:F3} " +
                      $"({anchor.trackingState}), mesa {tableHeight:F3}, câmera {cam.position:F3}, " +
                      $"sessão {ARSession.state}/{ARSession.notTrackingReason}, planos {planeManager.trackables.count}, " +
                      $"FOV {arCamera.fieldOfView:F1}");
        }

        /// <summary>
        /// Altura atual do plano da mesa sob a arcada. Procura de cima para baixo numa faixa estreita em torno
        /// da altura atual, para não pular para o chão nem para outro móvel.
        /// </summary>
        bool TryTableHeightBelow(Vector3 position, out float height)
        {
            height = 0f;
            var origin = new Vector3(position.x, tableHeight + maxHeightCorrection, position.z);
            if (!raycastManager.Raycast(new Ray(origin, Vector3.down), s_Hits, TrackableType.PlaneWithinInfinity))
                return false;

            var cameraHeight = arCamera.transform.position.y;
            foreach (var hit in s_Hits)
            {
                if (hit.distance > 2f * maxHeightCorrection)
                    break;
                if (!IsTable(hit, cameraHeight))
                    continue;

                height = hit.pose.position.y;
                return true;
            }

            return false;
        }

        void OnTrackingReset()
        {
            TrackingResets++;
            LastTrackingResetTime = Time.unscaledTime;
            Debug.Log($"[OdontoAR] Âncora perdida: o ARKit reiniciou o rastreamento ({TrackingResets}ª vez). " +
                      "Pedindo para posicionar de novo.");
            holder.State.SetPlaced(false);
        }

        /// <summary>Frente do modelo (+Z) voltada para a câmera, sem inclinar.</summary>
        Quaternion FacingCamera(Vector3 position)
        {
            var toCamera = arCamera.transform.position - position;
            toCamera.y = 0f;
            return toCamera.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                : Quaternion.identity;
        }

        void ReleaseAnchor()
        {
            anchored = false;
            if (anchor == null)
                return; // o ARKit já a descartou

            Destroy(anchor.gameObject);
            anchor = null;
        }

        void ApplyPlacedState()
        {
            var placed = holder.State.IsPlaced;
            if (!placed)
            {
                placementId++; // descarta uma âncora ainda sendo criada
                ReleaseAnchor();
            }

            ModelRoot.gameObject.SetActive(placed);
            reticle.gameObject.SetActive(false);
            hasTarget = false;

            // A detecção de planos continua com a arcada na mesa (os planos são invisíveis): é ela que
            // corrige a altura da mesa enquanto o aluno anda em volta.
            planeManager.enabled = true;
        }
    }
}
