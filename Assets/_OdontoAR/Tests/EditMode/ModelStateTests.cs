using NUnit.Framework;
using OdontoAR.Model;

namespace OdontoAR.Tests
{
    public class ModelStateTests
    {
        ModelState state;
        int changes;

        [SetUp]
        public void SetUp()
        {
            state = new ModelState();
            changes = 0;
            state.Changed += () => changes++;
        }

        [Test]
        public void ComecaNaoPosicionadoNaVistaPadrao()
        {
            Assert.IsFalse(state.IsPlaced);
            Assert.AreEqual(ModelState.DefaultScale, state.Scale);
            Assert.AreEqual(0f, state.Yaw);
            Assert.AreEqual(0f, state.Pitch);
        }

        [Test]
        public void SetPlacedSoNotificaQuandoMuda()
        {
            state.SetPlaced(true);
            state.SetPlaced(true);
            Assert.IsTrue(state.IsPlaced);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void GiroDaVoltaCompletaEFicaEntre0e360()
        {
            state.Rotate(-30f, 0f);
            Assert.AreEqual(330f, state.Yaw, 1e-4f);

            state.Rotate(400f, 0f);
            Assert.AreEqual(10f, state.Yaw, 1e-4f);
        }

        [Test]
        public void InclinacaoRespeitaOLimite()
        {
            state.Rotate(0f, 500f);
            Assert.AreEqual(ModelState.MaxPitch, state.Pitch);

            state.Rotate(0f, -1000f);
            Assert.AreEqual(-ModelState.MaxPitch, state.Pitch);
        }

        [Test]
        public void PincaMultiplicaAEscalaDentroDosLimites()
        {
            state.ScaleBy(1.5f);
            Assert.AreEqual(ModelState.DefaultScale * 1.5f, state.Scale, 1e-4f);

            state.ScaleBy(100f);
            Assert.AreEqual(ModelState.MaxScale, state.Scale);

            state.ScaleBy(0.0001f);
            Assert.AreEqual(ModelState.MinScale, state.Scale);
        }

        [TestCase(0f)]
        [TestCase(-2f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void FatorDeEscalaInvalidoEIgnorado(float factor)
        {
            state.ScaleBy(factor);
            Assert.AreEqual(ModelState.DefaultScale, state.Scale);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void GiroInvalidoEIgnorado()
        {
            state.Rotate(float.NaN, 10f);
            Assert.AreEqual(0f, state.Yaw);
            Assert.AreEqual(0f, state.Pitch);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void RestaurarVistaZeraGiroEscalaMasMantemPosicionado()
        {
            state.SetPlaced(true);
            state.Rotate(45f, 20f);
            state.ScaleBy(2f);

            state.ResetView();

            Assert.IsTrue(state.IsPlaced);
            Assert.AreEqual(0f, state.Yaw);
            Assert.AreEqual(0f, state.Pitch);
            Assert.AreEqual(ModelState.DefaultScale, state.Scale);
        }

        [Test]
        public void ComecaComAmbasAsArcadasFechadas()
        {
            Assert.AreEqual(ArchSelection.Both, state.Arch);
            Assert.AreEqual(0f, state.Opening);
        }

        [Test]
        public void SetArchSoNotificaQuandoMuda()
        {
            state.SetArch(ArchSelection.Lower);
            state.SetArch(ArchSelection.Lower);
            Assert.AreEqual(ArchSelection.Lower, state.Arch);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void ArcadaInvalidaEIgnorada()
        {
            state.SetArch((ArchSelection)99);
            Assert.AreEqual(ArchSelection.Both, state.Arch);
            Assert.AreEqual(0, changes);
        }

        [TestCase(0.4f, 0.4f)]
        [TestCase(-1f, 0f)]
        [TestCase(3f, 1f)]
        public void AberturaFicaEntre0e1(float pedido, float esperado)
        {
            state.SetOpening(pedido);
            Assert.AreEqual(esperado, state.Opening, 1e-5f);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void AberturaInvalidaEIgnorada(float opening)
        {
            state.SetOpening(opening);
            Assert.AreEqual(0f, state.Opening);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void RestaurarVistaMantemArcadaEAbertura()
        {
            state.SetArch(ArchSelection.Upper);
            state.SetOpening(0.7f);

            state.ResetView();

            Assert.AreEqual(ArchSelection.Upper, state.Arch);
            Assert.AreEqual(0.7f, state.Opening, 1e-5f);
        }
    }
}
