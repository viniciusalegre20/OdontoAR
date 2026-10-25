using OdontoAR.AR;
using OdontoAR.Diagnostics;
using OdontoAR.Logging;
using OdontoAR.Model;
using OdontoAR.Theme;
using OdontoAR.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace OdontoAR.Editor
{
    /// <summary>
    /// T8/T9: monta a interface conforme o design system (artefato "OdontoAR", tipo Design System):
    /// telas 1–3 (abertura, código, permissão), a RA (barra superior, seletor segmentado, pílulas de
    /// status, painel de camadas, cartão de estrutura), a folha de ajuda com os créditos e o registro de
    /// uso (tela 10), além do UsageRecorder.
    /// Medidas em pt: o Canvas usa a resolução de referência do iPhone 16 Pro Max, 440 × 956 pt.
    /// </summary>
    public static class ARUIBuilder
    {
        public struct Context
        {
            public ModelStateHolder Holder;
            public ARTapToPlace TapToPlace;
            public SelectionController Selection;
            public Camera Camera;
            public ARSession Session;
            public StructureCatalog Catalog;
        }

        static readonly Vector2 ReferenceResolution = new(440f, 956f);

        const string CreditsText =
            "Modelos 3D adaptados do Z-Anatomy (z-anatomy.com), baseado no BodyParts3D © DBCLS. " +
            "Licença CC BY-SA 4.0.\n\nFontes Inter e JetBrains Mono, licença SIL Open Font License 1.1.";

        static UIAssets kit;

        public static void Build(Context context)
        {
            kit = UIAssets.Load();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            var canvas = (RectTransform)canvasGo.transform;

            var router = canvasGo.AddComponent<ScreenRouter>();

            var opening = BuildOpening(canvas, router);
            var code = BuildCode(canvas, router);
            var permission = BuildPermission(canvas, router);
            var (ar, help) = BuildAR(canvas, router, context);

            var recorder = new GameObject("RegistroDeUso").AddComponent<UsageRecorder>();
            Set(recorder, "holder", context.Holder);
            Set(recorder, "tapToPlace", context.TapToPlace);
            Set(recorder, "router", router);
            var log = BuildLog(canvas, router, recorder);

            Set(router, "opening", opening);
            Set(router, "code", code);
            Set(router, "permission", permission);
            Set(router, "ar", ar);
            Set(router, "log", log);
            Set(router, "session", context.Session);
            Set(router, "holder", context.Holder);
            Set(router, "help", help);

            BuildFrameRateOverlay(canvas);

            // Estado inicial no Editor; em execução o ScreenRouter decide.
            code.SetActive(false);
            permission.SetActive(false);
            ar.SetActive(false);
            log.SetActive(false);
        }

        // ---------- Tela 1: Abertura ----------

        static GameObject BuildOpening(RectTransform canvas, ScreenRouter router)
        {
            var screen = FullScreen(canvas, "Tela01_Abertura", OdontoARTheme.Bg900);
            var safe = SafeContent(screen);

            var brand = Place(Rect(safe, "Marca"), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(300f, 300f));
            Img(Place(Rect(brand, "Halo"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f)),
                kit.Glow, OdontoARTheme.LogoHalo, raycast: false, sliced: false);
            Img(Place(Rect(brand, "Simbolo"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(176f, 176f)),
                kit.Logo, Color.white, raycast: false, sliced: false).preserveAspect = true;

            var accent = ColorUtility.ToHtmlStringRGB(OdontoARTheme.Accent);
            var title = Text(safe, $"Odonto<color=#{accent}>AR</color>", kit.Bold, 40f, OdontoARTheme.Ink,
                TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(392f, 50f));
            var subtitle = Text(safe, "Anatomia dentária em Realidade Aumentada", kit.Regular, OdontoARTheme.Body,
                OdontoARTheme.InkMuted, TextAlignmentOptions.Center);
            Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(392f, 28f));

            var start = PrimaryButton(safe, "Começar", null);
            BottomStretch((RectTransform)start.transform, 52f, OdontoARTheme.SizeButton);
            UnityEventTools.AddPersistentListener(start.onClick, router.GoToCode);

            var note = Text(safe, "Projeto de pesquisa · Graduação em Odontologia", kit.Regular, OdontoARTheme.Footnote,
                OdontoARTheme.InkFaint, TextAlignmentOptions.Center);
            BottomStretch(note.rectTransform, 16f, 20f);
            return screen;
        }

        // ---------- Tela 2: Código do participante ----------

        static GameObject BuildCode(RectTransform canvas, ScreenRouter router)
        {
            var screen = FullScreen(canvas, "Tela02_Codigo", OdontoARTheme.Bg900);
            var safe = SafeContent(screen);
            var back = BackButton(safe);
            UnityEventTools.AddPersistentListener(back.onClick, router.GoToOpening);

            var stack = TopStack(safe, 64f);
            Text(stack, "ETAPA 1 DE 2", kit.SemiBold, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);
            Text(stack, "Código do participante", kit.Bold, OdontoARTheme.LargeTitle, OdontoARTheme.Ink);
            Text(stack, "Digite o código que você recebeu da equipe da pesquisa.", kit.Regular, OdontoARTheme.Body,
                OdontoARTheme.InkMuted);
            Spacer(stack, 12f);

            // Campo: "P-" fixo + os 3 dígitos digitados no teclado numérico.
            var field = Rect(stack, "CampoCodigo");
            Size(field, height: OdontoARTheme.SizeField);
            Img(field, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.Bg800, raycast: false);
            var border = Img(Stretch(Rect(field, "Borda")), kit.Stroke(OdontoARTheme.RadiusMd, 1.5f), OdontoARTheme.Line,
                raycast: false);

            var prefix = Text(field, ParticipantCode.Prefix, kit.Mono, OdontoARTheme.CodeLarge, OdontoARTheme.Ink,
                TextAlignmentOptions.MidlineLeft);
            prefix.characterSpacing = 8f;
            var prefixRect = prefix.rectTransform;
            prefixRect.anchorMin = new Vector2(0f, 0f);
            prefixRect.anchorMax = new Vector2(0f, 1f);
            prefixRect.pivot = new Vector2(0f, 0.5f);
            prefixRect.anchoredPosition = new Vector2(20f, 0f);
            prefixRect.sizeDelta = new Vector2(48f, 0f);

            var input = BuildInputField(field);
            var inputRect = (RectTransform)input.transform;
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(70f, 0f);
            inputRect.offsetMax = new Vector2(-56f, 0f);

            var clear = IconButton(field, "x", 14f, 28f, OdontoARTheme.GlassFill, OdontoARTheme.InkMuted);
            var clearRect = (RectTransform)clear.transform;
            clearRect.anchorMin = clearRect.anchorMax = new Vector2(1f, 0.5f);
            clearRect.pivot = new Vector2(1f, 0.5f);
            clearRect.anchoredPosition = new Vector2(-16f, 0f);

            var error = Text(stack, "Use o formato P-000: letra P, hífen e 3 dígitos.", kit.Regular,
                OdontoARTheme.Footnote, OdontoARTheme.Danger);

            var privacy = HStack(stack, "Privacidade", 8f);
            privacy.childAlignment = TextAnchor.UpperLeft;
            Size(Icon(privacy.transform, "lock", 18f, OdontoARTheme.InkMuted).rectTransform, 18f, 18f);
            var privacyText = Text(privacy.transform,
                "Seu código identifica seu uso na pesquisa. Nenhum dado pessoal é coletado.", kit.Regular,
                OdontoARTheme.Footnote, OdontoARTheme.InkMuted);
            Size(privacyText.rectTransform, flexibleWidth: 1f);

            Spacer(stack, 12f);
            var next = PrimaryButton(stack, "Continuar", null);
            Size((RectTransform)next.transform, height: OdontoARTheme.SizeButton);

            var logic = screen.AddComponent<ParticipantCodeScreen>();
            Set(logic, "router", router);
            Set(logic, "field", input);
            Set(logic, "fieldBorder", border);
            Set(logic, "clearButton", clear);
            Set(logic, "error", error);
            Set(logic, "continueButton", next);
            Set(logic, "continueLabel", next.GetComponentInChildren<TextMeshProUGUI>());
            return screen;
        }

        static TMP_InputField BuildInputField(RectTransform parent)
        {
            var go = new GameObject("Entrada", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var hit = go.GetComponent<Image>();
            hit.color = OdontoARTheme.WithAlpha(OdontoARTheme.Bg800, 0f); // só para receber o toque

            var area = Stretch(Rect((RectTransform)go.transform, "Area"));
            area.gameObject.AddComponent<RectMask2D>();

            var placeholder = Text(area, "000", kit.Mono, OdontoARTheme.CodeLarge, OdontoARTheme.InkFaint,
                TextAlignmentOptions.MidlineLeft);
            placeholder.characterSpacing = 8f;
            Stretch(placeholder.rectTransform);
            var text = Text(area, "", kit.Mono, OdontoARTheme.CodeLarge, OdontoARTheme.Ink,
                TextAlignmentOptions.MidlineLeft);
            text.characterSpacing = 8f;
            Stretch(text.rectTransform);

            var field = go.GetComponent<TMP_InputField>();
            field.targetGraphic = hit;
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = kit.Mono;
            field.pointSize = OdontoARTheme.CodeLarge;
            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            field.keyboardType = TouchScreenKeyboardType.NumberPad;
            field.characterLimit = ParticipantCode.Digits;
            field.customCaretColor = true;
            field.caretColor = OdontoARTheme.Accent;
            field.caretWidth = 2;
            field.selectionColor = OdontoARTheme.AccentSoft;
            field.transition = Selectable.Transition.None;
            return field;
        }

        // ---------- Tela 3: Permissão de câmera ----------

        static GameObject BuildPermission(RectTransform canvas, ScreenRouter router)
        {
            var screen = FullScreen(canvas, "Tela03_Permissao", OdontoARTheme.Bg900);
            var safe = SafeContent(screen);
            var back = BackButton(safe);
            UnityEventTools.AddPersistentListener(back.onClick, router.GoToCode);

            var stack = TopStack(safe, 64f);
            Text(stack, "ETAPA 2 DE 2", kit.SemiBold, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);

            var art = Rect(stack, "Ilustracao");
            Size(art, height: 112f);
            var artBlock = Place(Rect(art, "Bloco"), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(96f, 96f),
                pivot: new Vector2(0f, 0.5f));
            Img(artBlock, kit.Rounded(OdontoARTheme.RadiusXl), OdontoARTheme.AccentSoft, raycast: false);
            Place(Icon(artBlock, "phone", 48f, OdontoARTheme.Accent).rectTransform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(48f, 48f));

            Text(stack, "Acesso à câmera", kit.Bold, OdontoARTheme.LargeTitle, OdontoARTheme.Ink);
            Text(stack, "O OdontoAR usa a câmera para reconhecer a superfície da mesa e posicionar a arcada 3D sobre ela.",
                kit.Regular, OdontoARTheme.Body, OdontoARTheme.InkMuted);
            Spacer(stack, 8f);
            InfoRow(stack, "plane", "Detecta superfícies planas", "Mesa, bancada ou carteira");
            InfoRow(stack, "device", "Processado no aparelho", "Nenhuma imagem é gravada ou enviada");
            Spacer(stack, 4f);
            var denied = Text(stack,
                "O acesso à câmera está desligado. Toque em Abrir Ajustes e ative a Câmera para o OdontoAR.",
                kit.Regular, OdontoARTheme.Footnote, OdontoARTheme.Danger).gameObject;

            var allow = PrimaryButton(safe, "Permitir câmera", "eye");
            BottomStretch((RectTransform)allow.transform, 64f, OdontoARTheme.SizeButton);
            var later = TextButton(safe, "Agora não");
            BottomStretch((RectTransform)later.transform, 12f, OdontoARTheme.SizeTouch);
            UnityEventTools.AddPersistentListener(later.onClick, router.GoToOpening);

            var logic = screen.AddComponent<PermissionScreen>();
            Set(logic, "router", router);
            Set(logic, "allowButton", allow);
            Set(logic, "allowLabel", allow.GetComponentInChildren<TextMeshProUGUI>());
            Set(logic, "deniedNote", denied);
            return screen;
        }

        static void InfoRow(Transform parent, string icon, string title, string detail)
        {
            var row = HStack(parent, "Info", 14f);
            row.childAlignment = TextAnchor.MiddleLeft;
            var block = Rect(row.transform, "Icone");
            Size(block, 44f, 44f);
            Img(block, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.Bg700, raycast: false);
            Place(Icon(block, icon, 24f, OdontoARTheme.Accent).rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(24f, 24f));

            var texts = VStack(row.transform, "Textos", 2f);
            Size((RectTransform)texts.transform, flexibleWidth: 1f);
            Text(texts.transform, title, kit.SemiBold, OdontoARTheme.Headline, OdontoARTheme.Ink);
            Text(texts.transform, detail, kit.Regular, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);
        }

        // ---------- Contador de FPS (T11) ----------

        /// <summary>Pílula com o FPS no canto superior esquerdo, por cima de tudo; liga com 3 dedos.</summary>
        static void BuildFrameRateOverlay(RectTransform canvas)
        {
            var safe = Stretch(Rect(canvas, "Diagnostico"));
            safe.gameObject.AddComponent<OdontoAR.UI.SafeArea>();

            var pill = Place(Rect(safe, "ContadorFPS"), new Vector2(0f, 1f), new Vector2(12f, -56f), new Vector2(0f, 28f),
                pivot: new Vector2(0f, 1f));
            Img(pill, kit.Rounded(14f), OdontoARTheme.Glass, raycast: false);
            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            pill.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var label = Text(pill, "-- FPS", kit.Mono, 13f, OdontoARTheme.Accent, TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            pill.gameObject.SetActive(false);

            var logic = canvas.gameObject.AddComponent<FrameRateOverlay>();
            Set(logic, "overlay", pill.gameObject);
            Set(logic, "label", label);
        }

        // ---------- Tela 10: Registro de uso ----------

        static GameObject BuildLog(RectTransform canvas, ScreenRouter router, UsageRecorder recorder)
        {
            var screen = FullScreen(canvas, "Tela10_Registro", OdontoARTheme.Bg900);
            var safe = SafeContent(screen);
            var back = BackButton(safe, "Arcadas");
            UnityEventTools.AddPersistentListener(back.onClick, router.GoToAR);

            var stack = TopStack(safe, 56f);
            ((VerticalLayoutGroup)stack.GetComponent<LayoutGroup>()).spacing = 10f;
            Text(stack, "Registro de uso", kit.Bold, OdontoARTheme.LargeTitle, OdontoARTheme.Ink);

            SectionLabel(stack, "PARTICIPANTE");
            var list = VStack(stack, "Lista", 0f);
            Img((RectTransform)list.transform, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.Bg700, raycast: false);
            var code = ListCell(list.transform, "person", "Código", kit.Mono, OdontoARTheme.Ink);
            var divider = Rect(list.transform, "Divisor");
            Size(divider, height: 1f);
            Img(Stretch(Rect(divider, "Linha")), null, OdontoARTheme.Line, raycast: false, sliced: false)
                .rectTransform.offsetMin = new Vector2(52f, 0f);
            var last = ListCell(list.transform, "clock", "Última sessão", kit.Regular, OdontoARTheme.InkMuted);

            SectionLabel(stack, "RESUMO");
            var metrics = HStack(stack, "Metricas", 10f);
            metrics.childForceExpandWidth = metrics.childForceExpandHeight = true;
            Size((RectTransform)metrics.transform, height: 84f);
            var total = Metric(metrics.transform, "Tempo total de uso");
            var sessions = Metric(metrics.transform, "Sessões");

            SectionLabel(stack, "CAMADAS MAIS ACESSADAS");
            var chart = VStack(stack, "Grafico", 12f);
            chart.padding = new RectOffset(16, 16, 16, 16);
            Img((RectTransform)chart.transform, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.Bg700, raycast: false);
            var labels = new Object[4];
            var fills = new Object[4];
            var images = new Object[4];
            var counts = new Object[4];
            for (var i = 0; i < 4; i++)
            {
                var row = HStack(chart.transform, "Barra", 10f);
                row.childAlignment = TextAnchor.MiddleLeft;
                var label = Text(row.transform, "Camada", kit.Medium, OdontoARTheme.Subheadline, OdontoARTheme.Ink);
                Size(label.rectTransform, width: 64f);
                var track = Rect(row.transform, "Trilho");
                Size(track, height: 10f, flexibleWidth: 1f);
                Img(track, kit.Rounded(5f), OdontoARTheme.WithAlpha(Color.white, 0.07f), raycast: false);
                var fill = Rect(track, "Preenchimento");
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(0.5f, 1f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
                var fillImage = Img(fill, kit.Rounded(5f), OdontoARTheme.ToothRoot, raycast: false);
                var count = Text(row.transform, "0", kit.Mono, OdontoARTheme.Subheadline, OdontoARTheme.Ink,
                    TextAlignmentOptions.MidlineRight);
                Size(count.rectTransform, width: 44f);
                labels[i] = label;
                fills[i] = fill;
                images[i] = fillImage;
                counts[i] = count;
            }
            Text(chart.transform, "Número de vezes que cada camada foi ativada", kit.Regular, OdontoARTheme.Footnote,
                OdontoARTheme.InkMuted);

            var export = PrimaryButton(safe, "Exportar registro (JSON)", "share");
            BottomStretch((RectTransform)export.transform, 44f, OdontoARTheme.SizeButton);
            var note = HStack(safe, "Privacidade", 6f);
            note.childAlignment = TextAnchor.MiddleCenter;
            BottomStretch((RectTransform)note.transform, 12f, 20f);
            Size(Icon(note.transform, "lock", 14f, OdontoARTheme.InkMuted).rectTransform, 14f, 14f);
            Text(note.transform, "O registro fica salvo apenas neste aparelho", kit.Regular, OdontoARTheme.Footnote,
                OdontoARTheme.InkMuted);

            var logic = screen.AddComponent<UsageLogScreen>();
            Set(logic, "recorder", recorder);
            Set(logic, "code", code);
            Set(logic, "lastSession", last);
            Set(logic, "totalTime", total);
            Set(logic, "sessions", sessions);
            SetArray(logic, "barLabels", labels);
            SetArray(logic, "barFills", fills);
            SetArray(logic, "barImages", images);
            SetArray(logic, "barCounts", counts);
            Set(logic, "exportButton", export);
            return screen;
        }

        static void SectionLabel(Transform parent, string text)
        {
            Spacer(parent, 4f);
            Text(parent, text, kit.SemiBold, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);
        }

        /// <summary>Célula de lista agrupada (52 pt): ícone ciano, rótulo e valor à direita.</summary>
        static TextMeshProUGUI ListCell(Transform parent, string icon, string label, TMP_FontAsset valueFont, Color valueColor)
        {
            var row = HStack(parent, "Celula", 12f);
            row.padding = new RectOffset(16, 16, 0, 0);
            row.childAlignment = TextAnchor.MiddleLeft;
            Size((RectTransform)row.transform, height: 52f);
            Size(Icon(row.transform, icon, 22f, OdontoARTheme.Accent).rectTransform, 22f, 22f);
            Size(Text(row.transform, label, kit.Regular, OdontoARTheme.Body, OdontoARTheme.Ink).rectTransform,
                flexibleWidth: 1f);
            return Text(row.transform, "—", valueFont, OdontoARTheme.Body, valueColor, TextAlignmentOptions.MidlineRight);
        }

        /// <summary>Bloco de métrica: rótulo pequeno e valor grande.</summary>
        static TextMeshProUGUI Metric(Transform parent, string label)
        {
            var block = VStack(parent, "Metrica", 4f);
            block.padding = new RectOffset(16, 16, 14, 14);
            block.childAlignment = TextAnchor.MiddleLeft;
            Img((RectTransform)block.transform, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.Bg700, raycast: false);
            Text(block.transform, label, kit.Regular, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);
            var value = Text(block.transform, "0", kit.Bold, 26f, OdontoARTheme.Ink);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            return value;
        }

        // ---------- Telas 4–8: RA ----------

        static (GameObject ar, HelpSheet help) BuildAR(RectTransform canvas, ScreenRouter router, Context context)
        {
            var screen = Stretch(Rect(canvas, "Tela_RA")).gameObject;

            BuildCardLayer((RectTransform)screen.transform, context);

            var hud = SafeContent(screen);
            hud.name = "HUD";

            BuildTopBar(hud, router, context, out var helpButton);
            var status = BuildStatus(hud, context);
            var gestureHint = BuildPanel(hud, context);
            Set(status, "gestureHint", gestureHint);

            var help = BuildHelp((RectTransform)screen.transform, context);
            UnityEventTools.AddPersistentListener(helpButton.onClick, help.Open);
            return (screen, help);
        }

        static void BuildTopBar(RectTransform hud, ScreenRouter router, Context context, out Button helpButton)
        {
            var bar = Place(Rect(hud, "BarraSuperior"), new Vector2(0.5f, 1f), new Vector2(0f, -6f),
                new Vector2(0f, 100f), pivot: new Vector2(0.5f, 1f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.sizeDelta = new Vector2(0f, 100f);

            var back = GlassCircle(bar, "chevL");
            Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(12f, 0f),
                new Vector2(OdontoARTheme.SizeTouch, OdontoARTheme.SizeTouch), pivot: new Vector2(0f, 1f));
            UnityEventTools.AddPersistentListener(back.onClick, router.GoToOpening);

            var title = Text(bar, "Arcadas", kit.SemiBold, OdontoARTheme.Headline, OdontoARTheme.Ink,
                TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(200f, OdontoARTheme.SizeTouch),
                pivot: new Vector2(0.5f, 1f));

            helpButton = GlassCircle(bar, "q");
            Place((RectTransform)helpButton.transform, new Vector2(1f, 1f), new Vector2(-12f, 0f),
                new Vector2(OdontoARTheme.SizeTouch, OdontoARTheme.SizeTouch), pivot: new Vector2(1f, 1f));

            // Registro de uso (tela 10), ao lado da ajuda.
            var logButton = GlassCircle(bar, "clock");
            Place((RectTransform)logButton.transform, new Vector2(1f, 1f), new Vector2(-12f - OdontoARTheme.SizeTouch - 8f, 0f),
                new Vector2(OdontoARTheme.SizeTouch, OdontoARTheme.SizeTouch), pivot: new Vector2(1f, 1f));
            UnityEventTools.AddPersistentListener(logButton.onClick, router.GoToLog);

            // Seletor segmentado (RF03), só com a arcada posicionada.
            var segment = Place(Rect(bar, "SeletorArcada"), new Vector2(0.5f, 1f), new Vector2(0f, -54f),
                new Vector2(300f, 36f), pivot: new Vector2(0.5f, 1f));
            Glass(segment, OdontoARTheme.RadiusSm, raycast: true);
            var layout = segment.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.spacing = 2f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            var upper = SegmentButton(segment, "Superior");
            var lower = SegmentButton(segment, "Inferior");
            var both = SegmentButton(segment, "Ambas");

            var logic = bar.gameObject.AddComponent<ArchSegment>();
            Set(logic, "holder", context.Holder);
            Set(logic, "segmentRoot", segment.gameObject);
            Set(logic, "upperButton", upper);
            Set(logic, "lowerButton", lower);
            Set(logic, "bothButton", both);
        }

        static Button SegmentButton(RectTransform parent, string label)
        {
            var rect = Rect(parent, $"Segmento{label}");
            var image = Img(rect, kit.Rounded(8f), OdontoARTheme.SegmentActive, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            Stretch(Text(rect, label, kit.SemiBold, OdontoARTheme.Subheadline, OdontoARTheme.Bg900,
                TextAlignmentOptions.Center).rectTransform);
            return button;
        }

        static ARStatus BuildStatus(RectTransform hud, Context context)
        {
            var status = Stretch(Rect(hud, "Status"));

            // Tela 4: procurando superfície.
            var searching = Stretch(Rect(status, "Procurando"));
            StatusPill(searching, "Procurando superfície…", null, out var pillRow);
            var searchingLabel = pillRow.GetComponentInChildren<TextMeshProUGUI>();
            var spinner = Rect(pillRow, "Indicador");
            Size(spinner, 18f, 18f);
            spinner.SetAsFirstSibling();
            Img(spinner, kit.Spinner(18f, 2.5f), OdontoARTheme.Accent, raycast: false, sliced: false);
            InstructionCard(searching, "phone", "Mova o celular devagar sobre a mesa",
                "Ambientes bem iluminados e superfícies com textura são detectados mais rápido.");

            // Tela 5: superfície encontrada.
            var found = Stretch(Rect(status, "Encontrada"));
            StatusPill(found, "Superfície detectada", "check", out _);
            InstructionCard(found, "tap", "Toque para posicionar a arcada", "Aponte o retículo para o centro da mesa.");

            // Rastreamento perdido com a arcada posicionada.
            var unstable = Stretch(Rect(status, "Instavel"));
            var unstablePill = StatusPill(unstable, "Rastreamento instável. Aponte de novo para a mesa.", "scope", out _);
            unstablePill.anchoredPosition = new Vector2(0f, -108f);

            var logic = status.gameObject.AddComponent<ARStatus>();
            Set(logic, "holder", context.Holder);
            Set(logic, "tapToPlace", context.TapToPlace);
            Set(logic, "searching", searching.gameObject);
            Set(logic, "found", found.gameObject);
            Set(logic, "unstable", unstable.gameObject);
            Set(logic, "spinner", spinner);
            Set(logic, "searchingLabel", searchingLabel);
            return logic; // a dica de gestos fica presa ao painel e é ligada depois
        }

        /// <summary>PilulaStatus no topo, abaixo da barra (36 pt, vidro).</summary>
        static RectTransform StatusPill(RectTransform parent, string text, string icon, out RectTransform row)
        {
            var pill = Place(Rect(parent, "Pilula"), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(0f, 36f),
                pivot: new Vector2(0.5f, 1f));
            Glass(pill, 18f, raycast: false);
            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 16, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (icon != null)
            {
                var badge = Rect(pill, "Selo");
                Size(badge, 18f, 18f);
                Img(badge, kit.Circle, OdontoARTheme.Accent, raycast: false, sliced: false);
                Place(Icon(badge, icon, 12f, OdontoARTheme.Bg900).rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(12f, 12f));
            }

            Text(pill, text, kit.SemiBold, 14f, OdontoARTheme.Ink, TextAlignmentOptions.MidlineLeft)
                .textWrappingMode = TextWrappingModes.NoWrap;
            row = pill;
            return pill;
        }

        /// <summary>Cartão de instrução na base (telas 4 e 5).</summary>
        static void InstructionCard(RectTransform parent, string icon, string title, string detail)
        {
            var card = Rect(parent, "Instrucao");
            card.anchorMin = new Vector2(0f, 0f);
            card.anchorMax = new Vector2(1f, 0f);
            card.pivot = new Vector2(0.5f, 0f);
            card.offsetMin = new Vector2(12f, 8f);
            card.offsetMax = new Vector2(-12f, 8f);
            Glass(card, OdontoARTheme.RadiusLg, raycast: false);
            var layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var block = Rect(card, "Icone");
            Size(block, 52f, 52f);
            Img(block, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.GlassFill, raycast: false);
            Place(Icon(block, icon, 28f, OdontoARTheme.Accent).rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(28f, 28f));

            var texts = VStack(card, "Textos", 4f);
            Size((RectTransform)texts.transform, flexibleWidth: 1f);
            Text(texts.transform, title, kit.SemiBold, OdontoARTheme.Headline, OdontoARTheme.Ink);
            Text(texts.transform, detail, kit.Regular, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);
        }

        // ---------- Painel de camadas ----------

        /// <summary>Monta o painel e devolve a dica de gestos, que fica logo acima dele.</summary>
        static GameObject BuildPanel(RectTransform hud, Context context)
        {
            var area = Stretch(Rect(hud, "PainelArea"));

            var panel = Rect(area, "PainelCamadas");
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.offsetMin = new Vector2(12f, 8f);
            panel.offsetMax = new Vector2(-12f, 8f);
            Glass(panel, OdontoARTheme.RadiusXl, raycast: true); // toques no painel não giram o modelo
            var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(16, 16, 8, 16);
            column.spacing = 12f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Dica de gestos logo acima do painel (fora do layout).
            var hint = Place(Rect(panel, "DicaGestos"), new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(0f, 28f),
                pivot: new Vector2(0.5f, 0f));
            hint.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Img(hint, kit.Rounded(14f), OdontoARTheme.WithAlpha(OdontoARTheme.Glass, 0.45f), raycast: false);
            var hintLayout = hint.gameObject.AddComponent<HorizontalLayoutGroup>();
            hintLayout.padding = new RectOffset(12, 12, 0, 0);
            hintLayout.childAlignment = TextAnchor.MiddleCenter;
            hintLayout.childControlWidth = hintLayout.childControlHeight = true;
            hint.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text(hint, "Arraste para girar • Pinça para ampliar", kit.Medium, OdontoARTheme.Caption, OdontoARTheme.Ink,
                TextAlignmentOptions.Center).textWrappingMode = TextWrappingModes.NoWrap;

            // Puxador.
            var grabber = Rect(panel, "Puxador");
            Size(grabber, height: 5f);
            Img(Place(Rect(grabber, "Barra"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 5f)),
                kit.Rounded(2.5f), OdontoARTheme.WithAlpha(Color.white, 0.3f), raycast: false);

            // Cabeçalho: toca para recolher/abrir.
            var header = Rect(panel, "Cabecalho");
            Size(header, height: 32f);
            var headerHit = Img(header, kit.Rounded(8f), OdontoARTheme.WithAlpha(OdontoARTheme.GlassFill, 0f), raycast: true);
            var toggle = header.gameObject.AddComponent<Button>();
            toggle.targetGraphic = headerHit;
            toggle.transition = Selectable.Transition.None;
            var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.spacing = 8f;
            headerRow.childAlignment = TextAnchor.MiddleLeft;
            headerRow.childControlWidth = headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = headerRow.childForceExpandHeight = false;
            Text(header, "Camadas", kit.SemiBold, OdontoARTheme.Headline, OdontoARTheme.Ink);
            var count = Text(header, "4 de 4 visíveis", kit.Regular, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);
            Size(count.rectTransform, flexibleWidth: 1f);

            var dotsRow = HStack(header, "Bolinhas", 6f);
            dotsRow.childAlignment = TextAnchor.MiddleRight;
            var dots = new Image[4];
            for (var i = 0; i < dots.Length; i++)
            {
                var dot = Rect(dotsRow.transform, $"Bolinha{i}");
                Size(dot, 10f, 10f);
                dots[i] = Img(dot, kit.Circle, Color.white, raycast: false, sliced: false);
            }
            var chevron = Icon(header, "chevD", 18f, OdontoARTheme.InkMuted);
            Size(chevron.rectTransform, 18f, 18f);

            // Conteúdo aberto.
            var expanded = VStack(panel, "Aberto", 12f);
            var chips = HStack(expanded.transform, "Chips", 8f);
            chips.childForceExpandWidth = chips.childForceExpandHeight = true;
            Size((RectTransform)chips.transform, height: OdontoARTheme.SizeChip);
            BuildChip(chips.transform, context, Layer.Crown, "Coroa");
            BuildChip(chips.transform, context, Layer.Root, "Raiz");
            BuildChip(chips.transform, context, Layer.Bone, "Osso");
            BuildChip(chips.transform, context, Layer.Nerves, "Nervos");

            var opening = VStack(expanded.transform, "Abertura", 6f);
            var openingGroup = opening.gameObject.AddComponent<CanvasGroup>();
            var labelRow = HStack(opening.transform, "Rotulo", 8f);
            Size(Text(labelRow.transform, "Abertura entre arcadas", kit.Medium, OdontoARTheme.Subheadline,
                OdontoARTheme.Ink).rectTransform, flexibleWidth: 1f);
            var value = Text(labelRow.transform, "0°", kit.Mono, OdontoARTheme.Subheadline, OdontoARTheme.Accent,
                TextAlignmentOptions.MidlineRight);
            var slider = BuildSlider(opening.transform);

            var actions = HStack(expanded.transform, "Acoes", 8f);
            actions.childForceExpandWidth = actions.childForceExpandHeight = true;
            Size((RectTransform)actions.transform, height: 48f);
            var reset = GlassAction(actions.transform, "reset", "Restaurar vista");
            var reposition = GlassAction(actions.transform, "scope", "Reposicionar");
            EqualWidth((RectTransform)reset.transform);
            EqualWidth((RectTransform)reposition.transform);
            UnityEventTools.AddPersistentListener(reset.onClick, context.Holder.ResetView);
            UnityEventTools.AddPersistentListener(reposition.onClick, context.TapToPlace.Reposition);

            var logic = area.gameObject.AddComponent<LayerPanel>();
            Set(logic, "holder", context.Holder);
            Set(logic, "panelRoot", panel.gameObject);
            Set(logic, "expanded", expanded.gameObject);
            Set(logic, "collapsedDots", dotsRow.gameObject);
            SetArray(logic, "dots", dots);
            Set(logic, "toggleButton", toggle);
            Set(logic, "toggleIcon", chevron);
            Set(logic, "chevronDown", kit.Icon("chevD"));
            Set(logic, "chevronUp", kit.Icon("chevU"));
            Set(logic, "countLabel", count);
            Set(logic, "openingSlider", slider);
            Set(logic, "openingValue", value);
            Set(logic, "openingGroup", openingGroup);
            return hint.gameObject;
        }

        static void BuildChip(Transform parent, Context context, Layer layer, string label)
        {
            var chip = Rect(parent, $"Chip{label}");
            EqualWidth(chip);
            var background = Img(chip, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.GlassFill, raycast: true);
            var button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            var border = Img(Stretch(Rect(chip, "Borda")), kit.Stroke(OdontoARTheme.RadiusMd, 1.5f), Color.white,
                raycast: false);

            var content = VStack(chip, "Conteudo", 2f);
            Stretch((RectTransform)content.transform);
            content.padding = new RectOffset(10, 8, 10, 8);
            content.childAlignment = TextAnchor.UpperLeft;
            content.childForceExpandWidth = false; // a amostra mantém 18 × 18

            var swatchHolder = Rect(content.transform, "Amostra");
            Size(swatchHolder, 18f, 18f);
            var swatch = Img(Stretch(Rect(swatchHolder, "Cheia")), kit.Circle, Color.white, raycast: false, sliced: false);
            var ring = Img(Stretch(Rect(swatchHolder, "Anel")), kit.Ring(18f, 2f), Color.white, raycast: false, sliced: false);
            var check = Icon(swatchHolder, "check", 12f, OdontoARTheme.Bg900);
            Place(check.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f));
            Spacer(content.transform, 4f);

            var name = Text(content.transform, label, kit.SemiBold, OdontoARTheme.Subheadline, OdontoARTheme.Ink);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            var status = Text(content.transform, "Visível", kit.Medium, OdontoARTheme.Caption, OdontoARTheme.InkMuted);

            var logic = chip.gameObject.AddComponent<LayerChip>();
            Set(logic, "holder", context.Holder);
            var serialized = new SerializedObject(logic);
            serialized.FindProperty("layer").enumValueIndex = (int)layer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Set(logic, "button", button);
            Set(logic, "border", border);
            Set(logic, "swatch", swatch);
            Set(logic, "swatchRing", ring);
            Set(logic, "check", check);
            Set(logic, "label", name);
            Set(logic, "status", status);
        }

        /// <summary>SliderAbertura: trilho 4 pt, preenchimento ciano, polegar branco de 28 pt.</summary>
        static Slider BuildSlider(Transform parent)
        {
            var root = Rect(parent, "SliderAbertura");
            Size(root, height: 28f);
            var slider = root.gameObject.AddComponent<Slider>();

            var track = Rect(root, "Trilho");
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(0f, 4f);
            Img(track, kit.Rounded(2f), OdontoARTheme.GlassFill, raycast: true);

            var fillArea = Rect(root, "AreaPreenchimento");
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-28f, 4f);
            var fill = Rect(fillArea, "Preenchimento");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = new Vector2(14f, 0f);
            Img(fill, kit.Rounded(2f), OdontoARTheme.Accent, raycast: false);

            var handleArea = Stretch(Rect(root, "AreaPolegar"));
            handleArea.offsetMin = new Vector2(14f, 0f);
            handleArea.offsetMax = new Vector2(-14f, 0f);
            var handle = Rect(handleArea, "Polegar");
            handle.sizeDelta = new Vector2(28f, 0f);
            var handleImage = Img(handle, kit.Circle, Color.white, raycast: true, sliced: false);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.transition = Selectable.Transition.None;
            return slider;
        }

        // ---------- Cartão de estrutura (tela 8) ----------

        static void BuildCardLayer(RectTransform screen, Context context)
        {
            var layer = Stretch(Rect(screen, "CamadaCartao"));

            var line = Rect(layer, "Linha");
            line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
            line.pivot = new Vector2(0.5f, 0f);
            line.sizeDelta = new Vector2(2f, 0f);
            Img(line, null, OdontoARTheme.Accent, raycast: false, sliced: false);

            var dot = Place(Rect(layer, "Ancora"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            Img(dot, kit.Circle, OdontoARTheme.Accent, raycast: false, sliced: false);

            var card = Place(Rect(layer, "CartaoEstrutura"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 0f));
            Glass(card, OdontoARTheme.RadiusLg, raycast: true);
            var column = card.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(16, 16, 16, 16);
            column.spacing = 12f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var top = HStack(card, "Topo", 12f);
            top.childAlignment = TextAnchor.UpperLeft;

            var badge = Rect(top.transform, "SeloFDI");
            Size(badge, 60f, 60f);
            Img(badge, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.Accent, raycast: false);
            var badgeColumn = VStack(badge, "Textos", -2f);
            Stretch((RectTransform)badgeColumn.transform);
            badgeColumn.childAlignment = TextAnchor.MiddleCenter;
            Text(badgeColumn.transform, "FDI", kit.SemiBold, 11f, OdontoARTheme.OnAccent, TextAlignmentOptions.Center);
            var number = Text(badgeColumn.transform, "36", kit.Mono, 26f, OdontoARTheme.OnAccent, TextAlignmentOptions.Center);

            var texts = VStack(top.transform, "Textos", 4f);
            Size((RectTransform)texts.transform, flexibleWidth: 1f);
            var title = Text(texts.transform, "Primeiro molar inferior esquerdo — 36", kit.SemiBold, OdontoARTheme.Headline,
                OdontoARTheme.Ink);
            var layerRow = HStack(texts.transform, "Camada", 6f);
            layerRow.childAlignment = TextAnchor.MiddleLeft;
            var layerDot = Rect(layerRow.transform, "Bolinha");
            Size(layerDot, 10f, 10f);
            var layerDotImage = Img(layerDot, kit.Circle, OdontoARTheme.ToothRoot, raycast: false, sliced: false);
            var layerText = Text(layerRow.transform, "Camada: Raiz", kit.Regular, OdontoARTheme.Subheadline,
                OdontoARTheme.InkMuted);
            var meta = Text(texts.transform, "Quadrante 3 · Dente 6", kit.Regular, OdontoARTheme.Footnote,
                OdontoARTheme.InkFaint);

            var close = IconButton(top.transform, "x", 14f, 28f, OdontoARTheme.GlassFill, OdontoARTheme.InkMuted);
            Size((RectTransform)close.transform, 28f, 28f);

            var closeWide = GlassAction(card, null, "Fechar");
            Size((RectTransform)closeWide.transform, height: OdontoARTheme.SizeTouch);

            var logic = layer.gameObject.AddComponent<StructureCard>();
            Set(logic, "holder", context.Holder);
            Set(logic, "catalog", context.Catalog);
            Set(logic, "selection", context.Selection);
            Set(logic, "arCamera", context.Camera);
            Set(logic, "card", card);
            Set(logic, "badge", badge.gameObject);
            Set(logic, "badgeNumber", number);
            Set(logic, "title", title);
            Set(logic, "layerDot", layerDotImage);
            Set(logic, "layerText", layerText);
            Set(logic, "meta", meta);
            SetArray(logic, "closeButtons", new Object[] { close, closeWide });
            Set(logic, "line", line);
            Set(logic, "anchorDot", dot);

            card.gameObject.SetActive(false);
            line.gameObject.SetActive(false);
            dot.gameObject.SetActive(false);
        }

        // ---------- Tela 9: ajuda e créditos ----------

        static HelpSheet BuildHelp(RectTransform screen, Context context)
        {
            var container = Stretch(Rect(screen, "Ajuda"));
            var sheetRoot = Stretch(Rect(container, "Folha"));

            var scrim = Img(Stretch(Rect(sheetRoot, "Fundo")), null, OdontoARTheme.Scrim, raycast: true, sliced: false);
            var scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;

            // A folha passa da base da tela para esconder os cantos de baixo.
            var sheet = Rect(sheetRoot, "FolhaAjuda");
            sheet.anchorMin = new Vector2(0f, 0f);
            sheet.anchorMax = new Vector2(1f, 0.82f);
            sheet.offsetMin = new Vector2(0f, -40f);
            sheet.offsetMax = Vector2.zero;
            Img(sheet, kit.Rounded(32f), OdontoARTheme.Bg700, raycast: true);

            var scrollRect = Stretch(Rect(sheet, "Rolagem"));
            scrollRect.offsetMin = new Vector2(0f, 40f);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            var viewport = Stretch(Rect(scrollRect, "Janela"));
            viewport.gameObject.AddComponent<RectMask2D>();
            // Alvo de toque transparente: o arrasto chega ao ScrollRect por qualquer ponto da folha.
            Img(viewport, null, OdontoARTheme.WithAlpha(OdontoARTheme.Bg700, 0f), raycast: true, sliced: false);
            var content = Rect(viewport, "Conteudo");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(24, 24, 10, 48);
            column.spacing = 14f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            var grabber = Rect(content, "Puxador");
            Size(grabber, height: 5f);
            Img(Place(Rect(grabber, "Barra"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 5f)),
                kit.Rounded(2.5f), OdontoARTheme.WithAlpha(Color.white, 0.25f), raycast: false);

            var titleRow = HStack(content, "Titulo", 8f);
            titleRow.childAlignment = TextAnchor.MiddleLeft;
            Size(Text(titleRow.transform, "Como interagir", kit.Bold, OdontoARTheme.Title2, OdontoARTheme.Ink)
                .rectTransform, flexibleWidth: 1f);
            var close = IconButton(titleRow.transform, "x", 16f, 32f, OdontoARTheme.Bg800, OdontoARTheme.InkMuted);
            Size((RectTransform)close.transform, 32f, 32f);
            Text(content, "Gestos para explorar a arcada em RA.", kit.Regular, OdontoARTheme.Body, OdontoARTheme.InkMuted);

            GestureRow(content, "reset", "Arrastar", "Gira a arcada em qualquer direção.");
            GestureRow(content, "scope", "Pinça", "Amplia ou reduz. Girando os dois dedos, inclina a arcada para os lados.");
            GestureRow(content, "tap", "Tocar em uma estrutura", "Mostra o nome, a camada e a notação FDI.");
            GestureRow(content, "layers", "Botão de camadas", "Exibe ou oculta coroa, raiz, osso e nervos.");

            var divider = Rect(content, "Divisor");
            Size(divider, height: 1f);
            Img(divider, null, OdontoARTheme.Line, raycast: false, sliced: false);

            Text(content, "CRÉDITOS", kit.SemiBold, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);
            Text(content, CreditsText, kit.Regular, OdontoARTheme.Footnote, OdontoARTheme.InkMuted);

            Spacer(content, 4f);
            var done = PrimaryButton(content, "Entendi", null);
            Size((RectTransform)done.transform, height: OdontoARTheme.SizeButton);

            var logic = container.gameObject.AddComponent<HelpSheet>();
            Set(logic, "holder", context.Holder);
            Set(logic, "sheetRoot", sheetRoot.gameObject);
            SetArray(logic, "closeButtons", new Object[] { close, done, scrimButton });
            Set(logic, "scroll", scroll);

            sheetRoot.gameObject.SetActive(false);
            return logic;
        }

        static void GestureRow(RectTransform parent, string icon, string title, string detail)
        {
            var row = HStack(parent, "Gesto", 14f);
            row.childAlignment = TextAnchor.MiddleLeft;
            var block = Rect(row.transform, "Ilustracao");
            Size(block, 64f, 64f);
            Img(block, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.Bg800, raycast: false);
            Place(Icon(block, icon, 30f, OdontoARTheme.Accent).rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(30f, 30f));

            var texts = VStack(row.transform, "Textos", 2f);
            Size((RectTransform)texts.transform, flexibleWidth: 1f);
            Text(texts.transform, title, kit.SemiBold, OdontoARTheme.Headline, OdontoARTheme.Ink);
            Text(texts.transform, detail, kit.Regular, OdontoARTheme.Subheadline, OdontoARTheme.InkMuted);
        }

        // ---------- Componentes ----------

        static Button PrimaryButton(Transform parent, string label, string icon)
        {
            var rect = Rect(parent, $"Botao{label.Replace(" ", "")}");
            var image = Img(rect, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.Accent, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f); // ≈ accent-strong
            colors.disabledColor = Color.white; // o estado desabilitado troca a cor de fundo (Botao)
            button.colors = colors;

            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            if (icon != null)
                Size(Icon(rect, icon, 20f, OdontoARTheme.OnAccent).rectTransform, 20f, 20f);
            Text(rect, label, kit.SemiBold, OdontoARTheme.Headline, OdontoARTheme.OnAccent, TextAlignmentOptions.Center);
            return button;
        }

        static Button TextButton(Transform parent, string label)
        {
            var rect = Rect(parent, $"Botao{label.Replace(" ", "")}");
            var image = Img(rect, null, OdontoARTheme.WithAlpha(OdontoARTheme.Bg900, 0f), raycast: true, sliced: false);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            Stretch(Text(rect, label, kit.SemiBold, OdontoARTheme.Headline, OdontoARTheme.Accent,
                TextAlignmentOptions.Center).rectTransform);
            return button;
        }

        static Button BackButton(RectTransform parent, string label = "Voltar")
        {
            var rect = Place(Rect(parent, "Voltar"), new Vector2(0f, 1f), new Vector2(12f, -6f), new Vector2(110f, 44f),
                pivot: new Vector2(0f, 1f));
            var image = Img(rect, null, OdontoARTheme.WithAlpha(OdontoARTheme.Bg900, 0f), raycast: true, sliced: false);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 2f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            Size(Icon(rect, "chevL", 24f, OdontoARTheme.Accent).rectTransform, 24f, 24f);
            Text(rect, label, kit.Regular, OdontoARTheme.Body, OdontoARTheme.Accent);
            return button;
        }

        /// <summary>BotaoVidro circular de 44 pt com ícone branco de 22 pt.</summary>
        static Button GlassCircle(RectTransform parent, string icon)
        {
            var rect = Rect(parent, $"Botao_{icon}");
            var image = Glass(rect, OdontoARTheme.SizeTouch * 0.5f, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            button.colors = colors;
            Place(Icon(rect, icon, 22f, OdontoARTheme.Ink).rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(22f, 22f));
            return button;
        }

        /// <summary>Ação de painel (48 pt, vidro claro, ícone ciano e rótulo branco).</summary>
        static Button GlassAction(Transform parent, string icon, string label)
        {
            var rect = Rect(parent, $"Acao{label.Replace(" ", "")}");
            var image = Img(rect, kit.Rounded(OdontoARTheme.RadiusMd), OdontoARTheme.GlassFill, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            button.colors = colors;
            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            if (icon != null)
                Size(Icon(rect, icon, 20f, OdontoARTheme.Accent).rectTransform, 20f, 20f);
            Text(rect, label, kit.SemiBold, OdontoARTheme.Subheadline, OdontoARTheme.Ink, TextAlignmentOptions.Center)
                .textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        static Button IconButton(Transform parent, string icon, float iconSize, float size, Color background, Color tint)
        {
            var rect = Rect(parent, $"Botao_{icon}");
            rect.sizeDelta = new Vector2(size, size);
            var image = Img(rect, kit.Rounded(size * 0.5f), background, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Place(Icon(rect, icon, iconSize, tint).rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(iconSize, iconSize));
            return button;
        }

        /// <summary>Material de vidro: fundo glass e borda glass-line de 1 pt (sem desfoque: o Unity UI não tem).</summary>
        static Image Glass(RectTransform rect, float radius, bool raycast)
        {
            var image = Img(rect, kit.Rounded(radius), OdontoARTheme.Glass, raycast);
            var border = Stretch(Rect(rect, "Borda"));
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Img(border, kit.Stroke(radius, 1f), OdontoARTheme.GlassLine, raycast: false);
            return image;
        }

        // ---------- Primitivas ----------

        static GameObject FullScreen(RectTransform canvas, string name, Color background)
        {
            var rect = Stretch(Rect(canvas, name));
            Img(rect, null, background, raycast: true, sliced: false);
            return rect.gameObject;
        }

        static RectTransform SafeContent(GameObject screen)
        {
            var rect = Stretch(Rect((RectTransform)screen.transform, "AreaSegura"));
            rect.gameObject.AddComponent<OdontoAR.UI.SafeArea>();
            return rect;
        }

        /// <summary>Coluna de conteúdo das telas sem câmera, a partir do topo, com margem de 24 pt.</summary>
        static Transform TopStack(RectTransform parent, float top)
        {
            var rect = Rect(parent, "Conteudo");
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(OdontoARTheme.Space6, 0f);
            rect.offsetMax = new Vector2(-OdontoARTheme.Space6, -top);
            var column = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = OdontoARTheme.Space2;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            rect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        static void BottomStretch(RectTransform rect, float bottom, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(OdontoARTheme.Space6, bottom);
            rect.offsetMax = new Vector2(-OdontoARTheme.Space6, bottom + height);
        }

        static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static void Size(RectTransform rect, float width = -1f, float height = -1f, float flexibleWidth = -1f)
        {
            if (!rect.TryGetComponent<LayoutElement>(out var element))
                element = rect.gameObject.AddComponent<LayoutElement>();
            if (width >= 0f)
            {
                element.preferredWidth = element.minWidth = width;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            }
            if (height >= 0f)
            {
                element.preferredHeight = element.minHeight = height;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }
            if (flexibleWidth >= 0f)
                element.flexibleWidth = flexibleWidth;
        }

        /// <summary>Divide a largura da linha por igual entre os irmãos, sem depender do texto.</summary>
        static void EqualWidth(RectTransform rect)
        {
            if (!rect.TryGetComponent<LayoutElement>(out var element))
                element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 0f;
            element.flexibleWidth = 1f;
        }

        static void Spacer(Transform parent, float height) => Size(Rect(parent, "Espaco"), height: height);

        static Image Img(RectTransform rect, Sprite sprite, Color color, bool raycast, bool sliced = true)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sliced)
                image.type = Image.Type.Sliced;
            return image;
        }

        static Image Icon(Transform parent, string icon, float size, Color color)
        {
            var rect = Rect(parent, $"Icone_{icon}");
            rect.sizeDelta = new Vector2(size, size);
            var image = Img(rect, kit.Icon(icon), color, raycast: false, sliced: false);
            image.preserveAspect = true;
            return image;
        }

        static TextMeshProUGUI Text(Transform parent, string content, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        static HorizontalLayoutGroup HStack(Transform parent, string name, float spacing)
        {
            var rect = Rect(parent, name);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return layout;
        }

        static VerticalLayoutGroup VStack(Transform parent, string name, float spacing)
        {
            var rect = Rect(parent, name);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[OdontoAR] Campo \"{field}\" não existe em {target.GetType().Name}.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
