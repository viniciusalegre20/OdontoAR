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
    /// de frente para o aluno e ancorada ao plano. Depois disso os toques ficam livres para os gestos
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

        [Tooltip("Tempo sem a âncora (s) até concluir que o ARKit reiniciou o mundo e pedir para posicionar de novo.")]
        [SerializeField] float anchorLostSeconds = 1f;

        const TrackableType RaycastTypes = TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated;

        static readonly List<ARRaycastHit> s_Hits = new();

        Transform ModelRoot => holder.transform;

        /// <summary>A mira está sobre uma superfície: um toque posiciona a arcada (telas 4 e 5).</summary>
        public bool HasTarget => hasTarget;

        bool hasTarget;
        Pose targetPose;
        ARPlane targetPlane;
        ARAnchor anchor;
        bool anchored;
        float anchorLostFor;
        Vector3 anchorOffset;

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
            hasTarget = TryFindSurface(screenCenter, out targetPose, out targetPlane);

            reticle.gameObject.SetActive(hasTarget);
            if (hasTarget)
                reticle.SetPositionAndRotation(targetPose.position, targetPose.rotation);
        }

        bool TryFindSurface(Vector2 screenPoint, out Pose pose, out ARPlane plane)
        {
            pose = default;
            plane = null;

            if (!raycastManager.Raycast(screenPoint, s_Hits, RaycastTypes))
                return false;

            var cameraHeight = arCamera.transform.position.y;
            var minUpDot = Mathf.Cos(maxSurfaceTiltDegrees * Mathf.Deg2Rad);

            // Os acertos vêm do mais próximo para o mais distante: fica com a primeira superfície
            // horizontal, voltada para cima e abaixo da câmera (a mesa, não teto nem parede).
            foreach (var hit in s_Hits)
            {
                var hitPose = hit.pose;
                if (Vector3.Dot(hitPose.up, Vector3.up) < minUpDot)
                    continue;
                if (hitPose.position.y > cameraHeight - minDistanceBelowCamera)
                    continue;

                pose = new Pose(hitPose.position, FacingCamera(hitPose.position));
                plane = hit.trackable as ARPlane;
                return true;
            }

            return false;
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

            Place(targetPose, targetPlane);
        }

        void Place(Pose pose, ARPlane plane)
        {
            // A âncora no plano acompanha os ajustes que o ARKit faz na altura da mesa. A arcada copia
            // só a posição dela (LateUpdate): como filha, herdava a orientação do plano e tombava.
            if (plane != null)
                anchor = anchorManager.AttachAnchor(plane, pose);
            anchored = anchor != null;
            anchorLostFor = 0f;

            ModelRoot.SetPositionAndRotation(pose.position, pose.rotation);
            anchorOffset = anchor != null ? pose.position - anchor.transform.position : Vector3.zero;
            holder.State.SetPlaced(true);
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
            if (anchor.trackingState == TrackingState.Tracking)
                ModelRoot.position = anchor.transform.position + anchorOffset;
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
                ReleaseAnchor();

            ModelRoot.gameObject.SetActive(placed);
            reticle.gameObject.SetActive(false);
            hasTarget = false;

            // Com a arcada na mesa, a detecção de planos para (os planos já são invisíveis).
            planeManager.enabled = !placed;
        }
    }
}
