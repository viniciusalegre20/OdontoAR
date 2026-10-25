using System.Text;

namespace OdontoAR.Model
{
    /// <summary>
    /// Código do participante da pesquisa no formato P-000 (letra P, hífen e 3 dígitos).
    /// O campo usa o teclado numérico: o aluno digita só os números e o "P-" é fixo.
    /// </summary>
    public static class ParticipantCode
    {
        public const string Prefix = "P-";
        public const int Digits = 3;

        /// <summary>Só os dígitos digitados (no máximo 3), ignorando "P", hífen, espaços etc.</summary>
        public static string DigitsOf(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            var digits = new StringBuilder(Digits);
            foreach (var c in input)
            {
                if (c is >= '0' and <= '9')
                {
                    digits.Append(c);
                    if (digits.Length == Digits)
                        break;
                }
            }
            return digits.ToString();
        }

        /// <summary>"p14" → "P-14"; vazio continua vazio.</summary>
        public static string Normalize(string input)
        {
            var digits = DigitsOf(input);
            return digits.Length == 0 ? "" : Prefix + digits;
        }

        public static bool IsValid(string code) =>
            code != null && code.Length == Prefix.Length + Digits && code.StartsWith(Prefix)
            && DigitsOf(code.Substring(Prefix.Length)).Length == Digits;
    }
}
