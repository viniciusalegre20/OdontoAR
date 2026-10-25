using System;
using NUnit.Framework;
using OdontoAR.Logging;
using OdontoAR.Model;
using UnityEngine;

namespace OdontoAR.Tests
{
    public class UsageLogTests
    {
        static readonly DateTimeOffset Start = new(2026, 10, 24, 14, 32, 5, TimeSpan.FromHours(-3));

        UsageLog log;

        [SetUp]
        public void SetUp()
        {
            log = new UsageLog("P-014");
        }

        [Test]
        public void EventosGuardamTempoRelativoAoInicioDaSessao()
        {
            log.StartSession(Start, 100d);
            log.Record(UsageEvent.Simple(UsageEvent.SurfaceDetected), 103.24d);
            log.Record(UsageEvent.Simple(UsageEvent.Placed), 105d);
            log.EndSession(812d);

            var session = log.Data.sessoes[0];
            Assert.AreEqual("2026-10-24T14:32:05-03:00", session.inicio);
            Assert.AreEqual(3.2f, session.eventos[0].t, 1e-4f);
            Assert.AreEqual(5f, session.eventos[1].t, 1e-4f);
            Assert.AreEqual(712f, session.duracaoSeg, 1e-4f);
            Assert.IsTrue(session.encerrada);
            Assert.IsFalse(log.InSession);
        }

        [Test]
        public void EventoSemSessaoEIgnorado()
        {
            log.Record(UsageEvent.Simple(UsageEvent.Placed), 1d);
            Assert.AreEqual(0, log.SessionCount);
        }

        [Test]
        public void SessaoInterrompidaFicaEncerradaComADuracaoSalva()
        {
            log.StartSession(Start, 0d);
            log.Record(UsageEvent.LayerToggle(Layer.Root, false), 12d);
            var saved = log.ToStorageJson(); // o app é fechado aqui, sem EndSession

            var reloaded = UsageLog.FromStorageJson("P-014", saved);
            var session = reloaded.Data.sessoes[0];
            Assert.IsTrue(session.encerrada);
            Assert.IsTrue(session.interrompida);
            Assert.AreEqual(12f, session.duracaoSeg, 1e-4f);
            Assert.IsFalse(reloaded.InSession);
            StringAssert.Contains("\"interrompida\": true", reloaded.ToExportJson("1.0"));
        }

        [Test]
        public void NovaSessaoDepoisDeRecarregarSeSomaAsAnteriores()
        {
            log.StartSession(Start, 0d);
            log.EndSession(60d);
            var reloaded = UsageLog.FromStorageJson("P-014", log.ToStorageJson());
            reloaded.StartSession(Start.AddDays(1), 0d);
            reloaded.EndSession(30d);

            Assert.AreEqual(2, reloaded.SessionCount);
            Assert.AreEqual(90f, reloaded.TotalSeconds, 1e-4f);
            Assert.IsFalse(reloaded.Data.sessoes[0].interrompida);
        }

        [Test]
        public void SessaoCurtaSemEventosEDescartada()
        {
            log.StartSession(Start, 0d);
            log.EndSession(2d);
            Assert.AreEqual(0, log.SessionCount);

            log.StartSession(Start, 10d);
            log.EndSession(40d); // sem eventos, mas 30 s olhando o modelo: conta
            Assert.AreEqual(1, log.SessionCount);

            log.StartSession(Start, 50d);
            log.Record(UsageEvent.Simple(UsageEvent.Placed), 51d);
            log.EndSession(52d); // curta, mas com evento: conta
            Assert.AreEqual(2, log.SessionCount);
        }

        [Test]
        public void SessaoInterrompidaVaziaEDescartadaAoRecarregar()
        {
            log.StartSession(Start, 0d);
            var reloaded = UsageLog.FromStorageJson("P-014", log.ToStorageJson());
            Assert.AreEqual(0, reloaded.SessionCount);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{ isto não é json")]
        public void RegistroAusenteOuInvalidoComecaVazio(string json)
        {
            var reloaded = UsageLog.FromStorageJson("P-014", json);
            Assert.AreEqual("P-014", reloaded.Participant);
            Assert.AreEqual(0, reloaded.SessionCount);
        }

        [Test]
        public void RegistroDeOutroParticipanteNaoEAproveitado()
        {
            log.StartSession(Start, 0d);
            log.EndSession(10d);
            var other = UsageLog.FromStorageJson("P-020", log.ToStorageJson());
            Assert.AreEqual(0, other.SessionCount);
        }

        [Test]
        public void ResumoContaSoAsCamadasLigadas()
        {
            log.StartSession(Start, 0d);
            log.Record(UsageEvent.LayerToggle(Layer.Root, false), 1d);
            log.Record(UsageEvent.LayerToggle(Layer.Root, true), 2d);
            log.Record(UsageEvent.LayerToggle(Layer.Root, true), 3d);
            log.Record(UsageEvent.LayerToggle(Layer.Nerves, true), 4d);

            Assert.AreEqual(2, log.Activations(Layer.Root));
            Assert.AreEqual(1, log.Activations(Layer.Nerves));
            Assert.AreEqual(0, log.Activations(Layer.Crown));
        }

        [Test]
        public void ExportacaoSegueOFormatoDoDesignSystem()
        {
            log.StartSession(Start, 0d);
            log.Record(UsageEvent.LayerToggle(Layer.Bone, true), 18.4d);
            log.Record(UsageEvent.Selection("36", Layer.Root), 42.9d);
            log.Record(UsageEvent.Selection("Maxilla.l", Layer.Bone), 50d);
            log.Record(UsageEvent.Opening(18), 60.1d);
            log.Record(UsageEvent.Arch(ArchSelection.Lower), 61d);
            log.EndSession(712d);

            var json = log.ToExportJson("1.0");
            StringAssert.Contains("\"participante\": \"P-014\"", json);
            StringAssert.Contains("{ \"t\": 18.4, \"tipo\": \"camada\", \"camada\": \"osso\", \"visivel\": true }", json);
            StringAssert.Contains("{ \"t\": 42.9, \"tipo\": \"selecao\", \"fdi\": 36, \"parte\": \"raiz\" }", json);
            StringAssert.Contains("{ \"t\": 50.0, \"tipo\": \"selecao\", \"estrutura\": \"Maxilla.l\", \"parte\": \"osso\" }", json);
            StringAssert.Contains("{ \"t\": 60.1, \"tipo\": \"abertura\", \"graus\": 18 }", json);
            StringAssert.Contains("{ \"t\": 61.0, \"tipo\": \"arcada\", \"arcada\": \"inferior\" }", json);
            StringAssert.DoesNotContain("\"fdi\": 0", json);

            // É JSON válido: o JsonUtility lê o cabeçalho e o resumo.
            var header = JsonUtility.FromJson<ExportHeader>(json);
            Assert.AreEqual("OdontoAR", header.app);
            Assert.AreEqual("P-014", header.participante);
            Assert.AreEqual(1, header.resumo.sessoes);
            Assert.AreEqual(712, header.resumo.tempoTotalSeg);
            Assert.AreEqual(2, header.resumo.selecoes);
        }

        [Test]
        public void ExportacaoSemSessoesEValida()
        {
            var header = JsonUtility.FromJson<ExportHeader>(log.ToExportJson("1.0"));
            Assert.AreEqual(0, header.resumo.sessoes);
        }

        [Test]
        public void TextoComAspasEEscapado()
        {
            var weird = new UsageLog("P-\"1\\");
            StringAssert.Contains("\"participante\": \"P-\\\"1\\\\\"", weird.ToExportJson("1.0"));
        }

        [Test]
        public void NomeDoArquivoExportado()
        {
            Assert.AreEqual("odontoar-P-014-2026-10-24.json", UsageLog.ExportFileName("P-014", Start));
        }

        [Serializable]
        class ExportHeader
        {
            public string app;
            public string participante;
            public Summary resumo;
        }

        [Serializable]
        class Summary
        {
            public int sessoes;
            public int tempoTotalSeg;
            public int selecoes;
        }
    }
}
