using System;
using NUnit.Framework;
using OdontoAR.Logging;

namespace OdontoAR.Tests
{
    public class UsageFormatTests
    {
        static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.FromHours(-3));

        [TestCase(0f, "0 min")]
        [TestCase(59f, "0 min")]
        [TestCase(2220f, "37 min")]
        [TestCase(9420f, "2 h 37 min")]
        [TestCase(3600f, "1 h 0 min")]
        [TestCase(-5f, "0 min")]
        public void FormataDuracao(float seconds, string expected)
        {
            Assert.AreEqual(expected, UsageFormat.Duration(seconds));
        }

        [TestCase("2026-10-25T08:15:00-03:00", "Hoje, 08:15")]
        [TestCase("2026-10-24T14:32:05-03:00", "Ontem, 14:32")]
        [TestCase("2026-10-20T09:05:00-03:00", "20/10, 09:05")]
        [TestCase("2026-10-25T11:30:00+00:00", "Hoje, 08:30")]
        [TestCase(null, "Nenhuma ainda")]
        [TestCase("ontem", "Nenhuma ainda")]
        public void FormataUltimaSessao(string iso, string expected)
        {
            Assert.AreEqual(expected, UsageFormat.LastSession(iso, Now));
        }
    }
}
