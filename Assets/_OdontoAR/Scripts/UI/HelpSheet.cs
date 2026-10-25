using OdontoAR.App;
using OdontoAR.Model;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// Tela 9 (FolhaAjuda): gestos e créditos dos modelos. Abre pelo "?" e sozinha na primeira vez
    /// que a arcada é posicionada; fecha com "Entendi", com o X ou tocando fora da folha.
    /// </summary>
    public sealed class HelpSheet : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] GameObject sheetRoot;
        [SerializeField] Button[] closeButtons;
        [SerializeField] ScrollRect scroll;

        void Awake()
        {
            foreach (var button in closeButtons)
                button.onClick.AddListener(Close);
        }

        void OnEnable()
        {
            holder.State.Changed += OnStateChanged;
        }

        void OnDisable()
        {
            holder.State.Changed -= OnStateChanged;
        }

        void OnStateChanged()
        {
            if (holder.State.IsPlaced && !AppPrefs.HelpSeen && !sheetRoot.activeSelf)
                Open();
        }

        public void Open()
        {
            sheetRoot.SetActive(true);
            scroll.verticalNormalizedPosition = 1f;
        }

        public void Close()
        {
            if (!sheetRoot.activeSelf)
                return;

            sheetRoot.SetActive(false);
            AppPrefs.HelpSeen = true;
        }
    }
}
