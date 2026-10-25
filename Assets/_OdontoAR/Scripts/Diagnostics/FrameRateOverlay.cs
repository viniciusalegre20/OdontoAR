using OdontoAR.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace OdontoAR.Diagnostics
{
    /// <summary>
    /// T11: contador de FPS na tela para a verificação (RNF02, meta ≥ 30). Aparece e some tocando com
    /// 3 dedos ao mesmo tempo; mostra a média e o pior quadro de cada meio segundo.
    /// </summary>
    public sealed class FrameRateOverlay : MonoBehaviour
    {
        [SerializeField] GameObject overlay;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] float intervalSeconds = 0.5f;
        [SerializeField] float targetFps = 30f;

        float elapsed;
        int frames;
        float worstFrame;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
        }

        void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            EnhancedTouchSupport.Disable();
        }

        void OnFingerDown(Finger finger)
        {
            if (Touch.activeFingers.Count == 3)
                overlay.SetActive(!overlay.activeSelf);
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
                label.text = $"{average:0} FPS · mín {minimum:0}";
                label.color = minimum >= targetFps ? OdontoARTheme.Accent : OdontoARTheme.Danger;
            }

            elapsed = 0f;
            frames = 0;
            worstFrame = 0f;
        }
    }
}
