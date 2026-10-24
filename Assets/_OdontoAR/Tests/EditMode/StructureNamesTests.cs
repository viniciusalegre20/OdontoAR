using NUnit.Framework;
using OdontoAR.Model;

namespace OdontoAR.Tests
{
    public class StructureNamesTests
    {
        [TestCase(36, "Primeiro molar inferior esquerdo")]
        [TestCase(11, "Incisivo central superior direito")]
        [TestCase(22, "Incisivo lateral superior esquerdo")]
        [TestCase(43, "Canino inferior direito")]
        [TestCase(14, "Primeiro pré-molar superior direito")]
        [TestCase(35, "Segundo pré-molar inferior esquerdo")]
        [TestCase(27, "Segundo molar superior esquerdo")]
        [TestCase(48, "Terceiro molar inferior direito")]
        public void NomeiaDentesPeloFdi(int code, string expected)
        {
            Assert.IsTrue(StructureNames.TryTooth(code, out var name));
            Assert.AreEqual(expected, name);
        }

        [TestCase(0)]
        [TestCase(10)]
        [TestCase(19)]
        [TestCase(51)]
        public void RecusaCodigosInvalidos(int code)
        {
            Assert.IsFalse(StructureNames.TryTooth(code, out _));
        }

        [TestCase("Maxilla.l", "Maxila esquerda")]
        [TestCase("Maxilla.r", "Maxila direita")]
        [TestCase("Mandible", "Mandíbula")]
        [TestCase("Facial nerve (VII).r", "Nervo facial direito (VII)")]
        [TestCase("Vestibulocochlear nerve (VIII).l", "Nervo vestibulococlear esquerdo (VIII)")]
        [TestCase("Vestibular nerve.l", "Nervo vestibular esquerdo")]
        [TestCase("Motor nucleus of facial nerve.r", "Núcleo motor do nervo facial direito")]
        public void TraduzDemaisEstruturas(string objectName, string expected)
        {
            Assert.IsTrue(StructureNames.TryOther(objectName, out var name));
            Assert.AreEqual(expected, name);
        }

        [TestCase("Upper canine.l")]
        [TestCase("Skull")]
        [TestCase("")]
        [TestCase(null)]
        public void RecusaEstruturasDesconhecidas(string objectName)
        {
            Assert.IsFalse(StructureNames.TryOther(objectName, out _));
        }

        [Test]
        public void NomeiaCamadas()
        {
            Assert.AreEqual("Coroa", StructureNames.LayerName(Layer.Crown));
            Assert.AreEqual("Raiz", StructureNames.LayerName(Layer.Root));
            Assert.AreEqual("Osso", StructureNames.LayerName(Layer.Bone));
            Assert.AreEqual("Nervos", StructureNames.LayerName(Layer.Nerves));
        }
    }
}
