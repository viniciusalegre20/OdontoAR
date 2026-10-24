using OdontoAR.Model;
using OdontoAR.UI;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace OdontoAR.AR
{
    /// <summary>
    /// RF04/RF05: um dedo arrastando gira a arcada (horizontal = giro, vertical = inclinação);
    /// dois dedos em pinça ampliam/reduzem. Só age com a arcada posicionada.
    /// </summary>
    public sealed class ModelGestures : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;

        [Tooltip("Graus de giro ao arrastar a largura inteira da tela.")]
        [SerializeField] float degreesPerScreenWidth = 270f;

#if UNITY_EDITOR
        [Tooltip("Só no Editor: fator de escala por passo da roda do mouse.")]
        [SerializeField] float editorScrollStep = 1.1f;
#endif

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            var state = holder.State;
            if (!state.IsPlaced)
                return;

            var touches = Touch.activeTouches;
            if (touches.Count == 1)
                Rotate(state, touches[0]);
            else if (touches.Count >= 2)
                Pinch(state, touches[0], touches[1]);

#if UNITY_EDITOR
            // A simulação de toque do Editor não faz pinça; a roda do mouse substitui.
            var scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (scroll > 0f)
                state.ScaleBy(editorScrollStep);
            else if (scroll < 0f)
                state.ScaleBy(1f / editorScrollStep);
#endif
        }

        void Rotate(ModelState state, Touch touch)
        {
            if (touch.phase != TouchPhase.Moved || UIHitTest.IsOverUI(touch.startScreenPosition))
                return;

            var degreesPerPixel = degreesPerScreenWidth / Screen.width;
            // Arrastar para a direita leva a face voltada ao aluno para a direita;
            // arrastar para cima inclina essa face para cima.
            state.Rotate(-touch.delta.x * degreesPerPixel, -touch.delta.y * degreesPerPixel);
        }

        static void Pinch(ModelState state, Touch a, Touch b)
        {
            if (a.phase != TouchPhase.Moved && b.phase != TouchPhase.Moved)
                return;

            var current = Vector2.Distance(a.screenPosition, b.screenPosition);
            var previous = Vector2.Distance(a.screenPosition - a.delta, b.screenPosition - b.delta);
            if (previous < 1f)
                return;

            state.ScaleBy(current / previous);
        }
    }
}
