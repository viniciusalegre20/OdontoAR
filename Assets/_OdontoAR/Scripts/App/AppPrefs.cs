using UnityEngine;

namespace OdontoAR.App
{
    /// <summary>
    /// O que o app lembra entre aberturas, só neste aparelho: código do participante e se a ajuda
    /// já abriu sozinha. Nenhum dado pessoal.
    /// </summary>
    public static class AppPrefs
    {
        const string ParticipantKey = "odontoar.participante";
        const string HelpSeenKey = "odontoar.ajudaVista";

        public static string ParticipantCode
        {
            get => PlayerPrefs.GetString(ParticipantKey, "");
            set
            {
                PlayerPrefs.SetString(ParticipantKey, value ?? "");
                PlayerPrefs.Save();
            }
        }

        public static bool HelpSeen
        {
            get => PlayerPrefs.GetInt(HelpSeenKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(HelpSeenKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
