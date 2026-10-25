using OdontoAR.App;
using OdontoAR.Model;
using OdontoAR.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OdontoAR.UI
{
    /// <summary>
    /// Tela 2: código do participante. O aluno digita os 3 números no teclado numérico; o "P-" é fixo.
    /// "Continuar" só habilita com o código completo; o código fica salvo neste aparelho.
    /// </summary>
    public sealed class ParticipantCodeScreen : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;
        [SerializeField] TMP_InputField field;
        [SerializeField] Image fieldBorder;
        [SerializeField] Button clearButton;
        [SerializeField] TextMeshProUGUI error;
        [SerializeField] Button continueButton;
        [SerializeField] TextMeshProUGUI continueLabel;

        void Awake()
        {
            field.keyboardType = TouchScreenKeyboardType.NumberPad;
            field.characterLimit = ParticipantCode.Digits;
            field.onValueChanged.AddListener(OnChanged);
            field.onSelect.AddListener(_ => Refresh());
            field.onDeselect.AddListener(_ => Refresh());
            field.onEndEdit.AddListener(_ => Refresh(showError: true));
            clearButton.onClick.AddListener(() => field.text = "");
            continueButton.onClick.AddListener(Continue);
        }

        void OnEnable()
        {
            field.SetTextWithoutNotify(ParticipantCode.DigitsOf(AppPrefs.ParticipantCode));
            Refresh();
        }

        string Code => ParticipantCode.Normalize(field.text);

        void OnChanged(string text)
        {
            var digits = ParticipantCode.DigitsOf(text);
            if (digits != text)
                field.SetTextWithoutNotify(digits);
            Refresh();
        }

        void Refresh(bool showError = false)
        {
            var valid = ParticipantCode.IsValid(Code);
            var hasText = field.text.Length > 0;
            var invalid = showError && hasText && !valid;

            error.gameObject.SetActive(invalid);
            clearButton.gameObject.SetActive(hasText);
            fieldBorder.color = invalid ? OdontoARTheme.Danger
                : field.isFocused ? OdontoARTheme.Accent
                : OdontoARTheme.Line;

            continueButton.interactable = valid;
            continueButton.image.color = valid ? OdontoARTheme.Accent : OdontoARTheme.Bg700;
            continueLabel.color = valid ? OdontoARTheme.OnAccent : OdontoARTheme.InkFaint;
        }

        void Continue()
        {
            if (!ParticipantCode.IsValid(Code))
                return;

            AppPrefs.ParticipantCode = Code;
            router.AfterCode();
        }
    }
}
