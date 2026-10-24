using NUnit.Framework;
using OdontoAR.Model;

namespace OdontoAR.Tests
{
    public class FdiCodeTests
    {
        [TestCase("Upper medial incisor.r", 11)]
        [TestCase("Upper lateral incisor.r", 12)]
        [TestCase("Upper canine.r", 13)]
        [TestCase("Upper first premolar.r", 14)]
        [TestCase("Upper second premolar.r", 15)]
        [TestCase("Upper first molar tooth.r", 16)]
        [TestCase("Upper second molar tooth.r", 17)]
        [TestCase("Upper medial incisor.l", 21)]
        [TestCase("Upper second molar tooth.l", 27)]
        [TestCase("Lower medial incisor.l", 31)]
        [TestCase("Lower first molar tooth.l", 36)]
        [TestCase("Lower canine.r", 43)]
        [TestCase("Lower second premolar.r", 45)]
        [TestCase("Lower third molar tooth.r", 48)]
        public void MapeiaNomeDoZAnatomyParaFdi(string name, int expected)
        {
            Assert.IsTrue(FdiCode.TryParse(name, out var code));
            Assert.AreEqual(expected, code);
        }

        [TestCase("Upper first molar tooth")] // sem lado: é o nome da malha, não do objeto
        [TestCase("Maxilla.l")]
        [TestCase("Mandible")]
        [TestCase("Upper tooth.l")]
        [TestCase("")]
        [TestCase(null)]
        public void RecusaNomesQueNaoSaoDentes(string name)
        {
            Assert.IsFalse(FdiCode.TryParse(name, out _));
        }

        [Test]
        public void ToleraMaiusculasEEspacos()
        {
            Assert.IsTrue(FdiCode.TryParse("  LOWER First Premolar.L ", out var code));
            Assert.AreEqual(34, code);
        }

        [Test]
        public void ListaOs32DentesPermanentes()
        {
            Assert.AreEqual(32, FdiCode.AllPermanent.Count);
            Assert.AreEqual(11, FdiCode.AllPermanent[0]);
            Assert.AreEqual(48, FdiCode.AllPermanent[31]);
        }

        [Test]
        public void ClassificaQuadrantes()
        {
            Assert.IsTrue(FdiCode.IsUpper(16));
            Assert.IsFalse(FdiCode.IsUpper(36));
            Assert.IsTrue(FdiCode.IsPatientLeft(26));
            Assert.IsTrue(FdiCode.IsPatientLeft(36));
            Assert.IsFalse(FdiCode.IsPatientLeft(46));
            Assert.IsTrue(FdiCode.IsThirdMolar(38));
        }
    }
}
