using System;
using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>Quais arcadas aparecem (RF03).</summary>
    public enum ArchSelection
    {
        Both,
        Upper,
        Lower,
    }

    /// <summary>Camadas anatômicas (RF08).</summary>
    public enum Layer
    {
        Crown,
        Root,
        Bone,
        Nerves,
    }

    /// <summary>
    /// Estado do modelo, sem MonoBehaviour (testável em EditMode):
    /// posicionamento, rotação, escala, arcada visível, abertura da boca e camadas.
    /// </summary>
    public sealed class ModelState
    {
        public const float DefaultScale = 1f;
        public const float MinScale = 0.5f;
        public const float MaxScale = 4f;
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

        public ArchSelection Arch { get; private set; } = ArchSelection.Both;

        /// <summary>Abertura da boca (RF06): 0 = fechada, 1 = abertura máxima.</summary>
        public float Opening { get; private set; }

        // Todas as camadas começam visíveis (visualização integrada).
        readonly bool[] layerVisible = { true, true, true, true };

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

        public void SetArch(ArchSelection arch)
        {
            if (!Enum.IsDefined(typeof(ArchSelection), arch) || arch == Arch)
                return;

            Arch = arch;
            Changed?.Invoke();
        }

        public void SetOpening(float opening)
        {
            if (!IsFinite(opening))
                return;

            opening = Mathf.Clamp01(opening);
            if (Mathf.Approximately(opening, Opening))
                return;

            Opening = opening;
            Changed?.Invoke();
        }

        public bool IsLayerVisible(Layer layer) => IsValid(layer) && layerVisible[(int)layer];

        public void SetLayerVisible(Layer layer, bool visible)
        {
            if (!IsValid(layer) || layerVisible[(int)layer] == visible)
                return;

            layerVisible[(int)layer] = visible;
            Changed?.Invoke();
        }

        public void ToggleLayer(Layer layer)
        {
            if (IsValid(layer))
                SetLayerVisible(layer, !layerVisible[(int)layer]);
        }

        /// <summary>
        /// "Restaurar vista" (RF07): volta rotação e escala ao padrão, sem mover o modelo.
        /// Arcada, abertura e camadas ficam como o aluno deixou.
        /// </summary>
        public void ResetView()
        {
            Yaw = 0f;
            Pitch = 0f;
            Scale = DefaultScale;
            Changed?.Invoke();
        }

        static bool IsValid(Layer layer) => Enum.IsDefined(typeof(Layer), layer);

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
