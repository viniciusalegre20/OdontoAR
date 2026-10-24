using UnityEngine;

namespace OdontoAR.Diagnostics
{
    /// <summary>
    /// Registra no log o FPS médio e o mínimo a cada intervalo, para conferir o requisito de FPS ≥ 30
    /// no aparelho (console via devicectl).
    /// </summary>
    public sealed class FrameRateMonitor : MonoBehaviour
    {
        [SerializeField] float intervalSeconds = 5f;

        float elapsed;
        int frames;
        float worstFrame;

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            elapsed += dt;
            frames++;
            worstFrame = Mathf.Max(worstFrame, dt);

            if (elapsed < intervalSeconds)
                return;

            Debug.Log($"[OdontoAR] FPS médio {frames / elapsed:F1}, mínimo {1f / worstFrame:F1}");
            elapsed = 0f;
            frames = 0;
            worstFrame = 0f;
        }
    }
}
