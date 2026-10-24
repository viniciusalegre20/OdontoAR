using System;
using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// Estado do modelo, sem MonoBehaviour (testável em EditMode).
    /// T3: posicionamento, rotação e escala. T4 acrescenta arcada e abertura.
    /// </summary>
    public sealed class ModelState
    {
        public const float DefaultScale = 2f;
        public const float MinScale = 0.5f;
        public const float MaxScale = 6f;
        public const float MaxPitch = 80f;

        /// <summary>Disparado sempre que algum valor muda.</summary>
        public event Action Changed;

        public bool IsPlaced { get; private set; }

        /// <summary>Multiplicador sobre o tamanho real (1 = tamanho anatômico).</summary>
        public float Scale { get; private set; } = DefaultScale;

        /// <summary>Giro em torno do eixo vertical, em graus [0, 360).</summary>
        public float Yaw { get; private set; }

        /// <summary>Inclinação para frente/trás, em graus [-MaxPitch, MaxPitch].</summary>
        public float Pitch { get; private set; }

        public void SetPlaced(bool placed)
        {
            if (IsPlaced == placed)
                return;

            IsPlaced = placed;
            Changed?.Invoke();
        }

        public void Rotate(float deltaYaw, float deltaPitch)
        {
            if (!IsFinite(deltaYaw) || !IsFinite(deltaPitch))
                return;

            var yaw = Mathf.Repeat(Yaw + deltaYaw, 360f);
            var pitch = Mathf.Clamp(Pitch + deltaPitch, -MaxPitch, MaxPitch);
            if (Mathf.Approximately(yaw, Yaw) && Mathf.Approximately(pitch, Pitch))
                return;

            Yaw = yaw;
            Pitch = pitch;
            Changed?.Invoke();
        }

        /// <summary>Multiplica a escala atual (ex.: razão da pinça), respeitando os limites.</summary>
        public void ScaleBy(float factor)
        {
            if (!IsFinite(factor) || factor <= 0f)
                return;

            var scale = Mathf.Clamp(Scale * factor, MinScale, MaxScale);
            if (Mathf.Approximately(scale, Scale))
                return;

            Scale = scale;
            Changed?.Invoke();
        }

        /// <summary>"Restaurar vista" (RF07): volta rotação e escala ao padrão, sem mover o modelo.</summary>
        public void ResetView()
        {
            Yaw = 0f;
            Pitch = 0f;
            Scale = DefaultScale;
            Changed?.Invoke();
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
