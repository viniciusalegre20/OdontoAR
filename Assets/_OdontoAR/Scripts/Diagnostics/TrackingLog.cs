using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OdontoAR.Diagnostics
{
    /// <summary>
    /// Registra no log (devicectl --console) cada mudança do estado do rastreamento do ARKit e o motivo
    /// quando ele fica limitado (pouca textura, movimento rápido, iniciando, relocalizando), para a
    /// verificação de estabilidade (RNF03).
    /// </summary>
    public sealed class TrackingLog : MonoBehaviour
    {
        [SerializeField] Camera arCamera;

        ARSessionState lastState = (ARSessionState)(-1);
        NotTrackingReason lastReason = (NotTrackingReason)(-1);
        float since;

        /// <summary>Usado pelo menu OdontoAR/Criar cena AR.</summary>
        public void SetCamera(Camera camera) => arCamera = camera;

        void Update()
        {
            var state = ARSession.state;
            var reason = ARSession.notTrackingReason;
            if (state == lastState && reason == lastReason)
                return;

            var t = arCamera.transform;
            Debug.Log($"[OdontoAR] Rastreamento: sessão {state}, motivo {reason} " +
                      $"(após {Time.realtimeSinceStartup - since:F1} s) · câmera em {t.position:F2}, " +
                      $"olhando {t.forward:F2}");
            lastState = state;
            lastReason = reason;
            since = Time.realtimeSinceStartup;
        }
    }
}
