using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OdontoAR.UI
{
    /// <summary>Diz se um ponto da tela está sobre a interface (para gestos não atravessarem botões).</summary>
    public static class UIHitTest
    {
        static readonly List<RaycastResult> s_Results = new();

        public static bool IsOverUI(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var pointer = new PointerEventData(eventSystem) { position = screenPosition };
            s_Results.Clear();
            eventSystem.RaycastAll(pointer, s_Results);
            return s_Results.Count > 0;
        }
    }
}
