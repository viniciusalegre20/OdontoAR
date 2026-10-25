using System.Collections;
using OdontoAR.App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// Tela 3: explica o uso da câmera antes do alerta do iOS. "Permitir câmera" abre o alerta; se o
    /// acesso foi negado antes, o iOS não pergunta de novo e o botão passa a abrir os Ajustes.
    /// </summary>
    public sealed class PermissionScreen : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;
        [SerializeField] Button allowButton;
        [SerializeField] TextMeshProUGUI allowLabel;
        [SerializeField] GameObject deniedNote;

        const string AllowText = "Permitir câmera";
        const string SettingsText = "Abrir Ajustes";

        bool denied;
        bool requesting;

        void Awake()
        {
            allowButton.onClick.AddListener(OnAllow);
        }

        void OnEnable()
        {
            // Negado antes (ou restrito): o iOS não pergunta de novo, só os Ajustes resolvem.
            var status = CameraPermission.Status;
            denied = status is CameraAccess.Denied or CameraAccess.Restricted;
            Refresh();
        }

        void OnAllow()
        {
            if (denied)
            {
                Application.OpenURL("app-settings:");
                return;
            }

            if (!requesting)
                StartCoroutine(Request());
        }

        IEnumerator Request()
        {
            requesting = true;
            yield return CameraPermission.Request();
            requesting = false;

            if (ScreenRouter.CameraAuthorized)
            {
                router.GoToAR();
                yield break;
            }

            denied = true;
            Refresh();
        }

        // Voltando dos Ajustes com a câmera liberada.
        void OnApplicationFocus(bool focus)
        {
            if (focus && denied && isActiveAndEnabled && ScreenRouter.CameraAuthorized)
                router.GoToAR();
        }

        void Refresh()
        {
            allowLabel.text = denied ? SettingsText : AllowText;
            deniedNote.SetActive(denied);
        }
    }
}
