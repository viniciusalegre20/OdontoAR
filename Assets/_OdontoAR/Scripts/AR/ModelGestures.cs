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
    /// RF04/RF05: gira a arcada em qualquer direção e amplia. Só age com a arcada posicionada.
    /// Um dedo: na horizontal gira sobre a mesa (eixo vertical); na vertical inclina em torno do eixo
    /// horizontal da tela, sem limite (dá para ver por cima, por baixo, pela face oclusal).
    /// Dois dedos: pinça amplia/reduz; girar os dois dedos roda a arcada em torno da linha de visão.
    /// </summary>
    public sealed class ModelGestures : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] Camera arCamera;

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
                Drag(state, touches[0]);
            else if (touches.Count >= 2)
                TwoFingers(state, touches[0], touches[1]);

#if UNITY_EDITOR
            // A simulação de toque do Editor não faz pinça; a roda do mouse substitui.
            var scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (scroll > 0f)
                state.ScaleBy(editorScrollStep);
            else if (scroll < 0f)
                state.ScaleBy(1f / editorScrollStep);
#endif
        }

        /// <summary>Eixo do mundo no espaço do objeto posicionado, onde fica a rotação do estado.</summary>
        Vector3 ToModelSpace(Vector3 worldAxis) => Quaternion.Inverse(holder.transform.rotation) * worldAxis;

        void Drag(ModelState state, Touch touch)
        {
            if (touch.phase != TouchPhase.Moved || UIHitTest.IsOverUI(touch.startScreenPosition))
                return;

            var degreesPerPixel = degreesPerScreenWidth / Screen.width;

            // Arrastar para a direita leva a face voltada ao aluno para a direita (giro sobre a mesa);
            // arrastar para cima leva essa face para cima (inclinação em torno do eixo horizontal da tela).
            var turn = Quaternion.AngleAxis(-touch.delta.x * degreesPerPixel, ToModelSpace(Vector3.up));
            var tilt = Quaternion.AngleAxis(touch.delta.y * degreesPerPixel, ToModelSpace(arCamera.transform.right));
            state.RotateBy(turn * tilt);
        }

        void TwoFingers(ModelState state, Touch a, Touch b)
        {
            if (a.phase != TouchPhase.Moved && b.phase != TouchPhase.Moved)
                return;
            if (UIHitTest.IsOverUI(a.startScreenPosition) || UIHitTest.IsOverUI(b.startScreenPosition))
                return;

            var current = b.screenPosition - a.screenPosition;
            var previous = (b.screenPosition - b.delta) - (a.screenPosition - a.delta);
            if (previous.magnitude < 1f || current.magnitude < 1f)
                return;

            state.ScaleBy(current.magnitude / previous.magnitude);

            // Girar os dedos no sentido anti-horário roda a arcada no mesmo sentido, como o aluno a vê
            // (no Unity, giro positivo em torno da direção do olhar aparece anti-horário na tela).
            var twist = Vector2.SignedAngle(previous, current);
            if (Mathf.Abs(twist) > 0.05f)
                state.RotateBy(Quaternion.AngleAxis(twist, ToModelSpace(arCamera.transform.forward)));
        }
    }
}
