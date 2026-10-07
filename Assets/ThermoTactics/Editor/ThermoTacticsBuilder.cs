using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Builds the playable Baguio level from the art in Assets/ThermoTactics/Art.
    /// Menu: Thermo-Tactics ▸ Build Baguio Level. Safe to run again after changing art or stats.
    /// </summary>
    public static class ThermoTacticsBuilder
    {
        private const string Root = "Assets/ThermoTactics";
        private const string ArtUnits = Root + "/Art/Units";
        private const string ArtEnv = Root + "/Art/Environment";
        private const string AnimDir = Root + "/Animations";
        private const string DataDir = Root + "/GameData";
        private const string ScenePath = Root + "/Scenes/Baguio_Level1.unity";
        private const string TemplateScene = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";

        private struct UnitDef
        {
            public string Folder, Id, Display; public Faction Side; public int Cost, Damage, Hp; public float PivotX;
            public UnitDef(string folder, string id, string display, Faction side, int cost, int damage, int hp, float pivotX)
            { Folder = folder; Id = id; Display = display; Side = side; Cost = cost; Damage = damage; Hp = hp; PivotX = pivotX; }
        }

        // Stats from the capstone overview (Section 10). Pivots keep the feet on the same pixel in every frame.
        private static readonly UnitDef[] Units =
        {
            new UnitDef("MangroveGuardian",   "A01", "Mangrove Guardian",   Faction.Ally, 3, 4, 20, 0.382f),
            new UnitDef("CoralDefender",      "A02", "Coral Defender",      Faction.Ally, 3, 4, 18, 0.361f),
            new UnitDef("WindCaller",         "A06", "Wind Caller",         Faction.Ally, 2, 4, 10, 0.375f),
            new UnitDef("GeothermalSentinel", "A08", "Geothermal Sentinel", Faction.Ally, 4, 6, 22, 0.344f),
            new UnitDef("ReefSentinel",       "A14", "Reef Sentinel",       Faction.Ally, 3, 5, 14, 0.368f),
            new UnitDef("IllegalLogger",      "E09", "Illegal Logger",      Faction.Enemy, 0, 5, 12, 0.323f),
            new UnitDef("MethaneCloud",       "E12", "Methane Cloud",       Faction.Enemy, 0, 3, 12, 0.382f),
            new UnitDef("QuarryColossus",     "E10", "Quarry Colossus",     Faction.Enemy, 0, 7, 22, 0.378f),
            new UnitDef("OilSlick",           "E07", "Oil Slick",           Faction.Enemy, 0, 3, 14, 0.399f),
            new UnitDef("PlasticGolem",       "E05", "Plastic Golem",       Faction.Enemy, 0, 4, 24, 0.347f),
        };
        private const float PivotY = 0.042f;

        [MenuItem("Thermo-Tactics/Build Baguio Level")]
        public static void BuildAll()
        {
            ImportArt();
            var controllers = BuildAnimations();
            LevelData level = BuildData(controllers);
            BuildScene(level);
            Debug.Log("[Thermo-Tactics] Baguio level built: " + ScenePath);
        }

        // =====================================================================
        // 1. Sprite import settings
        // =====================================================================
        [MenuItem("Thermo-Tactics/Steps/1. Import Art")]
        public static void ImportArt()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Art" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    string file = Path.GetFileNameWithoutExtension(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.spritePixelsPerUnit = file == "fog_drift_tileable" ? 32 : file == "pixel" ? 4 : 64;

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = PivotFor(path, file);
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static Vector2 PivotFor(string path, string file)
        {
            if (file == "pixel") return new Vector2(0f, 0.5f);
            if (path.StartsWith(ArtUnits) && file != "card")
                foreach (var u in Units)
                    if (path.Contains("/" + u.Folder + "/")) return new Vector2(u.PivotX, PivotY);
            return new Vector2(0.5f, 0.5f);
        }

        // =====================================================================
        // 2. Animation clips + Animator controllers (Idle / Attack / Hurt)
        // =====================================================================
        [MenuItem("Thermo-Tactics/Steps/2. Build Animations")]
        public static void BuildAnimationsMenu() => BuildAnimations();

        private static Dictionary<string, AnimatorController> BuildAnimations()
        {
            var result = new Dictionary<string, AnimatorController>();
            foreach (var u in Units)
            {
                string dir = $"{AnimDir}/{u.Folder}";
                EnsureFolder(dir);
                AnimationClip idle = MakeClip($"{dir}/{u.Folder}_Idle.anim", LoadFrames(u.Folder, "idle", 12), 12f, true);
                AnimationClip attack = MakeClip($"{dir}/{u.Folder}_Attack.anim", LoadFrames(u.Folder, "attack", 12), 14f, false);
                AnimationClip hurt = MakeClip($"{dir}/{u.Folder}_Hurt.anim", LoadFrames(u.Folder, "hurt", 8), 16f, false);

                string ctrlPath = $"{dir}/{u.Folder}.controller";
                AssetDatabase.DeleteAsset(ctrlPath);
                var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
                var sm = ctrl.layers[0].stateMachine;
                var sIdle = sm.AddState("Idle");   sIdle.motion = idle;
                var sAtk = sm.AddState("Attack");  sAtk.motion = attack;
                var sHurt = sm.AddState("Hurt");   sHurt.motion = hurt;
                sm.defaultState = sIdle;

                AddTrigger(sm.AddAnyStateTransition(sAtk), "Attack");
                AddTrigger(sm.AddAnyStateTransition(sHurt), "Hurt");
                foreach (var s in new[] { sAtk, sHurt })
                {
                    var back = s.AddTransition(sIdle);
                    back.hasExitTime = true; back.exitTime = 1f; back.duration = 0f;
                }
                EditorUtility.SetDirty(ctrl);
                result[u.Folder] = ctrl;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static void AddTrigger(AnimatorStateTransition t, string trigger)
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = true;
        }

        private static Sprite[] LoadFrames(string folder, string anim, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                string p = $"{ArtUnits}/{folder}/{anim}_{i:00}.png";
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (frames[i] == null) Debug.LogError("[Thermo-Tactics] Missing sprite " + p);
            }
            return frames;
        }

        private static AnimationClip MakeClip(string path, Sprite[] frames, float fps, bool loop)
        {
            AssetDatabase.DeleteAsset(path);
            var clip = new AnimationClip { frameRate = fps };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        // =====================================================================
        // 3. ScriptableObject game data
        // =====================================================================
        private static LevelData BuildData(Dictionary<string, AnimatorController> controllers)
        {
            EnsureFolder(DataDir + "/Units");
            EnsureFolder(DataDir + "/Waves");
            var byFolder = new Dictionary<string, UnitData>();
            foreach (var u in Units)
            {
                var data = LoadOrCreate<UnitData>($"{DataDir}/Units/{u.Id}_{u.Folder}.asset");
                data.unitId = u.Id;
                data.displayName = u.Display;
                data.faction = u.Side;
                data.opCost = u.Cost;
                data.damage = u.Damage;
                data.maxHp = u.Hp;
                data.card = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUnits}/{u.Folder}/card.png");
                data.idlePose = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUnits}/{u.Folder}/idle_00.png");
                data.animator = controllers[u.Folder];
                data.hitDelay = 6f / 14f;
                data.attackDuration = 12f / 14f;
                EditorUtility.SetDirty(data);
                byFolder[u.Folder] = data;
            }

            WaveData W(int n, int op, params string[] enemies)
            {
                var w = LoadOrCreate<WaveData>($"{DataDir}/Waves/Baguio_Wave{n}.asset");
                w.waveNumber = n; w.opAllowance = op;
                w.enemies = System.Array.ConvertAll(enemies, e => byFolder[e]);
                EditorUtility.SetDirty(w);
                return w;
            }

            var level = LoadOrCreate<LevelData>($"{DataDir}/Baguio_Level1.asset");
            level.levelName = "Stage 1 · Baguio — Kennon Road";
            level.startingTemperature = 1.0f;
            level.lossThreshold = 1.5f;
            level.heatPerUndefendedHit = 0.10f;
            level.coolingPerEmptyLaneAttack = 0.05f;
            level.opPenaltyByTurn = new[] { 0, 0, 1, 2, 4, 8 };
            level.enemyHpScale = 0.75f;
            level.allyRoster = new[] { byFolder["MangroveGuardian"], byFolder["CoralDefender"], byFolder["WindCaller"], byFolder["GeothermalSentinel"], byFolder["ReefSentinel"] };
            level.waves = new[]
            {
                W(1, 5, "IllegalLogger", "MethaneCloud"),
                W(2, 7, "OilSlick", "IllegalLogger", "PlasticGolem"),
                W(3, 10, "QuarryColossus", "PlasticGolem", "MethaneCloud", "OilSlick"),
            };
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            return level;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // =====================================================================
        // 4. Scene
        // =====================================================================
        private static Font _font;

        private static void BuildScene(LevelData level)
        {
            EnsureFolder(Root + "/Scenes");
            AssetDatabase.DeleteAsset(ScenePath);
            var scene = AssetDatabase.CopyAsset(TemplateScene, ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ---- camera ----
            Camera cam = Camera.main != null ? Camera.main : new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.16f, 0.18f, 0.22f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (cam.GetComponent<Physics2DRaycaster>() == null) cam.gameObject.AddComponent<Physics2DRaycaster>();

            // ---- environment ----
            var env = new GameObject("Environment").transform;
            var bg = NewSprite("Background_Baguio", env, Sprite(ArtEnv + "/baguio_background.png"), -1000);
            bg.transform.position = Vector3.zero;

            var fogRoot = new GameObject("Fog");
            fogRoot.transform.SetParent(env, false);
            var fog = fogRoot.AddComponent<FogScroller>();
            for (int i = 0; i < 2; i++)
            {
                var f = NewSprite($"FogTile_{i}", fogRoot.transform, Sprite(ArtEnv + "/fog_drift_tileable.png"), 900);
                f.transform.localPosition = new Vector3(21f * i, 0f, 0f);
                f.color = new Color(1f, 1f, 1f, 0.85f);
            }

            // ---- board slots ----
            var board = new GameObject("Board").transform;
            var allySlots = new BoardSlot[IsoBoardLayout.LaneCount];
            var enemySlots = new BoardSlot[IsoBoardLayout.LaneCount];
            for (int lane = 0; lane < IsoBoardLayout.LaneCount; lane++)
            {
                allySlots[lane] = NewSlot(board, lane, false);
                enemySlots[lane] = NewSlot(board, lane, true);
            }

            // ---- systems ----
            var sys = new GameObject("GameSystems");
            var gm = sys.AddComponent<GameManager>();
            var turns = sys.AddComponent<TurnManager>();
            var waves = sys.AddComponent<WaveManager>();
            var oxygen = sys.AddComponent<OxygenPointSystem>();
            var temp = sys.AddComponent<TemperatureSystem>();
            var grid = sys.AddComponent<GridManager>();
            var lanes = sys.AddComponent<LaneController>();
            var deploy = sys.AddComponent<DeploySystem>();
            var factory = sys.AddComponent<UnitFactory>();
            var db = sys.AddComponent<ResultsDatabase>();

            // ---- UI ----
            var ui = BuildUI(out var refs);
            var uiManager = ui.AddComponent<UIManager>();

            // ---- wiring ----
            // Reload by path: the instance returned while assets were being created can go stale.
            level = AssetDatabase.LoadAssetAtPath<LevelData>($"{DataDir}/Baguio_Level1.asset");
            Ref(gm, "_level", level); Ref(gm, "_turns", turns); Ref(gm, "_waves", waves); Ref(gm, "_oxygen", oxygen);
            Ref(gm, "_temperature", temp); Ref(gm, "_grid", grid); Ref(gm, "_lanes", lanes); Ref(gm, "_deploy", deploy);
            Ref(gm, "_database", db); Ref(gm, "_ui", uiManager); Ref(gm, "_fog", fog);
            Ref(turns, "_grid", grid); Ref(turns, "_waves", waves); Ref(turns, "_oxygen", oxygen);
            Ref(turns, "_temperature", temp); Ref(turns, "_lanes", lanes); Ref(turns, "_ui", uiManager);
            Ref(waves, "_grid", grid); Ref(waves, "_factory", factory);
            RefArray(grid, "_allySlots", allySlots); RefArray(grid, "_enemySlots", enemySlots);
            Ref(lanes, "_temperature", temp); Ref(lanes, "_ui", uiManager);
            Ref(deploy, "_grid", grid); Ref(deploy, "_oxygen", oxygen); Ref(deploy, "_factory", factory); Ref(deploy, "_ui", uiManager);
            Ref(factory, "_shadowSprite", Sprite(ArtEnv + "/unit_shadow.png")); Ref(factory, "_pixelSprite", Sprite(ArtEnv + "/pixel.png"));

            foreach (var kv in refs) Ref(uiManager, kv.Key, kv.Value);
            RefArray(uiManager, "_cards", refs_cards.ToArray());
            Ref(uiManager, "_temperature", temp); Ref(uiManager, "_oxygen", oxygen); Ref(uiManager, "_waves", waves);
            Ref(uiManager, "_deploy", deploy); Ref(uiManager, "_turns", turns); Ref(uiManager, "_database", db); Ref(uiManager, "_font", _font);

            // ---- event system (new Input System module) ----
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes) if (s.path != ScenePath) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static BoardSlot NewSlot(Transform parent, int lane, bool enemy)
        {
            Vector3 pos = IsoBoardLayout.SlotPosition(lane, enemy);
            var marker = NewSprite($"{(enemy ? "EnemySlot" : "AllySlot")}_L{lane}", parent, Sprite(ArtEnv + "/slot_marker.png"), -500);
            marker.transform.position = pos;
            var hl = NewSprite("DeployHighlight", marker.transform, Sprite(ArtEnv + "/slot_highlight_deploy.png"), -499);
            hl.transform.localPosition = Vector3.zero;
            hl.enabled = false;
            var col = marker.gameObject.AddComponent<PolygonCollider2D>();
            col.points = new[] { new Vector2(0f, 0.45f), new Vector2(0.9f, 0f), new Vector2(0f, -0.45f), new Vector2(-0.9f, 0f) };
            var slot = marker.gameObject.AddComponent<BoardSlot>();
            slot.Configure(lane, enemy, hl);
            Ref(slot, "_lane", lane); Ref(slot, "_isEnemySide", enemy); Ref(slot, "_highlight", hl);
            return slot;
        }

        // =====================================================================
        // UI construction
        // =====================================================================
        private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.12f, 0.94f);
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Green = new Color(0.22f, 0.62f, 0.36f);
        private static readonly Color Blue = new Color(0.2f, 0.42f, 0.7f);
        private static readonly Color Orange = new Color(0.86f, 0.42f, 0.16f);
        private static readonly Color Grey = new Color(0.3f, 0.34f, 0.4f);
        private static List<DeployCard> refs_cards = new List<DeployCard>();

        private static GameObject BuildUI(out Dictionary<string, Object> refs)
        {
            refs = new Dictionary<string, Object>();
            refs_cards = new List<DeployCard>();

            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform c = canvasGo.transform;

            // ---------------- HUD ----------------
            var hud = Rect("HUD", c); Stretch(hud);
            refs["_hud"] = hud.gameObject;

            var top = Image("TopBar", hud, new Color(0.05f, 0.07f, 0.1f, 0.82f));
            Place(top.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 118));

            var playerLbl = Text("PlayerLabel", top.transform, "PLAYER", 20, new Color(0.6f, 0.7f, 0.8f), TextAnchor.UpperLeft);
            Place(playerLbl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -18), new Vector2(300, 30));
            var player = Text("PlayerValue", top.transform, "-", 30, Color.white, TextAnchor.UpperLeft, true);
            Place(player.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -50), new Vector2(320, 44));
            refs["_playerValue"] = player;

            var tLbl = Text("TemperatureLabel", top.transform, "GLOBAL TEMPERATURE", 20, new Color(0.6f, 0.7f, 0.8f), TextAnchor.UpperLeft);
            Place(tLbl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(390, -16), new Vector2(560, 30));
            var tBack = Image("TemperatureBar", top.transform, new Color(0.1f, 0.12f, 0.16f, 0.95f));
            Place(tBack.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(390, -48), new Vector2(560, 28));
            var tFill = Image("Fill", tBack.transform, new Color(0.3f, 0.8f, 0.45f), Sprite(ArtEnv + "/pixel.png"));
            Stretch(tFill.rectTransform, 3);
            tFill.type = UnityEngine.UI.Image.Type.Filled;
            tFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            tFill.fillOrigin = 0;
            tFill.fillAmount = 0.05f;
            var tVal = Text("TemperatureValue", top.transform, "1.00°C / limit 1.5°C", 24, Color.white, TextAnchor.UpperLeft, true);
            Place(tVal.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(390, -80), new Vector2(560, 34));
            refs["_temperatureFill"] = tFill; refs["_temperatureBack"] = tBack; refs["_temperatureValue"] = tVal;

            var wave = Text("WaveValue", top.transform, "WAVE 1 / 3", 34, Color.white, TextAnchor.MiddleCenter, true);
            Place(wave.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(150, -20), new Vector2(560, 76));
            refs["_waveValue"] = wave;

            var oLbl = Text("OxygenLabel", top.transform, "OXYGEN POINTS", 20, new Color(0.6f, 0.8f, 0.7f), TextAnchor.UpperRight);
            Place(oLbl.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -16), new Vector2(360, 30));
            var oVal = Text("OxygenValue", top.transform, "5 / 5", 50, new Color(0.55f, 1f, 0.7f), TextAnchor.UpperRight, true);
            Place(oVal.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -44), new Vector2(360, 64));
            refs["_oxygenValue"] = oVal;

            var bottom = Image("DeployBar", hud, new Color(0.05f, 0.07f, 0.1f, 0.78f));
            Place(bottom.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 232));
            var hint = Text("Hint", bottom.transform, "Tap a card, then tap a glowing slot on the road.  Tap END TURN when ready.", 22, new Color(0.75f, 0.82f, 0.9f), TextAnchor.MiddleLeft);
            Place(hint.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -6), new Vector2(1200, 34));

            for (int i = 0; i < 5; i++)
                refs_cards.Add(Card(bottom.transform, i));

            var endTurn = Button("EndTurnButton", bottom.transform, "END TURN", Orange, 40);
            Place((RectTransform)endTurn.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 34), new Vector2(330, 150));
            refs["_endTurnButton"] = endTurn;

            var floating = Rect("FloatingLayer", hud); Stretch(floating);
            refs["_floatingLayer"] = floating;

            var announce = Text("Announcement", hud, "", 38, Color.white, TextAnchor.MiddleCenter, true);
            Place(announce.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1500, 70));
            announce.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);
            announce.raycastTarget = false;
            announce.gameObject.SetActive(false);
            refs["_announcement"] = announce;

            // ---------------- Login ----------------
            var login = Image("LoginPanel", c, Dim); Stretch(login.rectTransform);
            refs["_loginPanel"] = login.gameObject;
            var lbox = Image("Box", login.transform, PanelColor);
            Place(lbox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 640));
            Title(lbox.transform, "THERMO-TACTICS", 76, -50, new Color(1f, 0.82f, 0.35f));
            Title(lbox.transform, "Stage 1 · Baguio — Kennon Road", 30, -140, new Color(0.75f, 0.85f, 0.95f));
            var nameLbl = Text("UsernameLabel", lbox.transform, "USERNAME", 24, new Color(0.65f, 0.75f, 0.85f), TextAnchor.MiddleLeft);
            Place(nameLbl.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -215), new Vector2(620, 34));
            var input = InputField(lbox.transform, "UsernameInput", "Enter your username");
            Place((RectTransform)input.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -255), new Vector2(620, 84));
            refs["_nameInput"] = input;
            var msg = Text("Message", lbox.transform, "", 24, new Color(1f, 0.5f, 0.45f), TextAnchor.MiddleCenter);
            Place(msg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(620, 36));
            refs["_loginMessage"] = msg;
            var start = Button("StartButton", lbox.transform, "START", Green, 40);
            Place((RectTransform)start.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -400), new Vector2(620, 96));
            refs["_startButton"] = start;
            var lres = Button("ResultsButton", lbox.transform, "Results", Blue, 32);
            Place((RectTransform)lres.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -514), new Vector2(620, 76));
            refs["_loginResultsButton"] = lres;

            // ---------------- End ----------------
            var end = Image("EndPanel", c, Dim); Stretch(end.rectTransform);
            refs["_endPanel"] = end.gameObject;
            var ebox = Image("Box", end.transform, PanelColor);
            Place(ebox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 600));
            var etitle = Title(ebox.transform, "VICTORY", 90, -50, Color.white);
            refs["_endTitle"] = etitle;
            var ebody = Text("Body", ebox.transform, "", 30, new Color(0.88f, 0.92f, 0.96f), TextAnchor.UpperCenter);
            Place(ebody.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(880, 250));
            refs["_endBody"] = ebody;
            var again = Button("PlayAgainButton", ebox.transform, "Play Again", Green, 32);
            Place((RectTransform)again.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-300, 50), new Vector2(270, 90));
            var eres = Button("ResultsButton", ebox.transform, "Results", Blue, 32);
            Place((RectTransform)eres.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(270, 90));
            var menu = Button("MainMenuButton", ebox.transform, "Main Menu", Grey, 32);
            Place((RectTransform)menu.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(300, 50), new Vector2(270, 90));
            refs["_playAgainButton"] = again; refs["_endResultsButton"] = eres; refs["_menuButton"] = menu;

            // ---------------- Results ----------------
            var resRoot = Image("ResultsPanel", c, Dim); Stretch(resRoot.rectTransform);
            var results = resRoot.gameObject.AddComponent<ResultsPanel>();
            var rbox = Image("Box", resRoot.transform, PanelColor);
            Place(rbox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 860));
            Title(rbox.transform, "Results", 64, -36, new Color(1f, 0.82f, 0.35f));
            var header = Image("Header", rbox.transform, new Color(0.13f, 0.2f, 0.32f, 1f));
            Place(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1140, 56));
            RowTexts(header.transform, "NAME", "RESULT", "DATE CREATED", new Color(0.75f, 0.85f, 1f), true);

            var scroll = Image("Scroll", rbox.transform, new Color(0, 0, 0, 0.25f));
            Place(scroll.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -192), new Vector2(1140, 520));
            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", scroll.transform); Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false; vlg.spacing = 4;
            vlg.padding = new RectOffset(0, 0, 6, 6);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport; sr.content = content; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30;

            var row = Image("RowTemplate", content, new Color(1f, 1f, 1f, 0.05f));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;
            RowTexts(row.transform, "-", "-", "-", Color.white, false);

            var empty = Text("EmptyText", scroll.transform, "No results yet. Finish a level to record one.", 28, new Color(0.7f, 0.75f, 0.8f), TextAnchor.MiddleCenter);
            Stretch(empty.rectTransform);
            var footer = Text("Footer", rbox.transform, "", 20, new Color(0.55f, 0.6f, 0.68f), TextAnchor.MiddleLeft);
            Place(footer.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-170, 54), new Vector2(800, 60));
            footer.horizontalOverflow = HorizontalWrapMode.Wrap;
            var close = Button("CloseButton", rbox.transform, "Close", Grey, 32);
            Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(440, 44), new Vector2(240, 84));

            Ref(results, "_root", resRoot.gameObject); Ref(results, "_content", content); Ref(results, "_rowTemplate", row.gameObject);
            Ref(results, "_emptyText", empty); Ref(results, "_footer", footer); Ref(results, "_closeButton", close);
            refs["_results"] = results;

            end.gameObject.SetActive(false);
            return canvasGo;
        }

        private static DeployCard Card(Transform parent, int index)
        {
            var frame = Image($"Card_{index + 1}", parent, new Color(0.12f, 0.14f, 0.18f, 0.95f));
            Place(frame.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-560 + index * 190, 14), new Vector2(176, 182));
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            var icon = Image("Icon", frame.transform, Color.white);
            Place(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(118, 118));
            icon.raycastTarget = false;
            var name = Text("Name", frame.transform, "Unit", 17, Color.white, TextAnchor.MiddleCenter, true);
            Place(name.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(170, 26));
            var cost = Text("Cost", frame.transform, "2 OP", 22, new Color(0.55f, 1f, 0.65f), TextAnchor.MiddleCenter, true);
            Place(cost.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(170, 28));
            var card = frame.gameObject.AddComponent<DeployCard>();
            Ref(card, "_button", btn); Ref(card, "_icon", icon); Ref(card, "_frame", frame); Ref(card, "_name", name); Ref(card, "_cost", cost);
            return card;
        }

        private static void RowTexts(Transform row, string a, string b, string d, Color color, bool bold)
        {
            void Col(string name, string value, float x0, float x1)
            {
                var t = Text(name, row, value, 26, color, TextAnchor.MiddleLeft, bold);
                t.rectTransform.anchorMin = new Vector2(x0, 0); t.rectTransform.anchorMax = new Vector2(x1, 1);
                t.rectTransform.offsetMin = new Vector2(24, 0); t.rectTransform.offsetMax = Vector2.zero;
            }
            Col("Name", a, 0f, 0.42f);
            Col("Result", b, 0.42f, 0.6f);
            Col("Date", d, 0.6f, 1f);
        }

        // ---------------- UI helpers ----------------
        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null) img.sprite = sprite;
            return img;
        }

        private static Text Text(string name, Transform parent, string value, int size, Color color, TextAnchor align, bool bold = false)
        {
            var t = Rect(name, parent).gameObject.AddComponent<Text>();
            t.font = _font; t.text = value; t.fontSize = size; t.color = color; t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static Text Title(Transform parent, string value, int size, float y, Color color)
        {
            var t = Text("Title_" + value.Split(' ')[0], parent, value, size, color, TextAnchor.MiddleCenter, true);
            Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(900, size + 24));
            return t;
        }

        private static Button Button(string name, Transform parent, string label, Color color, int size)
        {
            var img = Image(name, parent, color);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            b.colors = colors;
            var t = Text("Label", img.transform, label, size, Color.white, TextAnchor.MiddleCenter, true);
            Stretch(t.rectTransform);
            return b;
        }

        private static InputField InputField(Transform parent, string name, string placeholder)
        {
            var bg = Image(name, parent, new Color(0.93f, 0.95f, 0.97f));
            var field = bg.gameObject.AddComponent<InputField>();
            var text = Text("Text", bg.transform, "", 36, new Color(0.08f, 0.1f, 0.14f), TextAnchor.MiddleLeft);
            Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(24, 6); text.rectTransform.offsetMax = new Vector2(-24, -6);
            text.supportRichText = false;
            var ph = Text("Placeholder", bg.transform, placeholder, 34, new Color(0.45f, 0.5f, 0.58f), TextAnchor.MiddleLeft);
            ph.fontStyle = FontStyle.Italic;
            Stretch(ph.rectTransform); ph.rectTransform.offsetMin = new Vector2(24, 6); ph.rectTransform.offsetMax = new Vector2(-24, -6);
            field.textComponent = text; field.placeholder = ph; field.targetGraphic = bg;
            field.characterLimit = ResultsDatabase.MaxNameLength;
            field.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            return field;
        }

        // ---------------- general helpers ----------------
        private static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private static Sprite Sprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogError("[Thermo-Tactics] Missing sprite " + path);
            return s;
        }

        private static void Ref(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Thermo-Tactics] {target.GetType().Name}.{field} not found"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Ref(Object target, string field, int value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).intValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Ref(Object target, string field, bool value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RefArray<T>(Object target, string field, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
