using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace OdontoAR.App
{
    /// <summary>
    /// Abre a folha de compartilhamento do iOS com um arquivo (plugin Plugins/iOS/OdontoARShare.mm).
    /// No Editor só mostra o caminho no Console.
    /// </summary>
    public static class ShareSheet
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void OdontoAR_ShareFile(string path);

        public static void ShareFile(string path) => OdontoAR_ShareFile(path);
#else
        public static void ShareFile(string path) => Debug.Log($"[OdontoAR] Arquivo para compartilhar: {path}");
#endif
    }
}
