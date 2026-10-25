using OdontoAR.App;
using OdontoAR.Model;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace OdontoAR.UI
{
    public enum AppScreen
    {
        Opening,
        Code,
        Permission,
        AR,
    }

    /// <summary>
    /// Navegação entre as telas (fluxo do design system): Abertura → Código → Permissão → RA.
    /// A sessão de RA (e a câmera) só liga na tela de RA. Na segunda abertura, com código salvo e
    /// câmera permitida, o app vai direto para a RA.
    /// </summary>
    public sealed class ScreenRouter : MonoBehaviour
    {
        [SerializeField] GameObject opening;
        [SerializeField] GameObject code;
        [SerializeField] GameObject permission;
        [SerializeField] GameObject ar;
        [SerializeField] ARSession session;
        [SerializeField] ModelStateHolder holder;
        [SerializeField] HelpSheet help;

        public AppScreen Current { get; private set; }

        public static bool CameraAuthorized => CameraPermission.IsAuthorized;

        void Start()
        {
            var ready = ParticipantCode.IsValid(AppPrefs.ParticipantCode) && CameraAuthorized;
            Show(ready ? AppScreen.AR : AppScreen.Opening);
        }

        // Métodos sem parâmetro para os botões (ligados pelo menu OdontoAR/Criar cena AR).
        public void GoToOpening() => Show(AppScreen.Opening);
        public void GoToCode() => Show(AppScreen.Code);
        public void GoToPermission() => Show(AppScreen.Permission);
        public void GoToAR() => Show(AppScreen.AR);

        /// <summary>Depois do código: pede a câmera só se ainda não foi permitida.</summary>
        public void AfterCode() => Show(CameraAuthorized ? AppScreen.AR : AppScreen.Permission);

        public void Show(AppScreen screen)
        {
            if (Current == AppScreen.AR && screen != AppScreen.AR)
            {
                holder.State.SetPlaced(false);
                help.Close();
            }

            Current = screen;
            opening.SetActive(screen == AppScreen.Opening);
            code.SetActive(screen == AppScreen.Code);
            permission.SetActive(screen == AppScreen.Permission);
            ar.SetActive(screen == AppScreen.AR);
            session.enabled = screen == AppScreen.AR;
        }
    }
}
