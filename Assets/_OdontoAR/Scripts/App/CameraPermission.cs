using System.Collections;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace OdontoAR.App
{
    public enum CameraAccess
    {
        NotDetermined = 0,
        Restricted = 1,
        Denied = 2,
        Authorized = 3,
    }

    /// <summary>
    /// Permissão de câmera consultada no próprio iOS (plugin Plugins/iOS/OdontoARCamera.mm).
    /// No Editor a câmera conta como permitida.
    /// </summary>
    public static class CameraPermission
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int OdontoAR_CameraAuthorizationStatus();
        [DllImport("__Internal")] static extern void OdontoAR_RequestCameraAccess();

        public static CameraAccess Status => (CameraAccess)OdontoAR_CameraAuthorizationStatus();
        static void RequestNative() => OdontoAR_RequestCameraAccess();
#else
        public static CameraAccess Status => CameraAccess.Authorized;
        static void RequestNative() { }
#endif

        public static bool IsAuthorized => Status == CameraAccess.Authorized;

        /// <summary>Pede a câmera e espera o aluno responder ao alerta do iOS.</summary>
        public static IEnumerator Request()
        {
            if (Status != CameraAccess.NotDetermined)
                yield break; // o iOS só pergunta uma vez

            RequestNative();
            while (Status == CameraAccess.NotDetermined)
                yield return null;
        }
    }
}
