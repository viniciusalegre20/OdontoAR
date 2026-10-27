using OdontoAR.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace OdontoAR.Diagnostics
{
    /// <summary>
    /// T11: contador de FPS na tela para a verificação (RNF02, meta ≥ 30). Aparece e some tocando com
    /// 3 dedos ao mesmo tempo; mostra a média e o pior quadro de cada meio segundo.
    /// Tocar com 4 dedos liga e desliga a oclusão do LiDAR, para comparar a estabilidade da arcada na mesa.
    /// </summary>
    public sealed class FrameRateOverlay : MonoBehaviour
    {
        [SerializeField] GameObject overlay;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] AROcclusionManager occlusion;
        [SerializeField] float intervalSeconds = 0.5f;
        [SerializeField] float targetFps = 30f;

        float elapsed;
        int frames;
        float worstFrame;
        int gestureFingers;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerUp += OnFingerUp;
        }

        void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
        }

        void OnFingerDown(Finger finger)
        {
            gestureFingers = Mathf.Max(gestureFingers, Touch.activeFingers.Count);
        }

        // Decide quando o último dedo sai: assim os 3 primeiros dedos de um toque com 4 não ligam o FPS.
        void OnFingerUp(Finger finger)
        {
            if (Touch.activeFingers.Count > 1)
                return;

            if (gestureFingers == 3)
                overlay.SetActive(!overlay.activeSelf);
            else if (gestureFingers == 4 && occlusion != null)
            {
                occlusion.enabled = !occlusion.enabled;
                overlay.SetActive(true);
                Debug.Log($"[OdontoAR] Oclusão {(occlusion.enabled ? "ligada" : "desligada")} (toque com 4 dedos).");
            }
            gestureFingers = 0;
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            elapsed += dt;
            frames++;
            worstFrame = Mathf.Max(worstFrame, dt);
            if (elapsed < intervalSeconds)
                return;

            if (overlay.activeSelf)
            {
                var average = frames / elapsed;
                var minimum = worstFrame > 0f ? 1f / worstFrame : average;
                var occlusionText = occlusion != null && occlusion.enabled ? "oclusão sim" : "oclusão não";
                label.text = $"{average:0} FPS · mín {minimum:0} · {occlusionText}";
                label.color = minimum >= targetFps ? OdontoARTheme.Accent : OdontoARTheme.Danger;
            }

            elapsed = 0f;
            frames = 0;
            worstFrame = 0f;
        }
    }
}
