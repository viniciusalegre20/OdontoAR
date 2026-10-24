using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// RF03/RF06: mostra a arcada escolhida no <see cref="ModelState"/> e aplica a abertura da boca.
    /// A abertura gira a arcada superior para cima em torno da articulação temporomandibular: o movimento
    /// relativo é o mesmo de baixar a mandíbula, mas ela continua apoiada na mesa em vez de atravessá-la.
    /// </summary>
    public sealed class ArchController : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] GameObject upperArch;
        [SerializeField] GameObject lowerArch;

        [Tooltip("Pivô na altura dos côndilos da mandíbula; pai da arcada superior.")]
        [SerializeField] Transform jawHinge;

        [Tooltip("Ângulo da articulação com a abertura máxima (graus).")]
        [SerializeField] float maxOpeningDegrees = 25f;

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

            upperArch.SetActive(state.Arch != ArchSelection.Lower);
            lowerArch.SetActive(state.Arch != ArchSelection.Upper);

            // Com +Z para a frente, girar negativo em X levanta a frente da arcada.
            jawHinge.localRotation = Quaternion.AngleAxis(-state.Opening * maxOpeningDegrees, Vector3.right);
        }
    }
}
