using OdontoAR.Model;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>Botões liga/desliga das camadas (RF08), ligados ao <see cref="ModelState"/>.</summary>
    public sealed class LayerControls : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] Button crownButton;
        [SerializeField] Button rootButton;
        [SerializeField] Button boneButton;
        [SerializeField] Button nervesButton;

        void Awake()
        {
            crownButton.onClick.AddListener(() => holder.State.ToggleLayer(Layer.Crown));
            rootButton.onClick.AddListener(() => holder.State.ToggleLayer(Layer.Root));
            boneButton.onClick.AddListener(() => holder.State.ToggleLayer(Layer.Bone));
            nervesButton.onClick.AddListener(() => holder.State.ToggleLayer(Layer.Nerves));
        }

        void OnEnable()
        {
            holder.State.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            holder.State.Changed -= Refresh;
        }

        void Refresh()
        {
            var state = holder.State;
            ButtonStyle.SetSelected(crownButton, state.IsLayerVisible(Layer.Crown));
            ButtonStyle.SetSelected(rootButton, state.IsLayerVisible(Layer.Root));
            ButtonStyle.SetSelected(boneButton, state.IsLayerVisible(Layer.Bone));
            ButtonStyle.SetSelected(nervesButton, state.IsLayerVisible(Layer.Nerves));
        }
    }
}
