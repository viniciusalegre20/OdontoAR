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
    /// RF01/RF02: enquanto a arcada não está posicionada, um toque curto sobre um plano detectado
    /// coloca o modelo ali, de frente para o aluno. Depois disso os toques ficam livres para os gestos
    /// (e, na T7, para a seleção); "Reposicionar" volta a este modo.
    /// </summary>
    public sealed class ARTapToPlace : MonoBehaviour
    {
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ModelStateHolder holder;
        [SerializeField] Camera arCamera;

        [Tooltip("Visível enquanto a arcada não foi posicionada (dica \"toque na mesa\").")]
        [SerializeField] GameObject placementHint;

        [Tooltip("Visível só com a arcada posicionada (Restaurar vista, Reposicionar).")]
        [SerializeField] GameObject placedControls;

        [SerializeField] float maxTapSeconds = 0.35f;
        [SerializeField] float maxTapMovementPixels = 30f;

        static readonly List<ARRaycastHit> s_Hits = new();

        Transform ModelRoot => holder.transform;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            // No Editor, o mouse vira toque para testar sem aparelho.
            TouchSimulation.Enable();
#endif
            Touch.onFingerUp += OnFingerUp;
            holder.State.Changed += ApplyPlacedState;
            ApplyPlacedState();
        }

        void OnDisable()
        {
            Touch.onFingerUp -= OnFingerUp;
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

        void OnFingerUp(Finger finger)
        {
            if (holder.State.IsPlaced)
                return;

            var touch = finger.lastTouch;
            var duration = touch.time - touch.startTime;
            var movement = (touch.screenPosition - touch.startScreenPosition).magnitude;
            if (duration > maxTapSeconds || movement > maxTapMovementPixels)
                return;

            if (UIHitTest.IsOverUI(touch.startScreenPosition))
                return;

            if (!raycastManager.Raycast(touch.screenPosition, s_Hits, TrackableType.PlaneWithinPolygon))
                return;

            Place(s_Hits[0].pose);
        }

        void Place(Pose pose)
        {
            // Frente do modelo (+Z) voltada para a câmera, sem inclinar.
            var toCamera = arCamera.transform.position - pose.position;
            toCamera.y = 0f;
            var rotation = toCamera.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                : pose.rotation;

            ModelRoot.SetPositionAndRotation(pose.position, rotation);
            holder.State.SetPlaced(true);
        }

        void ApplyPlacedState()
        {
            var placed = holder.State.IsPlaced;

            ModelRoot.gameObject.SetActive(placed);

            // Com a arcada na mesa, os planos somem e a detecção para (menos ruído visual e de CPU).
            planeManager.enabled = !placed;
            planeManager.SetTrackablesActive(!placed);

            if (placementHint != null)
                placementHint.SetActive(!placed);
            if (placedControls != null)
                placedControls.SetActive(placed);
        }
    }
}
