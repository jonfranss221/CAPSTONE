using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Editor helpers that build uGUI from the approved UI designs (1280 × 720 boards).
    /// Positions use the design's top-left pixel coordinates, so the code reads like the design.
    /// Palette and fonts follow the design: woven red / gold / cream on deep forest green.
    /// </summary>
    internal static class UiKit
    {
        public const string Root = "Assets/ThermoTactics";
        public const string ArtUI = Root + "/Art/UI";
        public const string FontDir = Root + "/Fonts";

        // ---- palette (from the design boards) ----
        public static readonly Color Night = Hex("#0E1511");
        public static readonly Color Forest = Hex("#12201A");
        public static readonly Color Panel = Hex("#121A15");
        public static readonly Color PanelDeep = Hex("#0E1712");
        public static readonly Color Moss = Hex("#2F5D45");
        public static readonly Color MossDim = Hex("#15231B");
        public static readonly Color Line = Hex("#22332A");
        public static readonly Color Slate = Hex("#4A5A4E");
        public static readonly Color Gold = Hex("#E2AE2C");
        public static readonly Color GoldLight = Hex("#F3C95A");
        public static readonly Color Red = Hex("#A3241C");
        public static readonly Color RedSoft = Hex("#C0453A");
        public static readonly Color Cream = Hex("#F3EAD7");
        public static readonly Color CreamDim = Hex("#CFC6B2");
        public static readonly Color Muted = Hex("#A69D8B");
        public static readonly Color Ink = Hex("#1A140A");
        public static readonly Color Mint = Hex("#9BD3B0");
        public static readonly Color Clear = new Color(0, 0, 0, 0);

        public static Font Pixel, PixelMedium, Body, BodyBold;

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }
        public static Color Alpha(Color c, float a) { c.a = a; return c; }

        public static void LoadFonts()
        {
            Pixel = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/PixelifySans-Bold.ttf");
            PixelMedium = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/PixelifySans-Medium.ttf");
            Body = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/AtkinsonHyperlegible-Regular.ttf");
            BodyBold = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/AtkinsonHyperlegible-Bold.ttf");
            Font fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Pixel == null) { Debug.LogError("[Thermo-Tactics] Missing Pixelify Sans font"); Pixel = fallback; }
            if (PixelMedium == null) PixelMedium = Pixel;
            if (Body == null) { Debug.LogError("[Thermo-Tactics] Missing Atkinson Hyperlegible font"); Body = fallback; }
            if (BodyBold == null) BodyBold = Body;
        }

        public static Sprite Ui(string file)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUI}/{file}.png");
            if (s == null) Debug.LogError("[Thermo-Tactics] Missing UI sprite " + file);
            return s;
        }

        // ---- layout ----
        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;   // UI
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Design coordinates: x, y from the parent's top-left corner, in pixels.</summary>
        public static T At<T>(T c, float x, float y, float w, float h) where T : Component
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
            return c;
        }

        /// <summary>Anchored to the parent's top-right corner: x is the distance from the right edge.</summary>
        public static T AtRight<T>(T c, float right, float y, float w, float h) where T : Component
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-right, -y); rt.sizeDelta = new Vector2(w, h);
            return c;
        }

        /// <summary>A fixed 1280 × 720 design frame centred on the screen (wider phones show more background).</summary>
        public static RectTransform Frame(string name, Transform parent)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1280, 720);
            return rt;
        }

        // ---- graphics ----
        public static Image Img(string name, Transform parent, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : Ui("ui_pixel");
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>A border drawn with a 9-sliced frame sprite ("ui_frame3", "ui_frame2" or "ui_frame_dashed").</summary>
        public static Image Border(Transform parent, Color color, string frame = "ui_frame3")
        {
            var img = Img("Border", parent, color, Ui(frame));
            img.type = frame == "ui_frame_dashed" ? Image.Type.Tiled : Image.Type.Sliced;
            Stretch(img.rectTransform);
            return img;
        }

        /// <summary>The design's hard drop shadow ("box-shadow: 4px 4px 0 #000").</summary>
        public static void HardShadow(Graphic g, float d = 4f, float alpha = 1f)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, alpha);
            s.effectDistance = new Vector2(d, -d);
        }

        /// <summary>A panel: fill + border (+ optional hard shadow drawn as an offset block behind it).</summary>
        public static Image Box(string name, Transform parent, Color fill, Color border, float x, float y, float w, float h, float shadow = 0f, string frame = "ui_frame3")
        {
            var box = At(Img(name, parent, fill), x, y, w, h);
            if (shadow > 0f) HardShadow(box, shadow, 0.5f);
            if (border.a > 0f) Border(box.transform, border, frame);
            return box;
        }

        public static Image Weave(Transform parent, float x, float y, float w, float h = 20f)
        {
            var img = At(Img("Weave", parent, Color.white, Ui("weave_tile")), x, y, w, h);
            img.type = Image.Type.Tiled;
            img.pixelsPerUnitMultiplier = 20f / Mathf.Max(1f, h) * (100f / 100f);
            return img;
        }

        public static Image Icon(string name, Transform parent, string icon, Color color, float x, float y, float size)
        {
            var img = At(Img(name, parent, color, Ui(icon)), x, y, size, size);
            img.preserveAspect = true;
            return img;
        }

        // ---- text ----
        public static Text Txt(string name, Transform parent, string value, Font font, int size, Color color,
                               TextAnchor align = TextAnchor.UpperLeft)
        {
            var t = NewRect(name, parent).gameObject.AddComponent<Text>();
            t.font = font; t.text = value; t.fontSize = size; t.color = color; t.alignment = align;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1f;
            t.raycastTarget = false;
            return t;
        }

        public static Text TxtAt(string name, Transform parent, string value, Font font, int size, Color color,
                                 float x, float y, float w, float h, TextAnchor align = TextAnchor.UpperLeft)
            => At(Txt(name, parent, value, font, size, color, align), x, y, w, h);

        // ---- controls ----
        /// <summary>A flat button: fill + border, optional hard shadow, centred pixel-font label.</summary>
        public static Button Btn(string name, Transform parent, string label, Color fill, Color text, Color border,
                                 float x, float y, float w, float h, int size = 22, float shadow = 4f, Font font = null)
        {
            var img = At(Img(name, parent, fill, null, true), x, y, w, h);
            // The shadow is an effect on the button itself, so it hides together with the button.
            if (shadow > 0f) HardShadow(img, shadow, fill.a > 0f ? 1f : 0f);
            if (border.a > 0f) Border(img.transform, border);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            img.gameObject.AddComponent<ButtonFx>();
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            b.colors = colors;
            if (!string.IsNullOrEmpty(label))
            {
                var t = Txt("Label", img.transform, label, font ?? Pixel, size, text, TextAnchor.MiddleCenter);
                Stretch(t.rectTransform);
            }
            return b;
        }

        public static InputField NewInput(string name, Transform parent, string placeholder, int size, float x, float y, float w, float h, int limit)
        {
            var bg = At(Img(name, parent, Hex("#0B110D"), null, true), x, y, w, h);
            Border(bg.transform, Hex("#6E8A76"), "ui_frame2");
            var field = bg.gameObject.AddComponent<InputField>();
            var text = Txt("Text", bg.transform, "", Body, size, Cream, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(14, 2); text.rectTransform.offsetMax = new Vector2(-14, -2);
            text.supportRichText = false;
            var ph = Txt("Placeholder", bg.transform, placeholder, Body, size, Alpha(Muted, 0.8f), TextAnchor.MiddleLeft);
            ph.fontStyle = FontStyle.Italic;
            Stretch(ph.rectTransform); ph.rectTransform.offsetMin = new Vector2(14, 2); ph.rectTransform.offsetMax = new Vector2(-14, -2);
            field.textComponent = text; field.placeholder = ph; field.targetGraphic = bg;
            field.characterLimit = limit;
            field.lineType = InputField.LineType.SingleLine;
            field.caretColor = Gold; field.customCaretColor = true;
            field.selectionColor = Alpha(Gold, 0.35f);
            return field;
        }

        /// <summary>A checkbox row: 24 px box with a check mark, then the label (44 px tall touch target).</summary>
        public static Toggle Check(string name, Transform parent, string label, float x, float y, float w)
        {
            var row = At(Img(name, parent, Clear, null, true), x, y, w, 44);
            var box = At(Img("Box", row.transform, Hex("#0B110D")), 0, 10, 24, 24);
            Border(box.transform, Hex("#6E8A76"), "ui_frame2");
            var fill = At(Img("On", box.transform, Gold), 0, 0, 24, 24);
            var mark = Icon("Check", fill.transform, "icon_check", Ink, 2, 2, 20);
            TxtAt("Label", row.transform, label, Body, 16, Cream, 36, 0, w - 36, 44, TextAnchor.MiddleLeft);
            var t = row.gameObject.AddComponent<Toggle>();
            t.targetGraphic = box; t.graphic = fill; t.isOn = true;
            return t;
        }

        public static Slider Range(string name, Transform parent, float x, float y, float w)
        {
            var root = At(NewRect(name, parent).gameObject.AddComponent<Image>(), x, y, w, 28);
            root.color = Clear; root.raycastTarget = true;
            var track = Img("Track", root.transform, Line); Stretch(track.rectTransform); track.rectTransform.offsetMin = new Vector2(0, 10); track.rectTransform.offsetMax = new Vector2(0, -10);
            var fillArea = NewRect("FillArea", root.transform); Stretch(fillArea); fillArea.offsetMin = new Vector2(0, 10); fillArea.offsetMax = new Vector2(-14, -10);
            var fill = Img("Fill", fillArea, Gold); Stretch(fill.rectTransform); fill.rectTransform.offsetMax = new Vector2(14, 0);
            var handleArea = NewRect("HandleArea", root.transform); Stretch(handleArea); handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
            var handle = Img("Handle", handleArea, Cream, null, true);
            handle.rectTransform.sizeDelta = new Vector2(20, 0);
            Border(handle.transform, Ink, "ui_frame2");
            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = 0; s.maxValue = 1; s.value = 0.7f;
            return s;
        }

        public static Image Bar(string name, Transform parent, Color back, Color fill, float x, float y, float w, float h, out Image fillImg, string border = "ui_frame2")
        {
            var b = At(Img(name, parent, back), x, y, w, h);
            if (!string.IsNullOrEmpty(border)) Border(b.transform, Color.black, border);
            fillImg = Img("Fill", b.transform, fill);
            Stretch(fillImg.rectTransform, 2);
            fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal; fillImg.fillOrigin = 0; fillImg.fillAmount = 0.5f;
            fillImg.transform.SetSiblingIndex(0);
            return b;
        }

        // ---- serialized wiring ----
        public static void Ref(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Thermo-Tactics] {target.GetType().Name}.{field} not found"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void RefInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Thermo-Tactics] {target.GetType().Name}.{field} not found"); return; }
            p.intValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void RefBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Thermo-Tactics] {target.GetType().Name}.{field} not found"); return; }
            p.boolValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void RefArray<T>(Object target, string field, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Thermo-Tactics] {target.GetType().Name}.{field} not found"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A Screen Space Overlay canvas scaled from the 1280 × 720 design, matching height.</summary>
        /// <summary>Fade (and slide) a screen in each time it is shown.</summary>
        public static void AddPanelFx(GameObject root, RectTransform slide = null)
        {
            if (root.GetComponent<CanvasGroup>() == null) root.AddComponent<CanvasGroup>();
            var fx = root.GetComponent<PanelFx>();
            if (fx == null) fx = root.AddComponent<PanelFx>();
            if (slide != null) Ref(fx, "_slide", slide);
        }

        public static Canvas NewCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            canvas.pixelPerfect = false;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<TextScaler>();
            return canvas;
        }
    }
}
