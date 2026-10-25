using NUnit.Framework;
using OdontoAR.Model;

namespace OdontoAR.Tests
{
    public class ParticipantCodeTests
    {
        [TestCase("014", "P-014")]
        [TestCase("p-014", "P-014")]
        [TestCase("P 0 1 4", "P-014")]
        [TestCase("14", "P-14")]
        [TestCase("01499", "P-014")]
        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("abc", "")]
        public void NormalizaParaPHifenDigitos(string input, string expected)
        {
            Assert.AreEqual(expected, ParticipantCode.Normalize(input));
        }

        [TestCase("P-014", true)]
        [TestCase("P-000", true)]
        [TestCase("P-14", false)]
        [TestCase("P-0145", false)]
        [TestCase("p-014", false)]
        [TestCase("Q-014", false)]
        [TestCase("P-01A", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void ValidaFormato(string code, bool expected)
        {
            Assert.AreEqual(expected, ParticipantCode.IsValid(code));
        }
    }
}
