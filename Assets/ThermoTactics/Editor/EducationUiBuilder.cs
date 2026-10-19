using UnityEngine;
using UnityEngine.UI;
using static ThermoTactics.EditorTools.UiKit;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Builds the education screens in the design's style (woven red / gold / cream on forest green, 1280 × 720 frame):
    /// the Pre-/Post-Quiz (both scenes), the climate-fact pop-up card (battle scene) and the Climate Facts list (Main Menu).
    /// </summary>
    internal static class EducationUiBuilder
    {
        private static readonly Color ChoiceFill = new Color(0.078f, 0.11f, 0.09f, 1f);

        private static RectTransform Overlay(string name, Transform c, out RectTransform frame, float alpha = 0.86f)
        {
            var o = Img(name, c, new Color(0.024f, 0.04f, 0.03f, alpha), null, true); Stretch(o.rectTransform);
            frame = Frame("Frame", o.transform);
            AddPanelFx(o.gameObject, frame);
            return o.rectTransform;
        }

        // =====================================================================
        // Pre-Quiz / Post-Quiz
        // =====================================================================
        public static QuizPanel BuildQuiz(Transform c)
        {
            var root = Overlay("QuizPanel", c, out var f);
            var panel = root.gameObject.AddComponent<QuizPanel>();
            Ref(panel, "_root", root.gameObject);

            var d = Box("Dialog", f, Panel, Gold, 190, 46, 900, 628, 8f);
            Weave(d.transform, 0, 0, 900);
            Ref(panel, "_heading", TxtAt("Heading", d.transform, "PRE-QUIZ", Pixel, 34, Cream, 36, 34, 600, 40, TextAnchor.MiddleLeft));
            Ref(panel, "_intro", TxtAt("Intro", d.transform, "", Body, 15, CreamDim, 36, 78, 828, 40));
            At(Img("Divider", d.transform, Line), 0, 124, 900, 2);

            // ---- question page ----
            var q = NewRect("QuestionPage", d.transform);
            At(q, 0, 140, 900, 480);
            Ref(panel, "_questionPage", q.gameObject);
            Ref(panel, "_eyebrow", TxtAt("Eyebrow", q, "QUESTION 1 OF 5", BodyBold, 14, GoldLight, 36, 0, 828, 20, TextAnchor.MiddleLeft));
            Ref(panel, "_question", TxtAt("Question", q, "", BodyBold, 22, Cream, 36, 26, 828, 84));
            var choices = new Button[QuizData.ChoiceCount];
            var labels = new Text[QuizData.ChoiceCount];
            var letters = new Text[QuizData.ChoiceCount];
            string[] abc = { "A", "B", "C", "D" };
            for (int i = 0; i < QuizData.ChoiceCount; i++)
            {
                var b = Btn($"Choice_{abc[i]}", q, "", ChoiceFill, Cream, Slate, 36, 116 + i * 62, 828, 54, 18, 0f);
                letters[i] = TxtAt("Letter", b.transform, abc[i], Pixel, 22, Gold, 18, 0, 30, 54, TextAnchor.MiddleLeft);
                labels[i] = TxtAt("Label", b.transform, "", Body, 18, Cream, 58, 0, 754, 54, TextAnchor.MiddleLeft);
                choices[i] = b;
            }
            RefArray(panel, "_choices", choices);
            RefArray(panel, "_choiceLabels", labels);
            RefArray(panel, "_choiceLetters", letters);
            var dots = new Image[QuizData.QuestionCount];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = At(Img($"Dot_{i + 1}", q, Line), 36 + i * 24, 374, 14, 14);
                Border(dots[i].transform, Ink, "ui_frame2");
            }
            RefArray(panel, "_dots", dots);
            Ref(panel, "_message", TxtAt("Message", q, "", BodyBold, 14, Hex("#FF9A86"), 200, 370, 460, 22, TextAnchor.MiddleLeft));
            Ref(panel, "_backButton", Btn("BackButton", q, "BACK", Clear, Cream, Slate, 36, 410, 180, 52, 20, 0f));
            var next = Btn("NextButton", q, "NEXT", Gold, Ink, Ink, 684, 410, 180, 52, 22);
            Ref(panel, "_nextButton", next);
            Ref(panel, "_nextLabel", next.GetComponentInChildren<Text>());

            // ---- result page ----
            var r = NewRect("ResultPage", d.transform);
            At(r, 0, 140, 900, 480);
            Ref(panel, "_resultPage", r.gameObject);
            Ref(panel, "_resultScore", TxtAt("Score", r, "Pre-quiz: 3 / 5", Pixel, 36, Gold, 36, 0, 828, 44, TextAnchor.MiddleLeft));
            Ref(panel, "_resultBody", TxtAt("Body", r, "", Body, 16, Cream, 36, 50, 828, 60));

            var table = NewRect("CompareTable", r);
            At(table, 36, 116, 828, 252);
            Ref(panel, "_compareTable", table.gameObject);
            var header = At(Img("Header", table, Moss), 0, 0, 828, 36);
            Columns(header.transform, "TOPIC", "BEFORE", "AFTER", "RESULT", BodyBold, 14, Cream);
            var topic = new Text[QuizData.QuestionCount]; var before = new Text[QuizData.QuestionCount];
            var after = new Text[QuizData.QuestionCount]; var status = new Text[QuizData.QuestionCount];
            for (int i = 0; i < QuizData.QuestionCount; i++)
            {
                var row = At(Img($"Row_{i + 1}", table, new Color(1f, 1f, 1f, i % 2 == 0 ? 0.05f : 0.02f)), 0, 40 + i * 42, 828, 38);
                Text[] cols = Columns(row.transform, "-", "-", "-", "-", Body, 15, Cream);
                cols[0].font = BodyBold;
                topic[i] = cols[0]; before[i] = cols[1]; after[i] = cols[2]; status[i] = cols[3];
            }
            RefArray(panel, "_rowTopic", topic); RefArray(panel, "_rowBefore", before);
            RefArray(panel, "_rowAfter", after); RefArray(panel, "_rowStatus", status);
            Ref(panel, "_gain", TxtAt("Gain", r, "", BodyBold, 18, GoldLight, 36, 376, 600, 26, TextAnchor.MiddleLeft));
            Ref(panel, "_continueButton", Btn("ContinueButton", r, "CONTINUE", Gold, Ink, Ink, 664, 410, 200, 52, 22));
            r.gameObject.SetActive(false);
            // The root stays active in the scene: QuizPanel.Awake hides it on load (like the Results panel).
            return panel;
        }

        /// <summary>Topic | before | after | result columns of the comparison table.</summary>
        private static Text[] Columns(Transform row, string a, string b, string cText, string dText, Font font, int size, Color color)
        {
            Text Col(string name, string value, float x0, float x1)
            {
                var t = Txt(name, row, value, font, size, color, TextAnchor.MiddleLeft);
                t.rectTransform.anchorMin = new Vector2(x0, 0); t.rectTransform.anchorMax = new Vector2(x1, 1);
                t.rectTransform.offsetMin = new Vector2(14, 0); t.rectTransform.offsetMax = new Vector2(-6, 0);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                return t;
            }
            return new[]
            {
                Col("Topic", a, 0f, 0.32f),
                Col("Before", b, 0.32f, 0.47f),
                Col("After", cText, 0.47f, 0.62f),
                Col("Result", dText, 0.62f, 1f),
            };
        }

        // =====================================================================
        // Climate-fact pop-up (battle scene)
        // =====================================================================
        public static void BuildPopup(Transform c, PopupManager manager)
        {
            var root = Overlay("FactPopup", c, out var f, 0.7f);
            var d = Box("Card", f, Panel, Gold, 290, 150, 700, 420, 8f);
            Weave(d.transform, 0, 0, 700);
            var badge = At(Img("IconBack", d.transform, MossDim), 588, 40, 80, 80);
            Border(badge.transform, Moss, "ui_frame2");
            var icon = Icon("Icon", badge.transform, "icon_oxygen", Mint, 14, 14, 52);
            Ref(manager, "_root", root.gameObject);
            Ref(manager, "_icon", icon);
            Ref(manager, "_topic", TxtAt("Topic", d.transform, "CLIMATE FACT", BodyBold, 14, GoldLight, 32, 40, 540, 20, TextAnchor.MiddleLeft));
            Ref(manager, "_title", TxtAt("Title", d.transform, "", Pixel, 28, Cream, 32, 66, 540, 64, TextAnchor.UpperLeft));
            Ref(manager, "_body", TxtAt("Body", d.transform, "", Body, 18, Cream, 32, 136, 636, 190));
            Ref(manager, "_counter", TxtAt("Counter", d.transform, "", Body, 13, CreamDim, 32, 344, 430, 44, TextAnchor.MiddleLeft));
            Ref(manager, "_okButton", Btn("OkButton", d.transform, "GOT IT", Gold, Ink, Ink, 488, 340, 180, 52, 22));
            root.gameObject.SetActive(false);
        }

        // =====================================================================
        // Climate Facts list (Main Menu, from the level path)
        // =====================================================================
        public static FactsPanel BuildFacts(Transform c)
        {
            var root = Overlay("FactsPanel", c, out var f, 0.72f);
            var panel = root.gameObject.AddComponent<FactsPanel>();
            var d = Box("Dialog", f, Panel, Moss, 190, 60, 900, 600, 8f);
            Weave(d.transform, 0, 0, 900);
            TxtAt("Title", d.transform, "CLIMATE FACTS", Pixel, 34, Cream, 28, 36, 600, 40, TextAnchor.MiddleLeft);
            Ref(panel, "_subtitle", TxtAt("Sub", d.transform, "", Body, 15, CreamDim, 28, 80, 844, 22));

            var scroll = At(Img("Scroll", d.transform, new Color(0, 0, 0, 0.25f), null, true), 28, 112, 844, 400);
            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            var viewport = NewRect("Viewport", scroll.transform); Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false; vlg.spacing = 6;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport; sr.content = content; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30;

            var row = Img("RowTemplate", content, new Color(1f, 1f, 1f, 0.05f));
            var rowLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.childControlHeight = true; rowLayout.childControlWidth = true; rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = 4; rowLayout.padding = new RectOffset(16, 16, 12, 12);
            Txt("Head", row.transform, "-", BodyBold, 16, Cream);
            Txt("Body", row.transform, "-", Body, 15, CreamDim).lineSpacing = 1.05f;

            Ref(panel, "_footer", TxtAt("Footer", d.transform, "", Body, 13, CreamDim, 28, 524, 620, 40, TextAnchor.MiddleLeft));
            Ref(panel, "_closeButton", Btn("CloseButton", d.transform, "CLOSE", Gold, Ink, Ink, 712, 520, 160, 50, 20));
            Ref(panel, "_root", root.gameObject); Ref(panel, "_content", content); Ref(panel, "_rowTemplate", row.gameObject);
            return panel;
        }
    }
}
