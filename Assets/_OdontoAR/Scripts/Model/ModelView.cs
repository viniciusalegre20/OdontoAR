using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// Aplica rotação e escala do <see cref="ModelState"/> neste transform (o pivô "Vista" do prefab).
    /// O pivô fica no centro do modelo e sobe junto com a escala, para a arcada continuar apoiada na mesa.
    /// </summary>
    public sealed class ModelView : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;

        [Tooltip("Meia altura do modelo em tamanho real (m). Preenchido pelo menu OdontoAR/Criar cena AR.")]
        [SerializeField] float baseHalfHeight;

        [Tooltip("Sombra de contato na mesa (irmã deste objeto): acompanha a escala e o giro, não a inclinação.")]
        [SerializeField] Transform contactShadow;

        [Tooltip("Tamanho da sombra em tamanho real (m). Preenchido pelo menu OdontoAR/Criar cena AR.")]
        [SerializeField] Vector2 contactShadowBaseSize;

        void OnEnable()
        {
            holder.State.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            holder.State.Changed -= Apply;
        }

        void Apply()
        {
            var state = holder.State;

            // Inclinação no espaço do pai, depois do giro: arrastar para cima sempre inclina
            // "para o aluno", qualquer que seja o giro atual.
            transform.localRotation = Quaternion.AngleAxis(state.Pitch, Vector3.right)
                                      * Quaternion.AngleAxis(state.Yaw, Vector3.up);
            transform.localScale = Vector3.one * state.Scale;
            transform.localPosition = new Vector3(0f, baseHalfHeight * state.Scale, 0f);

            if (contactShadow != null)
            {
                // O quad fica deitado na mesa (90° em X) e gira junto com a arcada em torno da vertical.
                contactShadow.localRotation = Quaternion.AngleAxis(state.Yaw, Vector3.up)
                                              * Quaternion.Euler(90f, 0f, 0f);
                contactShadow.localScale = new Vector3(contactShadowBaseSize.x, contactShadowBaseSize.y, 1f) * state.Scale;
            }
        }
    }
}
