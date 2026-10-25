using UnityEngine;

namespace OdontoAR.AR
{
    /// <summary>Retículo pulsa 8% a cada 1,8 s (design system, componente Reticulo).</summary>
    public sealed class ReticlePulse : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.08f;
        [SerializeField] float periodSeconds = 1.8f;

        void Update()
        {
            var phase = Mathf.Sin(Time.time * Mathf.PI * 2f / periodSeconds) * 0.5f + 0.5f;
            transform.localScale = Vector3.one * (1f + amplitude * phase);
        }
    }
}
