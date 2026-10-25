using OdontoAR.AR;
using OdontoAR.Model;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OdontoAR.UI
{
    /// <summary>
    /// Pílulas sobre a câmera (telas 4–6): "Procurando superfície…", "Superfície detectada",
    /// "Rastreamento instável" e a dica de gestos, que some depois do primeiro gesto.
    /// </summary>
    public sealed class ARStatus : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] ARTapToPlace tapToPlace;
        [SerializeField] GameObject searching;
        [SerializeField] GameObject found;
        [SerializeField] GameObject unstable;
        [SerializeField] GameObject gestureHint;
        [SerializeField] RectTransform spinner;

        [Tooltip("Voltas por segundo do indicador de busca.")]
        [SerializeField] float spinnerSpeed = 1f;

        bool gestured;

        void OnEnable()
        {
            holder.State.Changed += OnStateChanged;
        }

        void OnDisable()
        {
            holder.State.Changed -= OnStateChanged;
        }

        // Girar ou ampliar conta como primeiro gesto: a dica não volta mais nesta execução.
        void OnStateChanged()
        {
            var state = holder.State;
            if (state.IsPlaced && (state.Yaw != 0f || state.Pitch != 0f || state.Scale != ModelState.DefaultScale))
                gestured = true;
        }

        void Update()
        {
            var state = holder.State;
            var placed = state.IsPlaced;
            var target = tapToPlace.HasTarget;

            searching.SetActive(!placed && !target);
            found.SetActive(!placed && target);
            unstable.SetActive(placed && ARSession.notTrackingReason != NotTrackingReason.None);
            gestureHint.SetActive(placed && !gestured && !state.HasSelection);

            if (searching.activeSelf)
                spinner.Rotate(0f, 0f, -360f * spinnerSpeed * Time.deltaTime);
        }
    }
}
