using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// Dono da instância de <see cref="ModelState"/> na cena. Fica na raiz do prefab Arcadas;
    /// os demais componentes leem e escrevem o estado por aqui.
    /// </summary>
    public sealed class ModelStateHolder : MonoBehaviour
    {
        public ModelState State { get; } = new ModelState();

        /// <summary>Ligado ao botão "Restaurar vista".</summary>
        public void ResetView() => State.ResetView();
    }
}
