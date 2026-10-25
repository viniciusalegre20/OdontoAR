using System;
using System.Globalization;

namespace OdontoAR.Logging
{
    /// <summary>Textos do resumo de uso na tela 10 ("2 h 37 min", "Hoje, 14:32").</summary>
    public static class UsageFormat
    {
        public static string Duration(float seconds)
        {
            var minutes = (int)Math.Floor(Math.Max(0f, seconds) / 60f);
            var hours = minutes / 60;
            return hours > 0 ? $"{hours} h {minutes % 60} min" : $"{minutes} min";
        }

        /// <summary>Início da última sessão em relação a agora; null ou inválido → "Nenhuma ainda".</summary>
        public static string LastSession(string isoStart, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(isoStart)
                || !DateTimeOffset.TryParse(isoStart, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                return "Nenhuma ainda";

            var local = start.ToOffset(now.Offset);
            var time = local.ToString("HH:mm", CultureInfo.InvariantCulture);
            var days = (now.Date - local.Date).Days;
            return days switch
            {
                0 => $"Hoje, {time}",
                1 => $"Ontem, {time}",
                _ => $"{local.ToString("dd/MM", CultureInfo.InvariantCulture)}, {time}",
            };
        }
    }
}
