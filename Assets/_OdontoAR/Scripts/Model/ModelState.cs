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
    /// posicionamento, rotação, escala, arcada visível, abertura da boca, camadas e estrutura selecionada.
    /// </summary>
    public sealed class ModelState
    {
        public const float DefaultScale = 1f;
        public const float MinScale = 0.5f;
        public const float MaxScale = 4f;

        /// <summary>Ângulo da articulação com a abertura máxima (design system: 0–30°).</summary>
        public const float MaxOpeningDegrees = 30f;

        /// <summary>Disparado sempre que algum valor muda.</summary>
        public event Action Changed;

        public bool IsPlaced { get; private set; }

        /// <summary>Multiplicador sobre o tamanho real (1 = tamanho anatômico).</summary>
        public float Scale { get; private set; } = DefaultScale;

        /// <summary>
        /// Orientação da arcada no espaço do objeto posicionado (que fica de frente para o aluno, +Z para ele).
        /// Livre em qualquer direção (RF04): giro sobre a mesa, inclinação sem limite e rotação lateral.
        /// </summary>
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        public ArchSelection Arch { get; private set; } = ArchSelection.Both;

        /// <summary>Abertura da boca (RF06): 0 = fechada, 1 = <see cref="MaxOpeningDegrees"/>.</summary>
        public float Opening { get; private set; }

        // Todas as camadas começam visíveis (visualização integrada).
        readonly bool[] layerVisible = { true, true, true, true };

        /// <summary>
        /// Chave da estrutura selecionada no <c>StructureCatalog</c> (código FDI do dente ou nome do objeto),
        /// ou null sem seleção (RF09).
        /// </summary>
        public string SelectedKey { get; private set; }

        /// <summary>Camada do ponto tocado na estrutura selecionada.</summary>
        public Layer SelectedLayer { get; private set; }

        public bool HasSelection => SelectedKey != null;

        public void SetPlaced(bool placed)
        {
            if (IsPlaced == placed)
                return;

            IsPlaced = placed;
            if (placed)
            {
                // Cada posicionamento começa de frente, reto e em tamanho real: um giro feito antes de
                // "Reposicionar" deixava a arcada já tombada no lugar novo.
                Rotation = Quaternion.identity;
                Scale = DefaultScale;
            }
            else
            {
                SelectedKey = null;
            }
            Changed?.Invoke();
        }

        /// <summary>Aplica um giro (no espaço do objeto posicionado) sobre a orientação atual.</summary>
        public void RotateBy(Quaternion delta)
        {
            if (!IsFinite(delta.x) || !IsFinite(delta.y) || !IsFinite(delta.z) || !IsFinite(delta.w))
                return;

            var length = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z + delta.w * delta.w);
            if (length < 1e-6f)
                return;

            delta = new Quaternion(delta.x / length, delta.y / length, delta.z / length, delta.w / length);
            if (Quaternion.Angle(delta, Quaternion.identity) < 0.01f)
                return;

            // Renormaliza: muitos giros pequenos seguidos acumulam erro.
            Rotation = Normalize(delta * Rotation);
            Changed?.Invoke();
        }

        /// <summary>A arcada saiu da vista padrão (giro ou zoom).</summary>
        public bool IsViewChanged =>
            Quaternion.Angle(Rotation, Quaternion.identity) > 0.5f || !Mathf.Approximately(Scale, DefaultScale);

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

        public void Select(string key, Layer layer)
        {
            if (string.IsNullOrEmpty(key))
            {
                ClearSelection();
                return;
            }

            if (!IsValid(layer) || (key == SelectedKey && layer == SelectedLayer))
                return;

            SelectedKey = key;
            SelectedLayer = layer;
            Changed?.Invoke();
        }

        public void ClearSelection()
        {
            if (SelectedKey == null)
                return;

            SelectedKey = null;
            Changed?.Invoke();
        }

        /// <summary>
        /// "Restaurar vista" (RF07): volta rotação e escala ao padrão, sem mover o modelo.
        /// Arcada, abertura, camadas e seleção ficam como o aluno deixou.
        /// </summary>
        public void ResetView()
        {
            Rotation = Quaternion.identity;
            Scale = DefaultScale;
            Changed?.Invoke();
        }

        static bool IsValid(Layer layer) => Enum.IsDefined(typeof(Layer), layer);

        static Quaternion Normalize(Quaternion q)
        {
            var length = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return new Quaternion(q.x / length, q.y / length, q.z / length, q.w / length);
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
