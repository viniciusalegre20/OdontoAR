using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace OdontoAR.UI
{
    /// <summary>
    /// Lembra quando houve dois ou mais dedos na tela, para um toque que fez parte de uma pinça ou do
    /// gesto de 3 dedos não contar como toque simples (posicionar, selecionar).
    /// </summary>
    public static class MultiTouch
    {
        static double lastMultiTouchTime = double.NegativeInfinity;
        static int subscribers;

        public static void Track()
        {
            if (subscribers++ == 0)
                Touch.onFingerDown += OnFingerDown;
        }

        public static void Untrack()
        {
            if (subscribers > 0 && --subscribers == 0)
                Touch.onFingerDown -= OnFingerDown;
        }

        static void OnFingerDown(Finger finger)
        {
            if (Touch.activeFingers.Count >= 2)
                lastMultiTouchTime = finger.currentTouch.startTime;
        }

        /// <summary>Outro dedo tocou a tela enquanto este toque durava.</summary>
        public static bool Overlaps(Touch touch) => lastMultiTouchTime >= touch.startTime;
    }
}
