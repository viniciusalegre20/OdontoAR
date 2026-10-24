using System.Collections.Generic;
using System.Linq;

namespace OdontoAR.Model
{
    /// <summary>
    /// Notação FDI (ISO 3950) a partir dos nomes do atlas Z-Anatomy, sem renomear no Blender.
    /// "Upper first molar tooth.l" → 26: quadrante (1 superior direito, 2 superior esquerdo,
    /// 3 inferior esquerdo, 4 inferior direito) × 10 + dente (1 incisivo central … 8 terceiro molar).
    /// O lado ".l"/".r" é o do paciente.
    /// </summary>
    public static class FdiCode
    {
        // Mais específicos primeiro: "second premolar" contém "premolar", "first molar" não contém "premolar".
        static readonly (string type, int number)[] Types =
        {
            ("medial incisor", 1),
            ("central incisor", 1),
            ("lateral incisor", 2),
            ("canine", 3),
            ("first premolar", 4),
            ("second premolar", 5),
            ("first molar", 6),
            ("second molar", 7),
            ("third molar", 8),
        };

        /// <summary>Os 32 dentes permanentes, em ordem de quadrante.</summary>
        public static readonly IReadOnlyList<int> AllPermanent =
            Enumerable.Range(1, 4).SelectMany(q => Enumerable.Range(1, 8).Select(t => q * 10 + t)).ToArray();

        public static bool TryParse(string objectName, out int code)
        {
            code = 0;
            if (string.IsNullOrEmpty(objectName))
                return false;

            var name = objectName.Trim().ToLowerInvariant();

            bool left;
            if (name.EndsWith(".l"))
                left = true;
            else if (name.EndsWith(".r"))
                left = false;
            else
                return false;
            name = name.Substring(0, name.Length - 2);

            bool upper;
            if (name.StartsWith("upper "))
                upper = true;
            else if (name.StartsWith("lower "))
                upper = false;
            else
                return false;

            var number = 0;
            foreach (var (type, n) in Types)
            {
                if (name.Contains(type))
                {
                    number = n;
                    break;
                }
            }
            if (number == 0)
                return false;

            var quadrant = upper ? (left ? 2 : 1) : (left ? 3 : 4);
            code = quadrant * 10 + number;
            return true;
        }

        public static int Quadrant(int code) => code / 10;
        public static int ToothNumber(int code) => code % 10;
        public static bool IsUpper(int code) => Quadrant(code) is 1 or 2;
        public static bool IsPatientLeft(int code) => Quadrant(code) is 2 or 3;
        public static bool IsThirdMolar(int code) => ToothNumber(code) == 8;
    }
}
