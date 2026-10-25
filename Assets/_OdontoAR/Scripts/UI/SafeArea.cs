using UnityEngine;

namespace OdontoAR.UI
{
    /// <summary>
    /// Ajusta este RectTransform à área segura do iPhone (abaixo da Dynamic Island, acima do indicador
    /// de início). O pai deve ocupar a tela inteira.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screenSize;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != applied || screenSize.x != Screen.width || screenSize.y != Screen.height)
                Apply();
        }

        void Apply()
        {
            applied = Screen.safeArea;
            screenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
            rect.anchorMax = new Vector2(applied.xMax / Screen.width, applied.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
