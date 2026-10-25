using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// Aplica rotação e escala do <see cref="ModelState"/> neste transform (o pivô "Vista" do prefab).
    /// O pivô fica no centro do modelo e sobe o quanto for preciso para a caixa do modelo, já girada e
    /// escalada, continuar apoiada na mesa (inclinada, a arcada não afunda nela).
    /// </summary>
    public sealed class ModelView : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;

        [Tooltip("Meias medidas da caixa do modelo em tamanho real (m), centrada no pivô. " +
                 "Preenchido pelo menu OdontoAR/Criar cena AR.")]
        [SerializeField] Vector3 baseExtents;

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

            transform.localRotation = state.Rotation;
            transform.localScale = Vector3.one * state.Scale;
            // Ponto mais baixo da caixa girada: Σ |R_yj| · e_j (para uma caixa centrada na origem).
            var r = transform.localRotation;
            var lift = Mathf.Abs((r * Vector3.right).y) * baseExtents.x
                       + Mathf.Abs((r * Vector3.up).y) * baseExtents.y
                       + Mathf.Abs((r * Vector3.forward).y) * baseExtents.z;
            transform.localPosition = new Vector3(0f, lift * state.Scale, 0f);

            if (contactShadow != null)
            {
                // O quad fica deitado na mesa (90° em X) e acompanha o giro da arcada em torno da vertical.
                contactShadow.localRotation = Quaternion.AngleAxis(Heading(state.Rotation), Vector3.up)
                                              * Quaternion.Euler(90f, 0f, 0f);
                contactShadow.localScale = new Vector3(contactShadowBaseSize.x, contactShadowBaseSize.y, 1f) * state.Scale;
            }
        }

        /// <summary>Para onde a arcada aponta no plano da mesa (graus); de pé, usa o topo dela.</summary>
        static float Heading(Quaternion rotation)
        {
            var forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = rotation * Vector3.up;
                forward.y = 0f;
            }
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }
    }
}
