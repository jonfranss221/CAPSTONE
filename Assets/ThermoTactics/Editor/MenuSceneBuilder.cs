using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static ThermoTactics.EditorTools.UiKit;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Builds MainMenu.unity from the approved design boards: Main Menu (player profile, Continue, Start,
    /// Settings, Quit), Settings, Quit, Stage Select and the Baguio level path. Coordinates are the boards' pixels.
    /// </summary>
    public static class MenuSceneBuilder
    {
        public const string ScenePath = Root + "/Scenes/MainMenu.unity";

        // Level path nodes (design board "Baguio level select": centre x, y in the 1280 × 720 frame).
        private static readonly Vector2[] NodePos =
        {
            new Vector2(110, 620), new Vector2(270, 592), new Vector2(430, 624), new Vector2(590, 590), new Vector2(730, 498),
            new Vector2(570, 430), new Vector2(410, 412), new Vector2(250, 432), new Vector2(150, 320), new Vector2(380, 196),
        };

        [MenuItem("Thermo-Tactics/Steps/3. Build Main Menu")]
        public static void BuildMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<StageData>(ThermoTacticsBuilder.StagePath) == null) { Debug.LogError("[Thermo-Tactics] Run Build Everything first (no StageData)."); return; }
            Build();
        }

        /// <summary>Builds the Main Menu with every built stage (ThermoTacticsBuilder.StagePaths, in order).</summary>
        public static void Build()
        {
            ThermoTacticsBuilder.EnsureFolder(Root + "/Scenes");
            LoadFonts();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Load by path: the instances handed over while assets were being created can go stale.
            var stages = new System.Collections.Generic.List<StageData>();
            foreach (string path in ThermoTacticsBuilder.StagePaths)
            {
                var s = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (s != null) stages.Add(s);
            }
            StageData stage = stages[0];

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Night;
            cam.transform.position = new Vector3(0, 0, -10);

            var sys = new GameObject("MenuSystems");
            var db = sys.AddComponent<ResultsDatabase>();

            Canvas canvas = NewCanvas("UI", 10);
            Transform c = canvas.transform;
            var menu = canvas.gameObject.AddComponent<MainMenuController>();
            RefArray(menu, "_stages", stages.ToArray());
            Ref(menu, "_database", db);

            BuildTitle(c, menu);
            var stageSelect = BuildStageSelect(c, stages);
            var levelSelect = BuildLevelSelect(c);
            var settings = BuildSettings(c, menu);
            var results = BuildResults(c);
            var facts = EducationUiBuilder.BuildFacts(c);
            var quiz = EducationUiBuilder.BuildQuiz(c);
            var store = StoreUiBuilder.BuildStore(c);
            var squad = StoreUiBuilder.BuildSquad(c);
            BuildQuit(c, menu);
            BuildConfirm(c, menu);
            BuildCredits(c, menu);
            Ref(menu, "_settings", settings);
            Ref(menu, "_stageSelect", stageSelect);
            Ref(menu, "_levelSelect", levelSelect);
            Ref(menu, "_results", results);
            Ref(menu, "_facts", facts);
            Ref(menu, "_quiz", quiz);
            Ref(menu, "_store", store);
            Ref(menu, "_squad", squad);

            // Save every pop-up hidden, so the scene opens on the title screen in the editor too
            // (each panel hides itself at run time anyway).
            foreach (Component panel in new Component[] { results, facts, quiz, store, squad }) if (panel != null) panel.gameObject.SetActive(false);
            foreach (string dialog in new[] { "QuitDialog", "ConfirmDialog", "CreditsDialog", "SettingsDialog" })
            {
                Transform d = c.Find(dialog);
                if (d != null) d.gameObject.SetActive(false);
            }

            ThermoTacticsBuilder.NewEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // =====================================================================
        // Main Menu (design board "Main Menu")
        // =====================================================================
        private static void BuildTitle(Transform c, MainMenuController menu)
        {
            var screen = NewRect("TitleScreen", c); Stretch(screen);
            Ref(menu, "_titleScreen", screen.gameObject);

            var bg = Img("Background", screen, Color.white, Ui("menu_background"));
            Stretch(bg.rectTransform);
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 1344f / 576f;
            bg.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            var dim = Img("Dim", screen, new Color(0.047f, 0.07f, 0.055f, 0.55f)); Stretch(dim.rectTransform);

            // ---- living background: rolling fog and drifting pollen (MenuAmbience) ----
            var fogTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Environment/fog_drift_tileable.png");
            var fogs = new RawImage[2];
            for (int i = 0; i < 2; i++)
            {
                var fogImg = NewRect($"Fog_{i + 1}", screen).gameObject.AddComponent<RawImage>();
                Stretch(fogImg.rectTransform);
                fogImg.texture = fogTex; fogImg.raycastTarget = false;
                fogImg.uvRect = new Rect(i * 0.37f, 0f, i == 0 ? 1f : 0.7f, 1f);
                fogImg.color = new Color(0.86f, 0.92f, 0.9f, 0.25f);
                fogs[i] = fogImg;
            }
            var motes = NewRect("Motes", screen); Stretch(motes);
            var mote = Img("Mote", motes, Hex("#E9E4B8"));
            var ambience = screen.gameObject.AddComponent<MenuAmbience>();
            Ref(ambience, "_background", bg.rectTransform);
            RefArray(ambience, "_fog", fogs);
            Ref(ambience, "_motesRoot", motes);
            Ref(ambience, "_moteTemplate", mote);
            WeaveEdge(screen, true); WeaveEdge(screen, false);

            var f = Frame("Frame", screen);
            TxtAt("Eyebrow", f, "A CLIMATE STRATEGY GAME", BodyBold, 15, Gold, 64, 56, 620, 22);
            var title = TxtAt("Title", f, "THERMO-\nTACTICS", Pixel, 84, Cream, 64, 80, 620, 170);
            title.lineSpacing = 0.9f;
            HardShadow(title, 5f, 0.6f);
            Ref(ambience, "_title", title.rectTransform);
            TxtAt("Tagline", f, "Defend the Philippines. Keep the planet below <b>1.5 °C</b>.", Body, 19, Hex("#DCD2BD"), 64, 254, 620, 28);

            // ---- navigation ----
            // Continue (enabled): gold block with the saved level, wave and turn
            var cont = Btn("ContinueButton", f, "", Gold, Ink, Ink, 64, 290, 400, 92, 26, 4f);
            Ref(menu, "_continueButton", cont);
            var on = NewRect("On", cont.transform); Stretch(on);
            Icon("Icon", on, "icon_continue", Ink, 18, 31, 30);
            TxtAt("Label", on, "CONTINUE", Pixel, 26, Ink, 64, 12, 320, 28, TextAnchor.MiddleLeft);
            Ref(menu, "_continueDetail", TxtAt("Detail", on, "", BodyBold, 14, Ink, 64, 42, 326, 20, TextAnchor.MiddleLeft));
            Ref(menu, "_continueSaved", TxtAt("Saved", on, "", Body, 13, Ink, 64, 62, 326, 18, TextAnchor.MiddleLeft));
            Ref(menu, "_continueOn", on.gameObject);
            // Continue (disabled): dashed outline
            var off = Img("Off", cont.transform, Hex("#18201B")); Stretch(off.rectTransform);
            Border(off.transform, Hex("#5E584C"), "ui_frame_dashed");
            Icon("Icon", off.transform, "icon_continue", Muted, 18, 31, 30);
            TxtAt("Label", off.transform, "CONTINUE", Pixel, 26, Muted, 64, 20, 320, 28, TextAnchor.MiddleLeft);
            TxtAt("Detail", off.transform, "No saved session for this player yet", Body, 14, Muted, 64, 50, 326, 20, TextAnchor.MiddleLeft);
            Ref(menu, "_continueOff", off.gameObject);

            var start = Btn("StartButton", f, "", Red, Hex("#FFF6E6"), Ink, 64, 394, 400, 76, 26, 4f);
            Icon("Icon", start.transform, "icon_play", Hex("#FFF6E6"), 18, 23, 30);
            TxtAt("Label", start.transform, "START", Pixel, 26, Hex("#FFF6E6"), 64, 12, 320, 28, TextAnchor.MiddleLeft);
            TxtAt("Detail", start.transform, "Choose a stage and level", Body, 14, Hex("#FFF6E6"), 64, 42, 320, 20, TextAnchor.MiddleLeft);
            Ref(menu, "_startButton", start);

            // STORE (under Start): green once Baguio Level 1 is cleared, grey and locked before that.
            var storeGreen = Hex("#2F7A43"); var storeText = Hex("#F2FFF3");
            var store = Btn("StoreButton", f, "", storeGreen, storeText, Ink, 64, 482, 400, 76, 26, 4f);
            var sOn = NewRect("On", store.transform); Stretch(sOn);
            Icon("Icon", sOn, "icon_credit", storeText, 18, 23, 30);
            TxtAt("Label", sOn, "STORE", Pixel, 26, storeText, 64, 12, 320, 28, TextAnchor.MiddleLeft);
            TxtAt("Detail", sOn, "Spend Carbon Credits on new defenders", Body, 14, storeText, 64, 42, 320, 20, TextAnchor.MiddleLeft);
            Ref(menu, "_storeOn", sOn.gameObject);
            var sOff = Img("Off", store.transform, Hex("#2A2F2B")); Stretch(sOff.rectTransform);
            Border(sOff.transform, Hex("#4A4F4B"));
            Icon("Icon", sOff.transform, "icon_lock", Muted, 18, 23, 30);
            TxtAt("Label", sOff.transform, "STORE", Pixel, 26, Muted, 64, 12, 320, 28, TextAnchor.MiddleLeft);
            TxtAt("Detail", sOff.transform, "Locked · clear Baguio Level 1 to unlock", Body, 14, Muted, 64, 42, 330, 20, TextAnchor.MiddleLeft);
            Ref(menu, "_storeOff", sOff.gameObject);
            Ref(menu, "_storeButton", store);

            var set = Btn("SettingsButton", f, "", new Color(0.078f, 0.11f, 0.09f, 0.92f), Cream, Slate, 64, 570, 194, 64, 19, 0f);
            Icon("Icon", set.transform, "icon_settings", Cream, 16, 20, 24);
            TxtAt("Label", set.transform, "SETTINGS", Pixel, 19, Cream, 52, 12, 140, 22, TextAnchor.MiddleLeft);
            TxtAt("Detail", set.transform, "Sound, display", Body, 13, CreamDim, 52, 34, 140, 18, TextAnchor.MiddleLeft);
            Ref(menu, "_settingsButton", set);
            var quit = Btn("QuitButton", f, "", new Color(0.078f, 0.11f, 0.09f, 0.92f), Cream, Slate, 270, 570, 194, 64, 19, 0f);
            Icon("Icon", quit.transform, "icon_quit", Cream, 16, 20, 24);
            TxtAt("Label", quit.transform, "QUIT", Pixel, 19, Cream, 52, 12, 140, 22, TextAnchor.MiddleLeft);
            TxtAt("Detail", quit.transform, "Exit the game", Body, 13, CreamDim, 52, 34, 140, 18, TextAnchor.MiddleLeft);
            Ref(menu, "_quitButton", quit);

            // ---- Carbon Credits wallet (top right, above the profile; shown once a valid name is typed) ----
            // Carbon Credits count, top right above the profile (display only: the STORE button is under Start).
            var wallet = Box("Wallet", f, new Color(0.067f, 0.094f, 0.078f, 0.95f), Hex("#4E9A5A"), 1006, 58, 210, 50, 4f, "ui_frame2");
            Icon("Icon", wallet.transform, "icon_credit", Hex("#8FE09B"), 12, 12, 26);
            TxtAt("Label", wallet.transform, "CARBON CREDITS", BodyBold, 11, Hex("#8FE09B"), 48, 7, 156, 14, TextAnchor.MiddleLeft);
            Ref(menu, "_walletText", TxtAt("Amount", wallet.transform, "0 CC", BodyBold, 19, Hex("#C9F5CF"), 48, 21, 156, 24, TextAnchor.MiddleLeft));
            Ref(menu, "_walletRoot", wallet.gameObject);

            // ADMIN tag (top, left of the wallet): shown only while the admin code is unlocked in Settings.
            var admin = Box("AdminTag", f, Hex("#5A1A14"), Hex("#FF9A86"), 776, 66, 128, 34, 0f, "ui_frame2");
            TxtAt("Label", admin.transform, "ADMIN", Pixel, 18, Hex("#FFD9D3"), 0, 0, 128, 34, TextAnchor.MiddleCenter);
            Ref(menu, "_adminTag", admin.gameObject);

            // ---- player profile form ----
            var form = Box("PlayerProfile", f, new Color(0.067f, 0.094f, 0.078f, 0.95f), Moss, 776, 120, 440, 436, 6f);
            Icon("Icon", form.transform, "icon_user", Gold, 28, 30, 28);
            TxtAt("Title", form.transform, "PLAYER PROFILE", Pixel, 24, Cream, 68, 26, 340, 26, TextAnchor.MiddleLeft);
            TxtAt("Sub", form.transform, "Who is playing?", Body, 14, CreamDim, 68, 52, 340, 18, TextAnchor.MiddleLeft);
            TxtAt("NameLabel", form.transform, "Player name <color=#CFC6B2>(2–20 characters)</color>", BodyBold, 15, Cream, 28, 86, 384, 20, TextAnchor.MiddleLeft);
            Ref(menu, "_nameMessage", TxtAt("NameMessage", form.transform, "", BodyBold, 13, Hex("#FF9A86"), 28, 160, 384, 18, TextAnchor.MiddleLeft));
            Ref(menu, "_nameInput", NewInput("NameInput", form.transform, "Type your name", 19, 28, 110, 384, 48, ResultsDatabase.MaxNameLength));
            // The player code is given automatically (A-001, A-002, ...) the first time a name presses Start; it cannot be typed.
            TxtAt("CodeLabel", form.transform, "Player code <color=#CFC6B2>(given automatically)</color>", BodyBold, 15, Cream, 28, 184, 384, 20, TextAnchor.MiddleLeft);
            var codeBox = At(Img("CodeBox", form.transform, new Color(0.953f, 0.918f, 0.843f, 0.05f)), 28, 208, 384, 48);
            Border(codeBox.transform, Slate, "ui_frame2");
            Ref(menu, "_codeText", TxtAt("Code", codeBox.transform, "", BodyBold, 17, Cream, 14, 2, 356, 44, TextAnchor.MiddleLeft));

            var infoSaved = At(Img("InfoSaved", form.transform, new Color(0.886f, 0.682f, 0.173f, 0.12f)), 28, 272, 384, 78);
            Border(infoSaved.transform, Hex("#8C6D1E"), "ui_frame2");
            Icon("Icon", infoSaved.transform, "icon_save", Gold, 14, 14, 22);
            Ref(menu, "_infoSavedText", TxtAt("Text", infoSaved.transform, "", Body, 15, Cream, 46, 10, 326, 60));
            Ref(menu, "_infoSaved", infoSaved.gameObject);
            var infoNew = At(Img("InfoNew", form.transform, new Color(0.953f, 0.918f, 0.843f, 0.05f)), 28, 272, 384, 78);
            Border(infoNew.transform, Slate, "ui_frame2");
            Icon("Icon", infoNew.transform, "icon_info", CreamDim, 14, 14, 22);
            Ref(menu, "_infoNewText", TxtAt("Text", infoNew.transform, "", Body, 15, Cream, 46, 10, 326, 60));
            Ref(menu, "_infoNew", infoNew.gameObject);
            TxtAt("Privacy", form.transform, "Your name is stored only on this device with your results and progress. Not you? Type your own name to switch player.",
                Body, 13, CreamDim, 28, 364, 384, 54);

            // ---- Progress of the current stage on the title (levels cleared and stars) ----
            var prog = Box("StageProgress", f, new Color(0.067f, 0.094f, 0.078f, 0.92f), Moss, 776, 572, 440, 86, 0f, "ui_frame2");
            Ref(menu, "_progressTitle", TxtAt("Stage", prog.transform, "STAGE 1 · BAGUIO FOREST", BodyBold, 14, GoldLight, 16, 10, 300, 20, TextAnchor.MiddleLeft));
            Icon("StarIcon", prog.transform, "icon_star", Gold, 340, 9, 20);
            Ref(menu, "_progressStars", TxtAt("Stars", prog.transform, "0 / 30", BodyBold, 15, Cream, 364, 9, 64, 22, TextAnchor.MiddleLeft));
            var segs = new Image[10];
            float segW = (408f - 9 * 4f) / 10f;
            for (int i = 0; i < 10; i++)
            {
                segs[i] = At(Img($"Level_{i + 1}", prog.transform, Line), 16 + i * (segW + 4), 38, segW, 14);
            }
            RefArray(menu, "_progressSegments", segs);
            Ref(menu, "_progressInfo", TxtAt("Info", prog.transform, "", Body, 13, CreamDim, 16, 58, 410, 20, TextAnchor.MiddleLeft));
            Ref(menu, "_progressRoot", prog.gameObject);

            Ref(menu, "_versionText", TxtAt("Version", f, "v0.2 · Stage 1 prototype", Body, 13, CreamDim, 64, 664, 400, 18));
            AddPanelFx(screen.gameObject, f);
        }

        private static void WeaveEdge(Transform parent, bool top)
        {
            var w = Img(top ? "WeaveTop" : "WeaveBottom", parent, Color.white, Ui("weave_tile"));
            w.type = Image.Type.Tiled;
            var rt = w.rectTransform;
            rt.anchorMin = new Vector2(0, top ? 1 : 0); rt.anchorMax = new Vector2(1, top ? 1 : 0); rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.sizeDelta = new Vector2(0, 20); rt.anchoredPosition = Vector2.zero;
        }

        private static RectTransform Overlay(string name, Transform c, out RectTransform frame)
        {
            var o = Img(name, c, new Color(0.024f, 0.04f, 0.03f, 0.72f), null, true); Stretch(o.rectTransform);
            frame = Frame("Frame", o.transform);
            AddPanelFx(o.gameObject, frame);
            return o.rectTransform;
        }

        // =====================================================================
        // Settings (design board "Settings")
        // =====================================================================
        private static SettingsPanel BuildSettings(Transform c, MainMenuController menu)
        {
            var root = Overlay("SettingsDialog", c, out var f);
            var panel = root.gameObject.AddComponent<SettingsPanel>();
            Ref(panel, "_menu", menu);
            Ref(panel, "_root", root.gameObject);

            var d = Box("Dialog", f, Panel, Moss, 230, 36, 820, 648, 8f);
            Weave(d.transform, 0, 0, 820);
            TxtAt("Title", d.transform, "SETTINGS", Pixel, 34, Cream, 28, 38, 400, 38, TextAnchor.MiddleLeft);
            TxtAt("Sub", d.transform, "Saved on this phone", Body, 15, CreamDim, 28, 76, 400, 20);
            var close = Btn("CloseButton", d.transform, "", Clear, Cream, Slate, 744, 36, 48, 48, 18, 0f);
            Icon("Icon", close.transform, "icon_close", Cream, 13, 13, 22);
            Ref(panel, "_closeButton", close);

            const float L = 28, R = 428, W = 364;
            Legend(d.transform, "AUDIO", L, 116);
            TxtAt("MusicLabel", d.transform, "Music", BodyBold, 16, Cream, L, 150, 200, 22, TextAnchor.MiddleLeft);
            Ref(panel, "_musicValue", TxtAt("MusicValue", d.transform, "70%", Body, 16, CreamDim, L + W - 80, 150, 80, 22, TextAnchor.MiddleRight));
            Ref(panel, "_music", Range("MusicSlider", d.transform, L, 176, W));
            TxtAt("SfxLabel", d.transform, "Sound effects", BodyBold, 16, Cream, L, 216, 200, 22, TextAnchor.MiddleLeft);
            Ref(panel, "_sfxValue", TxtAt("SfxValue", d.transform, "85%", Body, 16, CreamDim, L + W - 80, 216, 80, 22, TextAnchor.MiddleRight));
            Ref(panel, "_sfx", Range("SfxSlider", d.transform, L, 242, W));
            Legend(d.transform, "DISPLAY", L, 290);
            Ref(panel, "_largeText", Check("LargeText", d.transform, "Large text", L, 320, W));
            Ref(panel, "_highContrast", Check("HighContrast", d.transform, "High-contrast lane highlights", L, 366, W));
            Ref(panel, "_laneCamera", Check("LaneCamera", d.transform, "Zoom in on each lane during battle", L, 412, W));

            Legend(d.transform, "GAMEPLAY", R, 116);
            TxtAt("SpeedLabel", d.transform, "Battle animation speed", BodyBold, 16, Cream, R, 150, W, 22, TextAnchor.MiddleLeft);
            var seg = At(Img("Speed", d.transform, Clear), R, 176, W, 46);
            Border(seg.transform, Slate, "ui_frame2");
            var speeds = new Button[3];
            string[] labels = { "1x", "1.5x", "2x" };
            float sw = (W - 4) / 3f;
            for (int i = 0; i < 3; i++)
                speeds[i] = Btn("Speed_" + labels[i], seg.transform, labels[i], Clear, Cream, Clear, 2 + i * sw, 2, sw, 42, 16, 0f, BodyBold);
            RefArray(panel, "_speedButtons", speeds);
            Ref(panel, "_popups", Check("Popups", d.transform, "Climate fact pop-ups", R, 228, W));
            Ref(panel, "_numbers", Check("Numbers", d.transform, "Show damage and °C numbers", R, 272, W));
            Ref(panel, "_bossWarning", Check("BossWarning", d.transform, "Warn me before the boss uses its AoE", R, 316, W));
            Legend(d.transform, "DATA", R, 370);
            float bw = (W - 16) / 3f;
            var reset = Btn("ResetButton", d.transform, "Reset my progress", Clear, Hex("#FFD9D3"), RedSoft, R, 402, bw + 30, 48, 14, 0f, BodyBold);
            Ref(panel, "_resetButton", reset);
            Ref(panel, "_creditsButton", Btn("CreditsButton", d.transform, "Credits", Clear, Cream, Slate, R + bw + 38, 402, bw - 15, 48, 15, 0f, BodyBold));
            Ref(panel, "_leaderboardButton", Btn("LeaderboardButton", d.transform, "Leaderboard", Clear, Cream, Slate, R + 2 * bw + 31, 402, bw - 15, 48, 15, 0f, BodyBold));

            // Admin tools: greyed out (CanvasGroup alpha) until the admin code is entered on the left.
            TxtAt("AdminToolsLabel", d.transform, "Admin only", BodyBold, 13, CreamDim, R, 458, W, 18, TextAnchor.MiddleLeft);
            Button AdminBtn(string name, string label, float x, float w)
            {
                var b = Btn(name, d.transform, label, Clear, Cream, Slate, x, 480, w, 46, 14, 0f, BodyBold);
                b.gameObject.AddComponent<CanvasGroup>();
                return b;
            }
            float aw = (W - 16) / 3f;
            Ref(panel, "_resultsButton", AdminBtn("ResultsButton", "Full results", R, aw));
            Ref(panel, "_exportButton", AdminBtn("ExportButton", "Export CSV", R + aw + 8, aw));
            var letter = AdminBtn("PhoneLetterButton", "Phone: A", R + 2 * aw + 16, aw);
            Ref(panel, "_phoneLetterButton", letter);
            Ref(panel, "_phoneLetterLabel", letter.GetComponentInChildren<Text>());

            Legend(d.transform, "ADMIN", L, 466);
            TxtAt("AdminLabel", d.transform, "Admin code:", BodyBold, 16, Cream, L, 498, 100, 44, TextAnchor.MiddleLeft);
            var code = NewInput("AdminInput", d.transform, "Enter code", 16, L + 104, 498, 150, 44, 16);
            code.contentType = InputField.ContentType.Password;
            code.asteriskChar = '*';
            Ref(panel, "_adminInput", code);
            var unlock = Btn("AdminButton", d.transform, "UNLOCK", Clear, Gold, Gold, L + 262, 498, 102, 44, 16, 0f);
            Ref(panel, "_adminButton", unlock);
            Ref(panel, "_adminButtonLabel", unlock.GetComponentInChildren<Text>());
            Ref(panel, "_adminStatus", TxtAt("AdminStatus", d.transform, "", Body, 14, CreamDim, L, 546, W, 22, TextAnchor.MiddleLeft));

            // Under both columns, left of the Done button: room for the export path.
            Ref(panel, "_dataNote", TxtAt("DataNote", d.transform, "", Body, 13, CreamDim, L, 576, 540, 60));

            Ref(panel, "_doneButton", Btn("DoneButton", d.transform, "DONE", Gold, Ink, Ink, 592, 576, 200, 52, 22));
            root.gameObject.SetActive(false);
            return panel;
        }

        private static void Legend(Transform parent, string text, float x, float y)
            => TxtAt("Legend_" + text, parent, text, Pixel, 20, Gold, x, y, 300, 26, TextAnchor.MiddleLeft);

        // =====================================================================
        // Quit, confirm and credits dialogs (design board "Quit confirmation")
        // =====================================================================
        private static void BuildQuit(Transform c, MainMenuController menu)
        {
            var root = Overlay("QuitDialog", c, out var f);
            var d = Box("Dialog", f, Panel, Red, 380, 200, 520, 300, 8f);
            Weave(d.transform, 0, 0, 520);
            TxtAt("Title", d.transform, "LEAVE THE GAME?", Pixel, 32, Cream, 28, 42, 464, 38, TextAnchor.MiddleLeft);
            TxtAt("Body", d.transform, "Thermo-Tactics will close.", Body, 17, Hex("#DCD2BD"), 28, 86, 464, 24);
            var info = At(Img("Info", d.transform, new Color(0.184f, 0.365f, 0.27f, 0.25f)), 28, 122, 464, 76);
            Border(info.transform, Moss, "ui_frame2");
            Icon("Icon", info.transform, "icon_check", Mint, 14, 14, 22);
            Ref(menu, "_quitInfo", TxtAt("Text", info.transform, "", Body, 15, Cream, 46, 10, 404, 60));
            Ref(menu, "_quitStay", Btn("StayButton", d.transform, "STAY", Clear, Cream, Slate, 28, 220, 226, 54, 21, 0f));
            Ref(menu, "_quitConfirm", Btn("QuitButton", d.transform, "QUIT", Red, Hex("#FFF6E6"), Ink, 266, 220, 226, 54, 21));
            Ref(menu, "_quitDialog", root.gameObject);
        }

        private static void BuildConfirm(Transform c, MainMenuController menu)
        {
            var root = Overlay("ConfirmDialog", c, out var f);
            var d = Box("Dialog", f, Panel, Red, 380, 210, 520, 280, 8f);
            Weave(d.transform, 0, 0, 520);
            Ref(menu, "_confirmTitle", TxtAt("Title", d.transform, "REPLACE SAVED GAME?", Pixel, 30, Cream, 28, 42, 464, 36, TextAnchor.MiddleLeft));
            Ref(menu, "_confirmBody", TxtAt("Body", d.transform, "", Body, 16, Hex("#DCD2BD"), 28, 90, 464, 90));
            Ref(menu, "_confirmCancel", Btn("CancelButton", d.transform, "CANCEL", Clear, Cream, Slate, 28, 200, 226, 54, 21, 0f));
            var ok = Btn("OkButton", d.transform, "REPLACE", Red, Hex("#FFF6E6"), Ink, 266, 200, 226, 54, 21);
            Ref(menu, "_confirmOk", ok);
            Ref(menu, "_confirmOkLabel", ok.GetComponentInChildren<Text>());
            Ref(menu, "_confirmDialog", root.gameObject);
        }

        private static void BuildCredits(Transform c, MainMenuController menu)
        {
            var root = Overlay("CreditsDialog", c, out var f);
            var d = Box("Dialog", f, Panel, Moss, 330, 140, 620, 420, 8f);
            Weave(d.transform, 0, 0, 620);
            TxtAt("Title", d.transform, "CREDITS", Pixel, 32, Cream, 28, 42, 560, 38, TextAnchor.MiddleLeft);
            TxtAt("Body", d.transform,
                "<b>Thermo-Tactics</b> — a capstone project: a turn-based strategy game about keeping the planet below 1.5 °C, set in the Philippines.\n\n" +
                "<b>Team</b>\nEdz Steinar Bederico · Jon Francis Gellido · Rodge Emanuel Ramos · Jherwin Reyes\n\n" +
                "<b>Fonts</b>\nPixelify Sans and Atkinson Hyperlegible, SIL Open Font License 1.1\n\n" +
                "Made with Unity 6. Results and progress are stored only on this device (SQLite).",
                Body, 15, Cream, 28, 90, 564, 240);
            Ref(menu, "_creditsClose", Btn("CloseButton", d.transform, "CLOSE", Gold, Ink, Ink, 392, 346, 200, 50, 20));
            Ref(menu, "_creditsDialog", root.gameObject);
        }

        // =====================================================================
        // Stage Select (design board "Stage Select")
        // =====================================================================
        /// <summary>Cards for stages that are not built yet (Stages 3–5).</summary>
        private static readonly (string name, string desc, string diff, string thumb)[] FutureStages =
        {
            ("SIERRA MADRE COAST", "Typhoon coast, mangroves and coral reefs", "Easy–Medium", "stage2_thumb"),
            ("METRO MANILA URBAN", "Jeepneys, EDSA traffic, flooded esteros", "Medium", "stage3_thumb"),
            ("BATANGAS INDUSTRIAL ZONE", "Taal Volcano, refineries, coal plants", "Hard", "stage4_thumb"),
            ("BORACAY & PALAWAN FINAL STAND", "Coral reefs, limestone cliffs, island beaches", "Final", "stage5_thumb"),
        };

        private static StageSelectPanel BuildStageSelect(Transform c, System.Collections.Generic.List<StageData> stages)
        {
            var root = Img("StageSelect", c, Hex("#101814"), null, true); Stretch(root.rectTransform);
            var panel = root.gameObject.AddComponent<StageSelectPanel>();
            Ref(panel, "_root", root.gameObject);
            WeaveEdge(root.transform, true); WeaveEdge(root.transform, false);
            var f = Frame("Frame", root.transform);
            AddPanelFx(root.gameObject, f);

            var back = Btn("BackButton", f, "", Clear, Cream, Slate, 48, 40, 52, 52, 18, 0f);
            Icon("Icon", back.transform, "icon_back", Cream, 14, 14, 24);
            Ref(panel, "_backButton", back);
            TxtAt("Title", f, "CHOOSE A STAGE", Pixel, 38, Cream, 120, 38, 760, 40, TextAnchor.MiddleLeft);
            TxtAt("Sub", f, "Clear every level of a stage to unlock the next one. Each stage ends with its own boss.", Body, 16, CreamDim, 120, 78, 760, 22);
            var chip = At(Img("Player", f, new Color(0.184f, 0.365f, 0.27f, 0.2f)), 1012, 42, 220, 52);
            Border(chip.transform, Moss, "ui_frame2");
            Icon("Icon", chip.transform, "icon_user", Gold, 14, 15, 22);
            Ref(panel, "_playerName", TxtAt("Name", chip.transform, "", BodyBold, 15, Cream, 46, 7, 168, 20, TextAnchor.MiddleLeft));
            Ref(panel, "_playerProgress", TxtAt("Progress", chip.transform, "", Body, 14, CreamDim, 46, 26, 168, 20, TextAnchor.MiddleLeft));

            for (float x = 140; x < 1140; x += 20) At(Img("Dash", f, Slate), x, 384, 12, 4);   // the dashed trail between cards

            const float W = 220, Gap = 21, Top = 140, H = 500;
            var so = new SerializedObject(panel);
            var cards = so.FindProperty("_cards");
            cards.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                int id = i + 1;
                StageData stage = stages.Find(s => s.stageId == id);
                float x = 48 + i * (W + Gap);
                var el = cards.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("stageId").intValue = id;
                At(Img($"Stage{id}_Shadow", f, Color.black), x + 6, Top + 6, W, H);

                if (stage != null)
                {
                    // ---- a built stage: open (PLAY) or locked until the stage before it is cleared ----
                    var card = At(Img($"Stage{id}", f, Hex("#142019"), null, true), x, Top, W, H);
                    var btn = card.gameObject.AddComponent<Button>(); btn.targetGraphic = card;
                    card.gameObject.AddComponent<ButtonFx>();
                    At(Img("Thumb", card.transform, Color.white, stage.thumbnail), 0, 0, W, 190);
                    At(Img("ThumbLine", card.transform, Gold), 0, 190, W, 3);
                    var tag = At(Img("Tag", card.transform, Gold), 10, 10, 84, 26);
                    var tagT = Txt("Text", tag.transform, $"STAGE {id}", BodyBold, 15, Ink, TextAnchor.MiddleCenter); Stretch(tagT.rectTransform);
                    // Long names ("SIERRA MADRE COAST") use a smaller size so they stay on one line.
                    TxtAt("Name", card.transform, stage.stageName.ToUpperInvariant(), Pixel, stage.stageName.Length > 14 ? 17 : 24, Cream, 16, 208, W - 32, 28, TextAnchor.MiddleLeft);
                    TxtAt("Region", card.transform, stage.region, Body, 14, CreamDim, 16, 240, W - 32, 40);
                    TxtAt("Threat", card.transform, "<color=#CFC6B2>Threat:</color> " + stage.threats, Body, 14, Cream, 16, 284, W - 32, 40);
                    TxtAt("Difficulty", card.transform, "<color=#CFC6B2>Difficulty:</color> " + stage.difficulty, Body, 14, Cream, 16, 324, W - 32, 20);
                    Icon("StarIcon", card.transform, "icon_star", Gold, 16, 354, 18);
                    TxtAt("StarsLabel", card.transform, "Stars", BodyBold, 14, Cream, 40, 354, 80, 20, TextAnchor.MiddleLeft);
                    var stars = TxtAt("StarsCount", card.transform, "0 / 30", BodyBold, 14, GoldLight, W - 116, 354, 100, 20, TextAnchor.MiddleRight);
                    TxtAt("LevelsLabel", card.transform, "Levels", BodyBold, 14, Cream, 16, 384, 100, 20, TextAnchor.MiddleLeft);
                    var count = TxtAt("LevelsCount", card.transform, "0 / 10", BodyBold, 14, Cream, W - 116, 384, 100, 20, TextAnchor.MiddleRight);
                    var bar = At(Img("Progress", card.transform, Hex("#2A3A30")), 16, 410, W - 32, 10);
                    var fill = Img("Fill", bar.transform, Gold); Stretch(fill.rectTransform);
                    fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0f;

                    var open = NewRect("Open", card.transform); Stretch(open);
                    var play = At(Img("Play", open, Red), 16, 430, W - 32, 46);
                    Border(play.transform, Ink);
                    var pt = Txt("Text", play.transform, "PLAY", Pixel, 20, Hex("#FFF6E6"), TextAnchor.MiddleCenter); Stretch(pt.rectTransform);

                    var locked = NewRect("Locked", card.transform); Stretch(locked);
                    At(Img("ThumbDim", locked, new Color(0.04f, 0.055f, 0.047f, 0.55f)), 0, 0, W, 190);
                    Icon("Lock", locked, "icon_lock", Hex("#D8CFBC"), W / 2 - 22, 73, 44);
                    var box = At(Img("LockBox", locked, Hex("#131A18")), 16, 430, W - 32, 58);
                    Border(box.transform, Slate, "ui_frame_dashed");
                    var lockText = TxtAt("Text", box.transform, "Locked.", Body, 13, Hex("#B8B0A0"), 10, 4, W - 52, 50, TextAnchor.MiddleLeft);
                    var border = Border(card.transform, Gold);

                    el.FindPropertyRelative("button").objectReferenceValue = btn;
                    el.FindPropertyRelative("count").objectReferenceValue = count;
                    el.FindPropertyRelative("fill").objectReferenceValue = fill;
                    el.FindPropertyRelative("stars").objectReferenceValue = stars;
                    el.FindPropertyRelative("openGroup").objectReferenceValue = open.gameObject;
                    el.FindPropertyRelative("lockedGroup").objectReferenceValue = locked.gameObject;
                    el.FindPropertyRelative("lockedText").objectReferenceValue = lockText;
                    el.FindPropertyRelative("border").objectReferenceValue = border;
                    el.FindPropertyRelative("tag").objectReferenceValue = tag;
                    el.FindPropertyRelative("tagText").objectReferenceValue = tagT;
                }
                else
                {
                    // ---- a stage that is not built yet: always locked ----
                    var info = FutureStages[i - 1];
                    var card = At(Img($"Stage{id}", f, Hex("#131A18")), x, Top, W, H);
                    At(Img("Thumb", card.transform, Color.white, Ui(info.thumb)), 0, 0, W, 190);
                    At(Img("ThumbDim", card.transform, new Color(0.04f, 0.055f, 0.047f, 0.45f)), 0, 0, W, 190);
                    Icon("Lock", card.transform, "icon_lock", Hex("#D8CFBC"), W / 2 - 22, 73, 44);
                    At(Img("ThumbLine", card.transform, Hex("#33423B")), 0, 190, W, 3);
                    var t = At(Img("Tag", card.transform, Hex("#33423B")), 10, 10, 84, 26);
                    var tt = Txt("Text", t.transform, $"STAGE {id}", BodyBold, 15, Hex("#E7DFCC"), TextAnchor.MiddleCenter); Stretch(tt.rectTransform);
                    TxtAt("Name", card.transform, info.name, Pixel, 19, Hex("#E7DFCC"), 14, 204, W - 26, 66, TextAnchor.UpperLeft).lineSpacing = 0.92f;
                    TxtAt("Desc", card.transform, info.desc, Body, 14, Hex("#B8B0A0"), 16, 272, W - 32, 40);
                    TxtAt("Difficulty", card.transform, "Difficulty: " + info.diff, Body, 14, Hex("#B8B0A0"), 16, 314, W - 32, 20);
                    var box = At(Img("LockBox", card.transform, Clear), 16, 404, W - 32, 72);
                    Border(box.transform, Slate, "ui_frame_dashed");
                    el.FindPropertyRelative("lockedText").objectReferenceValue =
                        TxtAt("Text", box.transform, "Locked.", Body, 14, Hex("#B8B0A0"), 12, 8, W - 56, 58, TextAnchor.MiddleLeft);
                    Border(card.transform, Hex("#33423B"));
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return panel;
        }

        // =====================================================================
        // Baguio level path (design board "Baguio level select")
        // =====================================================================
        private static LevelSelectPanel BuildLevelSelect(Transform c)
        {
            var root = Img("LevelSelect", c, Forest, null, true); Stretch(root.rectTransform);
            var panel = root.gameObject.AddComponent<LevelSelectPanel>();
            Ref(panel, "_root", root.gameObject);
            WeaveEdge(root.transform, true); WeaveEdge(root.transform, false);
            var f = Frame("Frame", root.transform);
            AddPanelFx(root.gameObject, f);
            Ref(panel, "_pathArt", At(Img("PathArt", f, Color.white, Ui("levelpath_background")), 0, 20, 820, 680));

            // gold progress over the grey road: one segment between each pair of nodes
            var segs = new Image[NodePos.Length - 1];
            for (int i = 0; i < segs.Length; i++)
            {
                Vector2 a = NodePos[i], b = NodePos[i + 1];
                var seg = Img($"Road_{i + 1}_{i + 2}", f, Gold);
                var rt = seg.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0.5f);
                Vector2 mid = (a + b) / 2f;
                rt.anchoredPosition = new Vector2(mid.x, -mid.y);
                rt.sizeDelta = new Vector2(Vector2.Distance(a, b) + 12, 12);
                rt.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                segs[i] = seg;
            }
            RefArray(panel, "_segments", segs);

            var back = Btn("BackButton", f, "", Forest, Cream, Slate, 40, 36, 52, 52, 18, 0f);
            Icon("Icon", back.transform, "icon_back", Cream, 14, 14, 24);
            Ref(panel, "_backButton", back);
            Ref(panel, "_title", TxtAt("Title", f, "STAGE 1 · BAGUIO FOREST", Pixel, 34, Cream, 108, 36, 700, 36, TextAnchor.MiddleLeft));
            Ref(panel, "_subtitle", TxtAt("Sub", f, "", Body, 15, CreamDim, 108, 72, 480, 20));
            var factsBtn = Btn("FactsButton", f, "", Forest, Cream, Gold, 596, 36, 212, 52, 16, 0f);
            Icon("Icon", factsBtn.transform, "icon_list", Gold, 14, 14, 24);
            TxtAt("Label", factsBtn.transform, "CLIMATE FACTS", Pixel, 18, Cream, 48, 0, 160, 52, TextAnchor.MiddleLeft);
            Ref(panel, "_factsButton", factsBtn);
            // STORE (under Climate Facts): the same lock rule as the Main Menu (open once Level 1 is cleared).
            var storeBtn = Btn("StoreButton", f, "", Forest, Cream, Hex("#4E9A5A"), 596, 100, 212, 52, 16, 0f);
            var lsOn = NewRect("On", storeBtn.transform); Stretch(lsOn);
            Icon("Icon", lsOn, "icon_credit", Hex("#8FE09B"), 14, 14, 24);
            TxtAt("Label", lsOn, "STORE", Pixel, 18, Cream, 48, 0, 80, 52, TextAnchor.MiddleLeft);
            Ref(panel, "_storeBalance", TxtAt("Balance", lsOn, "0 CC", BodyBold, 14, Hex("#C9F5CF"), 110, 0, 92, 52, TextAnchor.MiddleRight));
            Ref(panel, "_storeOn", lsOn.gameObject);
            var lsOff = Img("Off", storeBtn.transform, Hex("#2A2F2B")); Stretch(lsOff.rectTransform);
            Border(lsOff.transform, Hex("#4A4F4B"));
            Icon("Icon", lsOff.transform, "icon_lock", Muted, 14, 14, 24);
            TxtAt("Label", lsOff.transform, "STORE", Pixel, 18, Muted, 48, 0, 70, 52, TextAnchor.MiddleLeft);
            TxtAt("Hint", lsOff.transform, "Clear Level 1", Body, 12, Muted, 112, 0, 92, 52, TextAnchor.MiddleRight);
            Ref(panel, "_storeOff", lsOff.gameObject);
            Ref(panel, "_storeButton", storeBtn);

            var nodes = new LevelNode[NodePos.Length];
            for (int i = 0; i < NodePos.Length; i++) nodes[i] = Node(f, i + 1, NodePos[i], i == 4, i == 9);
            RefArray(panel, "_nodes", nodes);

            // ---- detail panel ----
            At(Img("Details_Shadow", f, Color.black), 828 + 6, 40 + 6, 420, 640);
            var d = At(Img("Details", f, PanelDeep), 828, 40, 420, 640);
            Ref(panel, "_detailBorder", Border(d.transform, Moss));
            var tag = At(Img("Tag", d.transform, Moss), 22, 18, 120, 24);
            Ref(panel, "_tagBack", tag);
            var tagT = Txt("Text", tag.transform, "LEVEL 4", BodyBold, 14, Hex("#FFF6E6"), TextAnchor.MiddleCenter); Stretch(tagT.rectTransform);
            Ref(panel, "_tag", tagT);
            Ref(panel, "_status", TxtAt("Status", d.transform, "Next to play", Body, 14, CreamDim, 152, 18, 150, 24, TextAnchor.MiddleLeft));
            var detailStars = new Image[3];
            for (int i = 0; i < 3; i++) detailStars[i] = Icon($"Star_{i + 1}", d.transform, "icon_star", Gold, 318 + i * 28, 18, 24);
            RefArray(panel, "_detailStars", detailStars);
            Ref(panel, "_starHint", TxtAt("StarHint", d.transform, "", Body, 12, GoldLight, 22, 108, 376, 16));
            Ref(panel, "_name", TxtAt("Name", d.transform, "", BodyBold, 24, Cream, 22, 52, 376, 34, TextAnchor.MiddleLeft));
            Ref(panel, "_description", TxtAt("Desc", d.transform, "", Body, 15, CreamDim, 22, 86, 376, 20));
            At(Img("Divider", d.transform, Line), 0, 122, 420, 2);

            string[] statLabels = { "WAVES", "OP / TURN", "BEST" };
            string[] statFields = { "_waves", "_oxygen", "_best" };
            for (int i = 0; i < 3; i++)
            {
                var s = At(Img("Stat_" + statLabels[i], d.transform, MossDim), 22 + i * 128, 138, 120, 66);
                Border(s.transform, Line, "ui_frame2");
                TxtAt("Label", s.transform, statLabels[i], BodyBold, 12, CreamDim, 10, 8, 104, 16);
                Ref(panel, statFields[i], TxtAt("Value", s.transform, "3", BodyBold, i == 1 ? 19 : 26, Cream, 10, 26, 106, 32, TextAnchor.MiddleLeft));
            }
            TxtAt("ThreatLabel", d.transform, "THREAT", BodyBold, 13, CreamDim, 22, 218, 200, 18);
            Ref(panel, "_threatLabel", TxtAt("ThreatValue", d.transform, "4 / 10", BodyBold, 13, CreamDim, 278, 218, 120, 18, TextAnchor.MiddleRight));
            var bars = new Image[10];
            float bw = (376 - 27) / 10f;
            for (int i = 0; i < 10; i++) bars[i] = At(Img($"Threat_{i + 1}", d.transform, Line), 22 + i * (bw + 3), 240, bw, 14);
            RefArray(panel, "_threatBars", bars);

            TxtAt("EnemiesLabel", d.transform, "ENEMIES YOU WILL FACE", BodyBold, 13, CreamDim, 22, 268, 376, 18);
            var slots = new GameObject[4]; var icons = new Image[4]; var rings = new Image[4]; var names = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = At(NewRect($"Enemy_{i + 1}", d.transform).gameObject.AddComponent<Image>(), 22 + i * 92, 292, 84, 100);
                slot.color = Clear;
                var ic = At(Img("Back", slot.transform, MossDim), 10, 0, 64, 64);
                icons[i] = At(Img("Icon", ic.transform, Color.white), 2, 2, 60, 60); icons[i].preserveAspect = true;
                rings[i] = Border(ic.transform, Hex("#3B5446"), "ui_frame2");
                names[i] = TxtAt("Name", slot.transform, "", Body, 12, Cream, 0, 68, 84, 32, TextAnchor.UpperCenter);
                slots[i] = slot.gameObject;
            }
            RefArray(panel, "_enemySlots", slots); RefArray(panel, "_enemyIcons", icons); RefArray(panel, "_enemyRings", rings); RefArray(panel, "_enemyNames", names);

            var note = At(Img("Note", d.transform, new Color(0.953f, 0.918f, 0.843f, 0.07f)), 22, 400, 376, 76);   // light tint that suits every stage palette
            Border(note.transform, Hex("#3E3A28"), "ui_frame2");
            Ref(panel, "_note", TxtAt("Text", note.transform, "", Body, 14, Cream, 12, 6, 352, 64));
            // Two lines for the squad (five unit names), then the saved-game line, both clear of the PLAY button.
            Ref(panel, "_squad", TxtAt("Squad", d.transform, "Squad for this level: 5 unit types in the deploy bar", Body, 13, CreamDim, 22, 482, 376, 36));
            Ref(panel, "_saveNote", TxtAt("SaveNote", d.transform, "", BodyBold, 13, GoldLight, 22, 522, 376, 18));

            var play = Btn("PlayButton", d.transform, "PLAY", Red, Hex("#FFF6E6"), Ink, 22, 558, 376, 56, 24);
            Ref(panel, "_playButton", play);
            Ref(panel, "_playLabel", play.GetComponentInChildren<Text>());
            var locked = At(Img("Locked", d.transform, Clear), 22, 558, 376, 56);
            Border(locked.transform, Slate, "ui_frame_dashed");
            Icon("Icon", locked.transform, "icon_lock", CreamDim, 74, 18, 20);
            Ref(panel, "_lockedText", TxtAt("Text", locked.transform, "Clear Level 3 to unlock", Body, 16, CreamDim, 102, 0, 260, 56, TextAnchor.MiddleLeft));
            Ref(panel, "_lockedBox", locked.gameObject);

            root.gameObject.SetActive(false);
            return panel;
        }

        private static LevelNode Node(Transform f, int level, Vector2 centre, bool spike, bool boss)
        {
            float size = boss ? 92 : spike ? 74 : 60;
            var holder = NewRect($"Level_{level}", f);
            holder.anchorMin = holder.anchorMax = new Vector2(0, 1); holder.pivot = new Vector2(0.5f, 0.5f);
            holder.anchoredPosition = new Vector2(centre.x, -centre.y); holder.sizeDelta = new Vector2(size, size);

            var ring = Img("SelectRing", holder, Cream, Ui("ui_frame3")); ring.type = Image.Type.Sliced;
            Stretch(ring.rectTransform, -9);
            var shape = NewRect("Shape", holder); Stretch(shape);
            if (spike) shape.localRotation = Quaternion.Euler(0, 0, 45);
            ring.transform.SetParent(shape, false); Stretch(ring.rectTransform, -9);
            var shadow = Img("Shadow", shape, Color.black); Stretch(shadow.rectTransform); shadow.rectTransform.anchoredPosition = new Vector2(4, -4);
            var fill = Img("Fill", shape, Hex("#1A2620"), null, true); Stretch(fill.rectTransform);
            var border = Border(fill.transform, Slate);
            var label = Txt("Label", holder, level.ToString(), BodyBold, boss ? 22 : 26, Hex("#9F978A"), TextAnchor.MiddleCenter); Stretch(label.rectTransform);
            var btn = fill.gameObject.AddComponent<Button>(); btn.targetGraphic = fill;
            holder.gameObject.AddComponent<ButtonFx>();
            var cb = btn.colors; cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f); btn.colors = cb;

            var nodeStars = new Image[3];
            for (int i = 0; i < 3; i++)
                nodeStars[i] = Icon($"Star_{i + 1}", f, "icon_star", Gold, centre.x - 26 + i * 18 - 7, centre.y + size / 2 + 6, 14);
            var cap = Txt("Caption", f, "", BodyBold, 13, Cream, TextAnchor.UpperCenter);
            At(cap, centre.x - 70, centre.y + size / 2 + 22, 140, 20);
            var capShadow = cap.gameObject.AddComponent<Shadow>(); capShadow.effectColor = Color.black; capShadow.effectDistance = new Vector2(0, -1);

            var node = holder.gameObject.AddComponent<LevelNode>();
            RefInt(node, "_levelNumber", level);
            Ref(node, "_button", btn); Ref(node, "_fill", fill); Ref(node, "_border", border); Ref(node, "_label", label);
            Ref(node, "_caption", cap); Ref(node, "_selectRing", ring.gameObject);
            RefArray(node, "_stars", nodeStars);
            RefBool(node, "_isSpike", spike); RefBool(node, "_isBoss", boss);
            return node;
        }

        // =====================================================================
        // Results list (shared with the battle scene)
        // =====================================================================
        public static ResultsPanel BuildResults(Transform c)
        {
            var root = Overlay("ResultsPanel", c, out var f);
            var results = root.gameObject.AddComponent<ResultsPanel>();
            var d = Box("Dialog", f, Panel, Moss, 190, 60, 900, 600, 8f);
            Weave(d.transform, 0, 0, 900);
            var title = TxtAt("Title", d.transform, "LEADERBOARD", Pixel, 34, Cream, 28, 36, 600, 40, TextAnchor.MiddleLeft);
            var header = At(Img("Header", d.transform, Moss), 28, 92, 844, 40);
            RowTexts(header.transform, "#   NAME", "PLAYER CODE", "TOTAL STARS", Cream, BodyBold);

            var scroll = At(Img("Scroll", d.transform, new Color(0, 0, 0, 0.25f), null, true), 28, 136, 844, 360);
            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            var viewport = NewRect("Viewport", scroll.transform); Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false; vlg.spacing = 3;
            vlg.padding = new RectOffset(0, 0, 4, 4);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport; sr.content = content; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30;

            var row = Img("RowTemplate", content, new Color(1f, 1f, 1f, 0.04f));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            RowTexts(row.transform, "-", "-", "-", Cream, Body);

            var empty = Txt("EmptyText", scroll.transform, "No results yet. Finish a level to record one.", Body, 18, CreamDim, TextAnchor.MiddleCenter);
            Stretch(empty.rectTransform);
            var footer = TxtAt("Footer", d.transform, "", Body, 12, CreamDim, 28, 506, 620, 60);
            var close = Btn("CloseButton", d.transform, "CLOSE", Gold, Ink, Ink, 712, 518, 160, 50, 20);

            Ref(results, "_root", root.gameObject); Ref(results, "_content", content); Ref(results, "_rowTemplate", row.gameObject);
            Ref(results, "_emptyText", empty); Ref(results, "_footer", footer); Ref(results, "_closeButton", close);
            Ref(results, "_title", title);
            Ref(results, "_headerA", header.transform.Find("Name").GetComponent<Text>());
            Ref(results, "_headerB", header.transform.Find("Result").GetComponent<Text>());
            Ref(results, "_headerC", header.transform.Find("Date").GetComponent<Text>());
            return results;
        }

        private static void RowTexts(Transform row, string a, string b, string d, Color color, Font font)
        {
            void Col(string name, string value, float x0, float x1)
            {
                var t = Txt(name, row, value, font, 16, color, TextAnchor.MiddleLeft);
                t.rectTransform.anchorMin = new Vector2(x0, 0); t.rectTransform.anchorMax = new Vector2(x1, 1);
                t.rectTransform.offsetMin = new Vector2(16, 0); t.rectTransform.offsetMax = Vector2.zero;
            }
            Col("Name", a, 0f, 0.42f);
            Col("Result", b, 0.42f, 0.6f);
            Col("Date", d, 0.6f, 1f);
        }
    }
}
