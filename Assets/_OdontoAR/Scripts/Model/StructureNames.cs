namespace OdontoAR.Model
{
    /// <summary>
    /// Nomes em português das estruturas, usados para gerar o <see cref="StructureCatalog"/>.
    /// Dentes pelo código FDI; as demais pelo nome do objeto no Z-Anatomy (lado ".l"/".r" do paciente).
    /// </summary>
    public static class StructureNames
    {
        static readonly string[] ToothTypes =
        {
            null,
            "Incisivo central",
            "Incisivo lateral",
            "Canino",
            "Primeiro pré-molar",
            "Segundo pré-molar",
            "Primeiro molar",
            "Segundo molar",
            "Terceiro molar",
        };

        // Nome no atlas → nome em português, complemento depois do lado (numeral do nervo craniano), gênero.
        static readonly (string english, string portuguese, string suffix, bool feminine)[] Others =
        {
            ("Maxilla", "Maxila", "", true),
            ("Mandible", "Mandíbula", "", true),
            ("Facial nerve (VII)", "Nervo facial", " (VII)", false),
            ("Motor nucleus of facial nerve", "Núcleo motor do nervo facial", "", false),
            ("Vestibulocochlear nerve (VIII)", "Nervo vestibulococlear", " (VIII)", false),
            ("Vestibular nerve", "Nervo vestibular", "", false),
        };

        /// <summary>"Primeiro molar inferior esquerdo" para 36.</summary>
        public static bool TryTooth(int code, out string name)
        {
            var quadrant = FdiCode.Quadrant(code);
            var number = FdiCode.ToothNumber(code);
            if (quadrant is < 1 or > 4 || number is < 1 or > 8)
            {
                name = null;
                return false;
            }

            var arch = FdiCode.IsUpper(code) ? "superior" : "inferior";
            var side = FdiCode.IsPatientLeft(code) ? "esquerdo" : "direito";
            name = $"{ToothTypes[number]} {arch} {side}";
            return true;
        }

        /// <summary>"Nervo facial direito (VII)" para "Facial nerve (VII).r".</summary>
        public static bool TryOther(string objectName, out string name)
        {
            name = null;
            if (string.IsNullOrEmpty(objectName))
                return false;

            var baseName = objectName.Trim();
            string side = null;
            if (baseName.EndsWith(".l") || baseName.EndsWith(".r"))
            {
                side = baseName.EndsWith(".l") ? "esquerd" : "direit";
                baseName = baseName.Substring(0, baseName.Length - 2);
            }

            foreach (var (english, portuguese, suffix, feminine) in Others)
            {
                if (baseName != english)
                    continue;

                var sideText = side == null ? "" : $" {side}{(feminine ? "a" : "o")}";
                name = portuguese + sideText + suffix;
                return true;
            }

            return false;
        }

        public static string LayerName(Layer layer) => layer switch
        {
            Layer.Crown => "Coroa",
            Layer.Root => "Raiz",
            Layer.Bone => "Osso",
            Layer.Nerves => "Nervos",
            _ => "",
        };
    }
}
