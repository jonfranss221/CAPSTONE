using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static ThermoTactics.EditorTools.UiKit;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Builds the game from the art in Assets/ThermoTactics/Art:
    /// sprites → animations → unit, wave, level and stage data → Main Menu scene → Baguio battle scene.
    /// Menu: Thermo-Tactics ▸ Build Everything. Safe to run again; existing assets keep their IDs.
    /// </summary>
    public static class ThermoTacticsBuilder
    {
        private const string ArtUnits = Root + "/Art/Units";
        private const string ArtBosses = Root + "/Art/Bosses";
        private const string ArtEnv = Root + "/Art/Environment";
        private const string AnimDir = Root + "/Animations";
        private const string DataDir = Root + "/GameData";
        public const string ScenePath = Root + "/Scenes/Baguio_Battle.unity";
        private const string OldScenePath = Root + "/Scenes/Baguio_Level1.unity";
        private const string TemplateScene = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
        public const string StagePath = DataDir + "/Stage1_Baguio.asset";

        private struct UnitDef
        {
            public string Folder, Id, Display; public Faction Side; public int Cost, Damage, Hp; public float PivotX;
            public UnitDef(string folder, string id, string display, Faction side, int cost, int damage, int hp, float pivotX)
            { Folder = folder; Id = id; Display = display; Side = side; Cost = cost; Damage = damage; Hp = hp; PivotX = pivotX; }
        }

        // Stats from the capstone overview (Section 10) and the Stage 1 level plan. Pivots keep the feet on the same pixel in every frame.
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
            new UnitDef("MiningRavager",      "E08", "Mining Ravager",      Faction.Enemy, 0, 6, 18, 0.389f),
            new UnitDef("SmogWraith",         "E03", "Smog Wraith",         Faction.Enemy, 0, 5, 12, 0.368f),
            new UnitDef("DeforestationBeast", "E04", "Deforestation Beast", Faction.Enemy, 0, 7, 20, 0.385f),
        };
        private const float PivotY = 0.042f;

        // Elite variants (Level 5 on): base art with a red tint, ×1.5 HP and +1 damage.
        private static readonly (string id, string display, string baseFolder, int hp, int dmg)[] Elites =
        {
            ("EL09", "Chainsaw Logger",       "IllegalLogger",      18, 6),
            ("EL07", "Crude Oil Slick",       "OilSlick",           21, 4),
            ("EL05", "Hardened Golem",        "PlasticGolem",       36, 5),
            ("EL10", "Elite Quarry Colossus", "QuarryColossus",     33, 8),
            ("EL08", "Elite Ravager",         "MiningRavager",      27, 7),
            ("EL04", "Elite Beast",           "DeforestationBeast", 30, 8),
        };
        private static readonly Color EliteTint = new Color(1f, 0.74f, 0.7f);

        // The Stage 1 boss (art: Art/Bosses/ClearcutBehemoth, 384 × 320 frames).
        private const string BossFolder = "ClearcutBehemoth";
        private static readonly Vector2 BossPivot = new Vector2(0.52f, 0.031f);

        [MenuItem("Thermo-Tactics/Build Everything")]
        public static void BuildAll()
        {
            ImportArt();
            var controllers = BuildAnimations();
            StageData stage = BuildData(controllers);
            MenuSceneBuilder.Build(stage);
            BuildScene(stage);
            SetBuildScenes();
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = "0.2.0";
            AssetDatabase.SaveAssets();
            Debug.Log("[Thermo-Tactics] Built: Main Menu, Baguio battle scene, Levels 1–10 and the boss.");
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
                    bool ui = path.StartsWith(ArtUI);
                    bool smooth = ui && file.StartsWith("icon_");

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = file == "weave_tile" || file == "fog_drift_tileable" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    importer.spritePixelsPerUnit = ui ? 100 : file == "fog_drift_tileable" ? 32 : file == "pixel" ? 4 : 64;

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = PivotFor(path, file);
                    importer.SetTextureSettings(settings);
                    importer.spriteBorder = file == "ui_frame3" || file == "ui_frame_dashed" ? new Vector4(3, 3, 3, 3)
                                          : file == "ui_frame2" ? new Vector4(2, 2, 2, 2) : Vector4.zero;
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
            if (path.StartsWith(ArtBosses) && file != "card") return BossPivot;
            if (path.StartsWith(ArtUnits) && file != "card")
                foreach (var u in Units)
                    if (path.Contains("/" + u.Folder + "/")) return new Vector2(u.PivotX, PivotY);
            return new Vector2(0.5f, 0.5f);
        }

        // =====================================================================
        // 2. Animation clips + Animator controllers
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
                AnimationClip idle = MakeClip($"{dir}/{u.Folder}_Idle.anim", LoadFrames(ArtUnits, u.Folder, "idle", 12), 12f, true);
                AnimationClip attack = MakeClip($"{dir}/{u.Folder}_Attack.anim", LoadFrames(ArtUnits, u.Folder, "attack", 12), 14f, false);
                AnimationClip hurt = MakeClip($"{dir}/{u.Folder}_Hurt.anim", LoadFrames(ArtUnits, u.Folder, "hurt", 8), 16f, false);

                var ctrl = ResetController($"{dir}/{u.Folder}.controller");
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
            result[BossFolder] = BuildBossAnimations();
            AssetDatabase.SaveAssets();
            return result;
        }

        /// <summary>Boss: Idle / Enraged idle (bool "Enraged") / Cleave (trigger "Attack") / Slam / Hurt.</summary>
        private static AnimatorController BuildBossAnimations()
        {
            string dir = $"{AnimDir}/{BossFolder}";
            EnsureFolder(dir);
            AnimationClip idle = MakeClip($"{dir}/{BossFolder}_Idle.anim", LoadFrames(ArtBosses, BossFolder, "idle", 12), 12f, true);
            AnimationClip enraged = MakeClip($"{dir}/{BossFolder}_Enraged.anim", LoadFrames(ArtBosses, BossFolder, "enraged", 12), 15f, true);
            AnimationClip cleave = MakeClip($"{dir}/{BossFolder}_Cleave.anim", LoadFrames(ArtBosses, BossFolder, "cleave", 12), 14f, false);
            AnimationClip slam = MakeClip($"{dir}/{BossFolder}_Slam.anim", LoadFrames(ArtBosses, BossFolder, "slam", 12), 14f, false);
            AnimationClip hurt = MakeClip($"{dir}/{BossFolder}_Hurt.anim", LoadFrames(ArtBosses, BossFolder, "hurt", 8), 16f, false);

            var ctrl = ResetController($"{dir}/{BossFolder}.controller");
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Slam", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Enraged", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;
            var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
            var sRage = sm.AddState("Enraged"); sRage.motion = enraged;
            var sCleave = sm.AddState("Cleave"); sCleave.motion = cleave;
            var sSlam = sm.AddState("Slam"); sSlam.motion = slam;
            var sHurt = sm.AddState("Hurt"); sHurt.motion = hurt;
            sm.defaultState = sIdle;

            var toRage = sIdle.AddTransition(sRage);
            toRage.AddCondition(AnimatorConditionMode.If, 0f, "Enraged"); toRage.hasExitTime = false; toRage.duration = 0f;
            AddTrigger(sm.AddAnyStateTransition(sCleave), "Attack");
            AddTrigger(sm.AddAnyStateTransition(sSlam), "Slam");
            AddTrigger(sm.AddAnyStateTransition(sHurt), "Hurt");
            foreach (var s in new[] { sCleave, sSlam, sHurt })
            {
                var calm = s.AddTransition(sIdle);
                calm.AddCondition(AnimatorConditionMode.IfNot, 0f, "Enraged");
                calm.hasExitTime = true; calm.exitTime = 1f; calm.duration = 0f;
                var rage = s.AddTransition(sRage);
                rage.AddCondition(AnimatorConditionMode.If, 0f, "Enraged");
                rage.hasExitTime = true; rage.exitTime = 1f; rage.duration = 0f;
            }
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>Loads the controller and empties it (keeps its asset ID), or creates it.</summary>
        private static AnimatorController ResetController(string path)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) return AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var p in ctrl.parameters) ctrl.RemoveParameter(p);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
            foreach (var s in sm.states) sm.RemoveState(s.state);
            return ctrl;
        }

        private static void AddTrigger(AnimatorStateTransition t, string trigger)
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = true;
        }

        private static Sprite[] LoadFrames(string root, string folder, string anim, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                string p = $"{root}/{folder}/{anim}_{i:00}.png";
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (frames[i] == null) Debug.LogError("[Thermo-Tactics] Missing sprite " + p);
            }
            return frames;
        }

        /// <summary>Creates the clip, or rewrites an existing one in place so its asset ID stays the same.</summary>
        private static AnimationClip MakeClip(string path, Sprite[] frames, float fps, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew) clip = new AnimationClip();
            clip.ClearCurves();
            clip.frameRate = fps;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNew) AssetDatabase.CreateAsset(clip, path); else EditorUtility.SetDirty(clip);
            return clip;
        }

        // =====================================================================
        // 3. ScriptableObject game data: units, elites, boss, 10 levels, the stage
        // =====================================================================
        private struct LevelDef
        {
            public string Name, Caption, Subtitle, Note; public int Threat; public float Scale; public bool Spike, Boss;
            public (int op, string[] enemies)[] Waves;
        }

        // Stage 1 level plan (Part III, Table III-5). Enemy keys: unit folder, "elite:<folder>", or "boss".
        private static readonly LevelDef[] Levels =
        {
            new LevelDef { Name = "Kennon Road", Caption = "Kennon Road", Subtitle = "The zigzag climb into the mountains", Threat = 1, Scale = 0.60f,
                Note = "Tutorial pace. Learn to deploy, end the turn, and cool empty lanes.",
                Waves = new[] { (5, new[] { "IllegalLogger", "MethaneCloud" }), (6, new[] { "OilSlick", "MethaneCloud" }), (7, new[] { "IllegalLogger", "OilSlick", "MethaneCloud" }) } },
            new LevelDef { Name = "Lion's Head", Caption = "Lion's Head", Subtitle = "The guardian of Kennon Road", Threat = 2, Scale = 0.65f,
                Note = "New enemy: Plastic Golem, a slow tank with 24 HP.",
                Waves = new[] { (5, new[] { "IllegalLogger", "MethaneCloud" }), (7, new[] { "OilSlick", "IllegalLogger", "PlasticGolem" }), (8, new[] { "PlasticGolem", "MethaneCloud", "OilSlick" }) } },
            new LevelDef { Name = "Camp John Hay", Caption = "Camp John Hay", Subtitle = "Old pine forest", Threat = 3, Scale = 0.75f,
                Note = "New enemy: Quarry Colossus hits for 7. Put a sturdy unit in its lane.",
                Waves = new[] { (5, new[] { "IllegalLogger", "MethaneCloud" }), (7, new[] { "OilSlick", "IllegalLogger", "PlasticGolem" }), (10, new[] { "QuarryColossus", "PlasticGolem", "MethaneCloud", "OilSlick" }) } },
            new LevelDef { Name = "Burnham Park", Caption = "Burnham Park", Subtitle = "The lake in the city centre", Threat = 4, Scale = 0.80f,
                Note = "Two Quarry Colossi. Geothermal Sentinels (22 HP) hold their lanes best.",
                Waves = new[] { (6, new[] { "IllegalLogger", "OilSlick", "MethaneCloud" }), (8, new[] { "QuarryColossus", "PlasticGolem", "IllegalLogger" }), (10, new[] { "QuarryColossus", "PlasticGolem", "OilSlick", "MethaneCloud" }) } },
            new LevelDef { Name = "Session Road", Caption = "Session Road", Subtitle = "The elite invasion", Threat = 7, Scale = 0.85f, Spike = true,
                Note = "Difficulty spike. Elite variants (+50% HP, +1 damage, red tint) and the new Mining Ravager. A 4th wave is added.",
                Waves = new[] { (6, new[] { "elite:IllegalLogger", "MethaneCloud", "OilSlick" }), (8, new[] { "MiningRavager", "elite:OilSlick", "IllegalLogger" }),
                                (10, new[] { "elite:QuarryColossus", "MiningRavager", "PlasticGolem", "MethaneCloud" }), (11, new[] { "elite:PlasticGolem", "MiningRavager", "elite:IllegalLogger", "SmogWraith", "OilSlick" }) } },
            new LevelDef { Name = "Mines View Park", Caption = "Mines View", Subtitle = "Overlooking the old mines", Threat = 5, Scale = 0.80f,
                Note = "A breather after Level 5. New enemy: Smog Wraith.",
                Waves = new[] { (6, new[] { "MiningRavager", "IllegalLogger", "MethaneCloud" }), (8, new[] { "SmogWraith", "PlasticGolem", "OilSlick", "IllegalLogger" }), (10, new[] { "QuarryColossus", "MiningRavager", "SmogWraith", "MethaneCloud" }) } },
            new LevelDef { Name = "Strawberry Farm", Caption = "La Trinidad", Subtitle = "Strawberry fields of La Trinidad", Threat = 6, Scale = 0.85f,
                Note = "New enemy: Deforestation Beast, the boss in miniature.",
                Waves = new[] { (6, new[] { "SmogWraith", "MiningRavager", "OilSlick" }), (8, new[] { "elite:IllegalLogger", "PlasticGolem", "SmogWraith", "MethaneCloud" }), (10, new[] { "DeforestationBeast", "QuarryColossus", "MiningRavager", "OilSlick" }) } },
            new LevelDef { Name = "Busol Watershed", Caption = "Busol", Subtitle = "The city water source", Threat = 7, Scale = 0.90f,
                Note = "Four waves with elites mixed in.",
                Waves = new[] { (7, new[] { "MiningRavager", "SmogWraith", "IllegalLogger", "MethaneCloud" }), (9, new[] { "DeforestationBeast", "elite:OilSlick", "PlasticGolem", "SmogWraith" }),
                                (11, new[] { "elite:QuarryColossus", "DeforestationBeast", "MiningRavager", "SmogWraith", "OilSlick" }), (11, new[] { "elite:PlasticGolem", "elite:IllegalLogger", "DeforestationBeast", "SmogWraith", "OilSlick" }) } },
            new LevelDef { Name = "Tam-awan Village", Caption = "Tam-awan", Subtitle = "Artists' village in the hills", Threat = 8, Scale = 0.90f,
                Note = "Five-enemy waves. The last test before the boss.",
                Waves = new[] { (7, new[] { "DeforestationBeast", "SmogWraith", "IllegalLogger", "OilSlick" }), (9, new[] { "elite:MiningRavager", "PlasticGolem", "SmogWraith", "MethaneCloud" }),
                                (10, new[] { "elite:QuarryColossus", "DeforestationBeast", "SmogWraith", "OilSlick", "IllegalLogger" }), (11, new[] { "elite:DeforestationBeast", "elite:PlasticGolem", "MiningRavager", "SmogWraith", "elite:IllegalLogger" }) } },
            new LevelDef { Name = "Mt. Santo Tomas", Caption = "Mt. Santo Tomas", Subtitle = "The last stand", Threat = 10, Scale = 0.90f, Boss = true,
                Note = "Climax. Four waves, then Wave 5: the Clearcut Behemoth, a solo boss filling all 6 lanes, with cleave and area attacks.",
                Waves = new[] { (7, new[] { "MiningRavager", "SmogWraith", "IllegalLogger", "MethaneCloud" }), (9, new[] { "DeforestationBeast", "elite:OilSlick", "PlasticGolem", "SmogWraith" }),
                                (10, new[] { "elite:QuarryColossus", "DeforestationBeast", "MiningRavager", "OilSlick", "MethaneCloud" }),
                                (11, new[] { "elite:DeforestationBeast", "elite:MiningRavager", "elite:PlasticGolem", "SmogWraith", "SmogWraith", "OilSlick" }), (8, new[] { "boss" }) } },
        };

        // =====================================================================
        // Education content for Stage 1 (question bank "Thermo-Tactics: Pre/Post Quiz Question Bank").
        // Pre-quiz = Q1–Q5, post-quiz = Q6–Q10. Both are tagged with the same three topics, so learning is
        // compared topic by topic. Every topic has climate facts (pop-ups) in Levels 1–9 that review the post-quiz.
        // =====================================================================
        private static readonly (string id, string name)[] BaguioTopics =
        {
            ("climate", "Climate basics"),
            ("forests", "Forests & carbon"),
            ("game", "Game & real world"),
        };

        private struct QuizDef
        {
            public string Id, TopicId, Question; public string[] Choices; public char Answer;
        }

        private static readonly QuizDef[] BaguioPre =
        {
            new QuizDef { Id = "S1_PRE_Q1", TopicId = "climate", Answer = 'B',
                Question = "What does the \"1.5°C target\" in the Paris Agreement actually mean?",
                Choices = new[] { "The maximum temperature a human body can survive",
                                  "The limit for global warming above pre-industrial levels before climate impacts become severe",
                                  "The average temperature of the Philippines during summer",
                                  "The temperature at which forests stop growing" } },
            new QuizDef { Id = "S1_PRE_Q2", TopicId = "forests", Answer = 'B',
                Question = "Baguio is famous for its cool climate and pine forests. What role do forests like these play in fighting climate change?",
                Choices = new[] { "They make the air colder by releasing ice particles",
                                  "They absorb carbon dioxide (CO2) from the atmosphere and store it",
                                  "They reflect sunlight back into space permanently",
                                  "They increase rainfall only in mountainous areas" } },
            new QuizDef { Id = "S1_PRE_Q3", TopicId = "game", Answer = 'C',
                Question = "In Thermo-Tactics, the temperature bar starts at 1.0°C. What happens if it reaches 1.6°C?",
                Choices = new[] { "The player earns bonus Oxygen Points", "The game pauses automatically",
                                  "The player loses the game", "The enemies retreat" } },
            new QuizDef { Id = "S1_PRE_Q4", TopicId = "climate", Answer = 'C',
                Question = "Which of the following is a renewable energy source commonly used in the Philippines?",
                Choices = new[] { "Coal", "Diesel", "Geothermal energy", "Gasoline" } },
            new QuizDef { Id = "S1_PRE_Q5", TopicId = "climate", Answer = 'B',
                Question = "Why is keeping global warming below 1.5°C important for countries like the Philippines?",
                Choices = new[] { "It guarantees no typhoons will ever occur",
                                  "The Philippines is highly vulnerable to climate impacts like stronger typhoons, floods, and heat waves",
                                  "It only matters for cold countries",
                                  "It has no effect on island nations" } },
        };

        private static readonly QuizDef[] BaguioPost =
        {
            new QuizDef { Id = "S1_POST_Q6", TopicId = "forests", Answer = 'A',
                Question = "You just defended the Baguio Forest. Why are forests considered \"carbon sinks\"?",
                Choices = new[] { "They store carbon in their trunks, roots, and soil, keeping it out of the atmosphere",
                                  "They sink into the ground during earthquakes",
                                  "They absorb all the water in the environment",
                                  "They convert carbon into oxygen without any limits" } },
            new QuizDef { Id = "S1_POST_Q7", TopicId = "game", Answer = 'B',
                Question = "In the game, enemy attacks on undefended lanes raise the temperature by +0.1°C. What real-world action does this represent?",
                Choices = new[] { "Planting trees",
                                  "Fossil fuel emissions and environmental damage that worsen climate change",
                                  "Recycling plastic bottles", "Building more forests" } },
            new QuizDef { Id = "S1_POST_Q8", TopicId = "climate", Answer = 'C',
                Question = "What is the main cause of the rising global temperature?",
                Choices = new[] { "The Earth moving closer to the Sun", "Volcanic eruptions",
                                  "Human activities that release greenhouse gases like CO2 and methane",
                                  "The moon's gravitational pull" } },
            new QuizDef { Id = "S1_POST_Q9", TopicId = "forests", Answer = 'B',
                Question = "The Philippine Eagle is a species found in forests like those in Mindanao and Sierra Madre. How does protecting forests help wildlife AND the climate?",
                Choices = new[] { "It only helps animals, not the climate", "Forests provide habitat while also absorbing CO2",
                                  "Animals produce oxygen that cools the planet", "Forests have no connection to wildlife" } },
            new QuizDef { Id = "S1_POST_Q10", TopicId = "game", Answer = 'B',
                Question = "In Thermo-Tactics, you deploy units to defend lanes. If you leave a lane undefended, what happens in the game?",
                Choices = new[] { "You gain bonus Oxygen Points", "The enemy attacks and increases the temperature",
                                  "The lane becomes a forest", "Nothing happens" } },
        };

        private struct FactDef
        {
            public string Id, TopicId, Title, Body; public int Level; public PopupTrigger Trigger; public int Wave;
        }

        // The reviewer: facts placed through Levels 1–8, written from the answers of the post-quiz (and pre-quiz).
        private static readonly FactDef[] BaguioFacts =
        {
            new FactDef { Id = "F01", TopicId = "game", Level = 1, Trigger = PopupTrigger.LevelStart,
                Title = "Every open lane heats the planet",
                Body = "When an enemy hits a lane you left undefended, the temperature rises by +0.10 °C. The enemies stand for fossil fuel emissions and environmental damage, like illegal logging, mining and smog, that make climate change worse." },
            new FactDef { Id = "F02", TopicId = "climate", Level = 1, Trigger = PopupTrigger.AfterWave, Wave = 1,
                Title = "What is warming the planet?",
                Body = "The main cause of rising global temperature is human activity. Burning coal, oil and gas and clearing forests release greenhouse gases such as carbon dioxide (CO2) and methane, which trap heat in the atmosphere." },
            new FactDef { Id = "F03", TopicId = "forests", Level = 2, Trigger = PopupTrigger.LevelStart,
                Title = "Forests are carbon sinks",
                Body = "Trees take in CO2 as they grow and store the carbon in their trunks, branches, roots and the soil beneath them. Baguio's pine forests keep that carbon out of the atmosphere, as long as they are left standing." },
            new FactDef { Id = "F04", TopicId = "climate", Level = 3, Trigger = PopupTrigger.LevelStart,
                Title = "The 1.5 °C target",
                Body = "Under the Paris Agreement, countries aim to limit global warming to 1.5 °C above pre-industrial levels. Beyond that, impacts such as stronger typhoons, floods and heat waves become far more severe. The temperature bar in this game tracks that limit." },
            new FactDef { Id = "F05", TopicId = "game", Level = 4, Trigger = PopupTrigger.LevelStart,
                Title = "Your units are climate action",
                Body = "Each unit you deploy stands for protecting nature and cutting emissions. A defended lane stops the damage before it heats the planet; an open lane lets the enemy through and the temperature climbs." },
            new FactDef { Id = "F06", TopicId = "forests", Level = 6, Trigger = PopupTrigger.LevelStart,
                Title = "Homes for wildlife",
                Body = "Forests shelter wildlife such as the Philippine Eagle, which lives in the forests of Mindanao and the Sierra Madre. Protecting a forest protects its animals and keeps its carbon stored: one action helps wildlife and the climate." },
            new FactDef { Id = "F07", TopicId = "climate", Level = 7, Trigger = PopupTrigger.LevelStart,
                Title = "Clean power from the ground",
                Body = "The Philippines is one of the world's largest producers of geothermal energy, a renewable source that uses the Earth's natural heat. Unlike coal, diesel or gasoline, it releases very little CO2." },
            new FactDef { Id = "F08", TopicId = "climate", Level = 8, Trigger = PopupTrigger.LevelStart,
                Title = "Why it matters here",
                Body = "The Philippines is one of the countries most at risk from climate change, facing stronger typhoons, floods, rising seas and heat waves. Keeping warming below 1.5 °C protects Filipino communities." },
        };

        private static QuizQuestion ToQuestion(QuizDef q) => new QuizQuestion
        {
            questionId = q.Id, topicId = q.TopicId, text = q.Question, choices = (string[])q.Choices.Clone(), correctIndex = q.Answer - 'A',
        };

        private static (QuizData quiz, PopupData[] facts) BuildEducation()
        {
            EnsureFolder(DataDir + "/Quizzes");
            EnsureFolder(DataDir + "/Facts");
            var quiz = LoadOrCreate<QuizData>(DataDir + "/Quizzes/Baguio_Quiz.asset");
            quiz.quizId = "Q1"; quiz.stageId = 1;
            quiz.topics = System.Array.ConvertAll(BaguioTopics, t => new QuizTopic { id = t.id, name = t.name });
            quiz.preQuestions = System.Array.ConvertAll(BaguioPre, ToQuestion);
            quiz.postQuestions = System.Array.ConvertAll(BaguioPost, ToQuestion);
            EditorUtility.SetDirty(quiz);

            var facts = new PopupData[BaguioFacts.Length];
            for (int i = 0; i < BaguioFacts.Length; i++)
            {
                FactDef d = BaguioFacts[i];
                var f = LoadOrCreate<PopupData>($"{DataDir}/Facts/Baguio_{d.Id}.asset");
                f.factId = "S1_" + d.Id; f.stageId = 1; f.topicId = d.TopicId; f.title = d.Title; f.body = d.Body;
                f.levelNumber = d.Level; f.trigger = d.Trigger; f.waveNumber = Mathf.Max(1, d.Wave);
                EditorUtility.SetDirty(f);
                facts[i] = f;
            }

            // Checks: 5 + 5 questions with 4 choices, known topics, and every topic reviewed by a fact before the boss level.
            foreach (string type in new[] { QuizManager.Pre, QuizManager.Post })
            {
                if (quiz.Count(type) != QuizData.QuestionCount)
                    Debug.LogError($"[Thermo-Tactics] The Baguio {type}-quiz has {quiz.Count(type)} questions; it needs {QuizData.QuestionCount}.");
                foreach (QuizQuestion q in quiz.Questions(type))
                {
                    if (q.choices.Length != QuizData.ChoiceCount || q.correctIndex < 0 || q.correctIndex >= q.choices.Length)
                        Debug.LogError($"[Thermo-Tactics] Quiz question {q.questionId} needs {QuizData.ChoiceCount} choices and a valid answer.");
                    if (quiz.TopicIndex(q.topicId) == 99)
                        Debug.LogError($"[Thermo-Tactics] Quiz question {q.questionId} has an unknown topic \"{q.topicId}\".");
                }
            }
            foreach (QuizTopic t in quiz.topics)
                if (!System.Array.Exists(facts, f => f.topicId == t.id && f.levelNumber < Levels.Length))
                    Debug.LogError($"[Thermo-Tactics] Quiz topic \"{t.name}\" has no climate fact in Levels 1–{Levels.Length - 1}.");
            return (quiz, facts);
        }

        private static StageData BuildData(Dictionary<string, AnimatorController> controllers)
        {
            EnsureFolder(DataDir + "/Units");
            EnsureFolder(DataDir + "/Waves");
            EnsureFolder(DataDir + "/Levels");
            var byFolder = new Dictionary<string, UnitData>();
            var catalog = new List<UnitData>();
            foreach (var u in Units)
            {
                var data = LoadOrCreate<UnitData>($"{DataDir}/Units/{u.Id}_{u.Folder}.asset");
                data.unitId = u.Id;
                data.displayName = u.Display;
                data.faction = u.Side;
                data.isElite = false;
                data.opCost = u.Cost;
                data.damage = u.Damage;
                data.maxHp = u.Hp;
                data.tint = Color.white;
                data.card = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUnits}/{u.Folder}/card.png");
                data.idlePose = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUnits}/{u.Folder}/idle_00.png");
                data.animator = controllers[u.Folder];
                data.hitDelay = 6f / 14f;
                data.attackDuration = 12f / 14f;
                EditorUtility.SetDirty(data);
                byFolder[u.Folder] = data;
                catalog.Add(data);
            }

            var elites = new Dictionary<string, UnitData>();
            foreach (var e in Elites)
            {
                UnitData src = byFolder[e.baseFolder];
                var data = LoadOrCreate<UnitData>($"{DataDir}/Units/{e.id}_{e.display.Replace(" ", "")}.asset");
                data.unitId = e.id; data.displayName = e.display; data.faction = Faction.Enemy; data.isElite = true;
                data.opCost = 0; data.maxHp = e.hp; data.damage = e.dmg; data.tint = EliteTint;
                data.card = src.card; data.idlePose = src.idlePose; data.animator = src.animator;
                data.hitDelay = src.hitDelay; data.attackDuration = src.attackDuration;
                EditorUtility.SetDirty(data);
                elites[e.baseFolder] = data;
                catalog.Add(data);
            }

            var boss = LoadOrCreate<BossData>($"{DataDir}/Units/B01_ClearcutBehemoth.asset");
            boss.unitId = "B01"; boss.displayName = "Clearcut Behemoth"; boss.faction = Faction.Enemy;
            boss.opCost = 0; boss.maxHp = 300; boss.damage = 8;
            boss.enragedCleaveDamage = 10; boss.slamDamage = 7; boss.slamEvery = 3; boss.enragedSlamEvery = 2;
            boss.enrageAtFraction = 0.4f; boss.cleaveRadius = 1;
            boss.boardAnchor = new Vector2(4.5f, 3.0f); boss.worldScale = 1.25f; boss.tint = Color.white;
            boss.card = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtBosses}/{BossFolder}/card.png");
            boss.idlePose = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtBosses}/{BossFolder}/idle_00.png");
            boss.animator = controllers[BossFolder];
            boss.hitDelay = 6f / 14f; boss.attackDuration = 12f / 14f;
            boss.description = "Stage 1 boss: felled pine logs, landslide mud and stone, with a chainsaw arm (Cleave) and a boulder fist (Landslide Slam).";
            EditorUtility.SetDirty(boss);
            catalog.Add(boss);

            UnitData Enemy(string key)
            {
                if (key == "boss") return boss;
                if (key.StartsWith("elite:")) return elites[key.Substring(6)];
                return byFolder[key];
            }

            // Level 1 keeps its original asset IDs (Baguio_Wave1–3 become Baguio_L1_Wave1–3).
            for (int w = 1; w <= 3; w++)
                MoveIfExists($"{DataDir}/Waves/Baguio_Wave{w}.asset", $"{DataDir}/Waves/Baguio_L1_Wave{w}.asset");
            MoveIfExists($"{DataDir}/Baguio_Level1.asset", $"{DataDir}/Levels/Baguio_Level1.asset");

            UnitData[] roster = { byFolder["MangroveGuardian"], byFolder["CoralDefender"], byFolder["WindCaller"], byFolder["GeothermalSentinel"], byFolder["ReefSentinel"] };
            var levels = new LevelData[Levels.Length];
            for (int i = 0; i < Levels.Length; i++)
            {
                LevelDef d = Levels[i];
                int n = i + 1;
                var waves = new WaveData[d.Waves.Length];
                for (int w = 0; w < d.Waves.Length; w++)
                {
                    var wave = LoadOrCreate<WaveData>($"{DataDir}/Waves/Baguio_L{n}_Wave{w + 1}.asset");
                    wave.waveNumber = w + 1;
                    wave.opAllowance = d.Waves[w].op;
                    wave.enemies = System.Array.ConvertAll(d.Waves[w].enemies, Enemy);
                    wave.ignorePenalty = wave.HasBoss;
                    EditorUtility.SetDirty(wave);
                    waves[w] = wave;
                }
                var level = LoadOrCreate<LevelData>($"{DataDir}/Levels/Baguio_Level{n}.asset");
                level.levelName = $"Stage 1 · Baguio — {d.Name}";
                level.stageId = 1; level.levelNumber = n;
                level.locationName = d.Name; level.pathCaption = d.Caption; level.subtitle = d.Subtitle; level.note = d.Note;
                level.threat = d.Threat; level.isEliteSpike = d.Spike; level.isBossLevel = d.Boss;
                level.startingTemperature = 1.0f;
                level.lossThreshold = 1.5f;
                level.heatPerUndefendedHit = 0.10f;
                level.coolingPerEmptyLaneAttack = 0.05f;
                level.opPenaltyByTurn = new[] { 0, 0, 1, 2, 4, 8 };
                level.enemyHpScale = d.Scale;
                level.allyRoster = roster;
                level.waves = waves;
                EditorUtility.SetDirty(level);
                levels[i] = level;
            }

            var stage = LoadOrCreate<StageData>(StagePath);
            stage.stageId = 1;
            stage.stageName = "Baguio Forest";
            stage.region = "Cordillera mountains · pine forest, fog, Kennon Road";
            stage.threats = "illegal logging, mining, smog";
            stage.difficulty = "Easy";
            stage.thumbnail = Ui("stage1_thumb");
            stage.levels = levels;
            stage.unitCatalog = catalog.ToArray();
            (stage.quiz, stage.facts) = BuildEducation();
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<StageData>(StagePath);
        }

        private static void MoveIfExists(string from, string to)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(from) == null || AssetDatabase.LoadAssetAtPath<Object>(to) != null) return;
            EnsureFolder(Path.GetDirectoryName(to).Replace('\\', '/'));
            string err = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[Thermo-Tactics] " + err);
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
        // 4. Battle scene (plays every Baguio level; the level comes from GameSession)
        // =====================================================================
        private static void BuildScene(StageData stage)
        {
            EnsureFolder(Root + "/Scenes");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldScenePath) != null) AssetDatabase.DeleteAsset(OldScenePath);
            AssetDatabase.DeleteAsset(ScenePath);
            var scene = AssetDatabase.CopyAsset(TemplateScene, ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LoadFonts();

            // ---- camera: the 21 × 9 board fills the space between the HUD header (84 px) and footer (87 px) ----
            Camera cam = Camera.main != null ? Camera.main : new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 4.5f * 720f / 549f;
            cam.transform.position = new Vector3(0f, -0.02f, -10f);
            cam.backgroundColor = Night;
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (cam.GetComponent<Physics2DRaycaster>() == null) cam.gameObject.AddComponent<Physics2DRaycaster>();
            var battleCam = cam.GetComponent<BattleCamera>();
            if (battleCam == null) battleCam = cam.gameObject.AddComponent<BattleCamera>();

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
            var popups = sys.AddComponent<PopupManager>();

            // ---- UI ----
            var refs = new Dictionary<string, Object>();
            var cards = new List<DeployCard>();
            Canvas canvas = BuildBattleUI(refs, cards);
            var ui = canvas.gameObject.AddComponent<UIManager>();
            EducationUiBuilder.BuildPopup(canvas.transform, popups);
            Ref(popups, "_database", db);

            // ---- wiring ----
            stage = AssetDatabase.LoadAssetAtPath<StageData>(StagePath);   // reload: a stale instance would serialize as null
            Ref(gm, "_defaultStage", stage); Ref(gm, "_turns", turns); Ref(gm, "_waves", waves); Ref(gm, "_oxygen", oxygen);
            Ref(gm, "_temperature", temp); Ref(gm, "_grid", grid); Ref(gm, "_lanes", lanes); Ref(gm, "_deploy", deploy);
            Ref(gm, "_factory", factory); Ref(gm, "_database", db); Ref(gm, "_ui", ui); Ref(gm, "_fog", fog); Ref(gm, "_camera", battleCam);
            Ref(gm, "_popups", popups);
            Ref(turns, "_grid", grid); Ref(turns, "_waves", waves); Ref(turns, "_oxygen", oxygen);
            Ref(turns, "_temperature", temp); Ref(turns, "_lanes", lanes); Ref(turns, "_ui", ui); Ref(turns, "_camera", battleCam);
            Ref(waves, "_grid", grid); Ref(waves, "_factory", factory);
            RefArray(grid, "_allySlots", allySlots); RefArray(grid, "_enemySlots", enemySlots);
            Ref(lanes, "_temperature", temp); Ref(lanes, "_ui", ui);
            Ref(deploy, "_grid", grid); Ref(deploy, "_oxygen", oxygen); Ref(deploy, "_factory", factory); Ref(deploy, "_ui", ui);
            Ref(factory, "_shadowSprite", Sprite(ArtEnv + "/unit_shadow.png")); Ref(factory, "_pixelSprite", Sprite(ArtEnv + "/pixel.png"));

            foreach (var kv in refs) if (kv.Value != null) Ref(ui, kv.Key, kv.Value);
            RefArray(ui, "_cards", cards.ToArray());
            RefArray(ui, "_endStars", _endArrays.stars);
            RefArray(ui, "_endSecondary", _endArrays.secondary);
            RefArray(ui, "_endSecondaryLabels", _endArrays.labels);
            RefArray(ui, "_endSecondaryIcons", _endArrays.icons);
            RefArray(ui, "_stageLevelStars", _stageStars);
            Ref(ui, "_temperature", temp); Ref(ui, "_oxygen", oxygen); Ref(ui, "_waves", waves); Ref(ui, "_grid", grid);
            Ref(ui, "_deploy", deploy); Ref(ui, "_turns", turns); Ref(ui, "_database", db); Ref(ui, "_font", BodyBold);

            NewEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        public static void NewEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Main Menu first (build index 0), then the battle scene.</summary>
        private static void SetBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuSceneBuilder.ScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
            };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath && s.path != MenuSceneBuilder.ScenePath && s.path != OldScenePath)
                    list.Add(new EditorBuildSettingsScene(s.path, false));
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
            var danger = NewSprite("BossWarning", marker.transform, Sprite(ArtEnv + "/slot_highlight_attack.png"), -498);
            danger.transform.localPosition = Vector3.zero;
            danger.color = new Color(1f, 0.35f, 0.28f, 0.95f);
            danger.enabled = false;
            var col = marker.gameObject.AddComponent<PolygonCollider2D>();
            col.points = new[] { new Vector2(0f, 0.45f), new Vector2(0.9f, 0f), new Vector2(0f, -0.45f), new Vector2(-0.9f, 0f) };
            var slot = marker.gameObject.AddComponent<BoardSlot>();
            slot.Configure(lane, enemy, hl);
            RefInt(slot, "_lane", lane); RefBool(slot, "_isEnemySide", enemy); Ref(slot, "_highlight", hl); Ref(slot, "_danger", danger);
            return slot;
        }

        // =====================================================================
        // Battle UI (design board "Level 10 boss wave", 1280 × 720)
        // =====================================================================
        private static Canvas BuildBattleUI(Dictionary<string, Object> refs, List<DeployCard> cards)
        {
            Canvas canvas = NewCanvas("UI", 10);
            Transform c = canvas.transform;

            var hud = NewRect("HUD", c); Stretch(hud);
            refs["_hud"] = hud.gameObject;

            // ---------------- header (84 px, red woven line under it) ----------------
            var header = Img("Header", hud, Night);
            var hrt = header.rectTransform;
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1); hrt.pivot = new Vector2(0.5f, 1); hrt.sizeDelta = new Vector2(0, 84); hrt.anchoredPosition = Vector2.zero;
            var hline = Img("RedLine", header.transform, Red);
            hline.rectTransform.anchorMin = new Vector2(0, 0); hline.rectTransform.anchorMax = new Vector2(1, 0); hline.rectTransform.pivot = new Vector2(0.5f, 0);
            hline.rectTransform.sizeDelta = new Vector2(0, 3); hline.rectTransform.anchoredPosition = Vector2.zero;

            refs["_levelTitle"] = TxtAt("LevelTitle", header.transform, "LEVEL 1 · KENNON ROAD", BodyBold, 20, Cream, 20, 14, 330, 26, TextAnchor.MiddleLeft);
            refs["_waveValue"] = TxtAt("WaveValue", header.transform, "WAVE 1 / 3 · TURN 1", BodyBold, 14, GoldLight, 20, 42, 320, 22, TextAnchor.MiddleLeft);

            // temperature (stretches between the title and the Oxygen box)
            var tGroup = NewRect("Temperature", header.transform);
            tGroup.anchorMin = new Vector2(0, 0); tGroup.anchorMax = new Vector2(1, 1); tGroup.pivot = new Vector2(0.5f, 0.5f);
            tGroup.offsetMin = new Vector2(360, 18); tGroup.offsetMax = new Vector2(-286, -18);
            var tLabel = Txt("Label", tGroup, "Global temperature", BodyBold, 14, Cream, TextAnchor.UpperLeft);
            tLabel.rectTransform.anchorMin = new Vector2(0, 1); tLabel.rectTransform.anchorMax = new Vector2(1, 1); tLabel.rectTransform.pivot = new Vector2(0.5f, 1);
            tLabel.rectTransform.sizeDelta = new Vector2(0, 20); tLabel.rectTransform.anchoredPosition = Vector2.zero;
            var tValue = Txt("TemperatureValue", tGroup, "1.00 °C / limit 1.5 °C", Body, 14, Cream, TextAnchor.UpperRight);
            tValue.rectTransform.anchorMin = new Vector2(0, 1); tValue.rectTransform.anchorMax = new Vector2(1, 1); tValue.rectTransform.pivot = new Vector2(0.5f, 1);
            tValue.rectTransform.sizeDelta = new Vector2(0, 20); tValue.rectTransform.anchoredPosition = Vector2.zero;
            var tBack = Img("TemperatureBar", tGroup, Hex("#22332A"));
            tBack.rectTransform.anchorMin = new Vector2(0, 0); tBack.rectTransform.anchorMax = new Vector2(1, 0); tBack.rectTransform.pivot = new Vector2(0.5f, 0);
            tBack.rectTransform.sizeDelta = new Vector2(0, 18); tBack.rectTransform.anchoredPosition = new Vector2(0, 2);
            Border(tBack.transform, Color.black, "ui_frame2");
            var tFill = Img("Fill", tBack.transform, Hex("#D9532F"));
            Stretch(tFill.rectTransform, 2);
            tFill.type = Image.Type.Filled; tFill.fillMethod = Image.FillMethod.Horizontal; tFill.fillOrigin = 0; tFill.fillAmount = 0.05f;
            tFill.transform.SetSiblingIndex(0);
            foreach (float at in new[] { 0.4f, 0.8f })   // 1.2 °C and 1.4 °C marks
            {
                var tick = Img("Tick", tBack.transform, Cream);
                tick.rectTransform.anchorMin = new Vector2(at, 0); tick.rectTransform.anchorMax = new Vector2(at, 1); tick.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                tick.rectTransform.sizeDelta = new Vector2(2, 8); tick.rectTransform.anchoredPosition = Vector2.zero;
            }
            refs["_temperatureFill"] = tFill; refs["_temperatureBack"] = tBack; refs["_temperatureValue"] = tValue;

            // Oxygen box and pause button (right edge)
            var ox = AtRight(Img("Oxygen", header.transform, MossDim), 96, 14, 170, 56);
            Border(ox.transform, Moss, "ui_frame2");
            Icon("Icon", ox.transform, "icon_oxygen", Mint, 12, 16, 24);
            refs["_oxygenValue"] = TxtAt("OxygenValue", ox.transform, "5 / 5", BodyBold, 22, Cream, 46, 4, 120, 28, TextAnchor.MiddleLeft);
            refs["_oxygenSub"] = TxtAt("OxygenSub", ox.transform, "Oxygen", Body, 12, CreamDim, 46, 30, 124, 18, TextAnchor.MiddleLeft);
            var pause = AtRight(Img("PauseButton", header.transform, Clear, null, true), 20, 16, 52, 52);
            Border(pause.transform, Slate);
            Icon("Icon", pause.transform, "icon_pause", Cream, 16, 16, 20);
            var pb = pause.gameObject.AddComponent<Button>(); pb.targetGraphic = pause;
            refs["_pauseButton"] = pb;

            // ---------------- footer (87 px, green line on top) ----------------
            var footer = Img("Footer", hud, Night);
            var frt = footer.rectTransform;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 0); frt.pivot = new Vector2(0.5f, 0); frt.sizeDelta = new Vector2(0, 87); frt.anchoredPosition = Vector2.zero;
            var fline = Img("GreenLine", footer.transform, Moss);
            fline.rectTransform.anchorMin = new Vector2(0, 1); fline.rectTransform.anchorMax = new Vector2(1, 1); fline.rectTransform.pivot = new Vector2(0.5f, 1);
            fline.rectTransform.sizeDelta = new Vector2(0, 3); fline.rectTransform.anchoredPosition = Vector2.zero;
            for (int i = 0; i < LevelData.MaxRosterTypes; i++) cards.Add(Card(footer.transform, i));

            var hint = Txt("Hint", footer.transform, "", Body, 14, CreamDim, TextAnchor.MiddleLeft);
            hint.rectTransform.anchorMin = new Vector2(0, 0); hint.rectTransform.anchorMax = new Vector2(1, 1); hint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hint.rectTransform.offsetMin = new Vector2(450, 10); hint.rectTransform.offsetMax = new Vector2(-250, -8);
            refs["_hint"] = hint;

            var endShadow = AtRight(Img("EndTurn_Shadow", footer.transform, Color.black), 16, 21, 210, 60);
            var endTurn = Btn("EndTurnButton", footer.transform, "END TURN", Red, Hex("#FFF6E6"), Ink, 0, 0, 210, 60, 22, 0f);
            AtRight(endTurn, 20, 17, 210, 60);
            refs["_endTurnButton"] = endTurn;

            var floating = NewRect("FloatingLayer", hud); Stretch(floating);
            refs["_floatingLayer"] = floating;

            var announce = Txt("Announcement", hud, "", BodyBold, 22, Cream, TextAnchor.MiddleCenter);
            var art = announce.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 1); art.pivot = new Vector2(0.5f, 1); art.sizeDelta = new Vector2(900, 60); art.anchoredPosition = new Vector2(120, -230);
            var aol = announce.gameObject.AddComponent<Outline>(); aol.effectColor = new Color(0, 0, 0, 0.95f); aol.effectDistance = new Vector2(2, -2);
            announce.gameObject.SetActive(false);
            refs["_announcement"] = announce;

            // ---------------- lane caption (End Turn, lane by lane) ----------------
            var lane = Img("LaneCaption", hud, new Color(0.055f, 0.08f, 0.066f, 0.93f));
            var lrt = lane.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 1); lrt.pivot = new Vector2(0.5f, 1);
            lrt.sizeDelta = new Vector2(760, 64); lrt.anchoredPosition = new Vector2(0, -96);
            Border(lane.transform, Moss, "ui_frame2");
            var accent = At(Img("Accent", lane.transform, Mint), 0, 0, 8, 64);
            refs["_laneAccent"] = accent;
            refs["_laneTitle"] = TxtAt("Title", lane.transform, "LANE 1", BodyBold, 20, Cream, 24, 6, 700, 26, TextAnchor.MiddleLeft);
            refs["_laneDetail"] = TxtAt("Detail", lane.transform, "", Body, 15, Cream, 24, 34, 720, 24, TextAnchor.MiddleLeft);
            refs["_laneCaption"] = lane.gameObject;

            // ---------------- boss panel ----------------
            var boss = At(Img("BossPanel", hud, new Color(0.055f, 0.04f, 0.03f, 0.92f)), 20, 96, 470, 124);
            Border(boss.transform, Red);
            Weave(boss.transform, 0, -3, 470, 6);
            refs["_bossPanel"] = boss.gameObject;
            refs["_bossName"] = TxtAt("Name", boss.transform, "CLEARCUT BEHEMOTH", BodyBold, 20, Cream, 14, 10, 330, 28, TextAnchor.MiddleLeft);
            refs["_bossHp"] = TxtAt("Hp", boss.transform, "300 / 300", BodyBold, 15, Cream, 330, 10, 126, 28, TextAnchor.MiddleRight);
            Bar("HpBar", boss.transform, Hex("#2A1512"), RedSoft, 14, 42, 442, 14, out Image bossFill);
            var enrage = At(Img("EnrageTick", boss.transform, Gold), 14 + 442 * 0.4f - 1, 37, 3, 24);
            refs["_bossHpFill"] = bossFill;
            refs["_bossNext"] = TxtAt("Next", boss.transform, "", Body, 13, Cream, 14, 62, 442, 18, TextAnchor.MiddleLeft);
            refs["_bossSlam"] = TxtAt("Slam", boss.transform, "", Body, 13, Cream, 14, 80, 442, 18, TextAnchor.MiddleLeft);
            refs["_bossNote"] = TxtAt("Note", boss.transform, "", Body, 12, CreamDim, 14, 100, 442, 18, TextAnchor.MiddleLeft);

            // ---------------- pause ----------------
            var pausePanel = Img("PausePanel", c, new Color(0.024f, 0.04f, 0.03f, 0.78f), null, true); Stretch(pausePanel.rectTransform);
            var pf = Frame("Frame", pausePanel.transform);
            AddPanelFx(pausePanel.gameObject, pf);
            var pd = Box("Dialog", pf, Panel, Moss, 380, 150, 520, 420, 8f);
            Weave(pd.transform, 0, 0, 520);
            TxtAt("Title", pd.transform, "PAUSED", Pixel, 32, Cream, 28, 40, 460, 40, TextAnchor.MiddleLeft);
            refs["_pauseInfo"] = TxtAt("Info", pd.transform, "", Body, 15, CreamDim, 28, 86, 464, 60, TextAnchor.UpperLeft);
            refs["_resumeButton"] = Btn("ResumeButton", pd.transform, "RESUME", Gold, Ink, Ink, 28, 156, 464, 54, 22);
            refs["_restartButton"] = Btn("RestartButton", pd.transform, "RESTART LEVEL", Clear, Cream, Slate, 28, 224, 464, 50, 19, 0f);
            refs["_pauseLevelSelectButton"] = Btn("LevelSelectButton", pd.transform, "LEVEL SELECT", Clear, Cream, Slate, 28, 286, 226, 50, 19, 0f);
            refs["_pauseMenuButton"] = Btn("MainMenuButton", pd.transform, "MAIN MENU", Clear, Cream, Slate, 266, 286, 226, 50, 19, 0f);
            TxtAt("Saved", pd.transform, "Leaving keeps your saved game. Continue on the Main Menu returns to this turn.", Body, 13, CreamDim, 28, 350, 464, 40);
            refs["_pausePanel"] = pausePanel.gameObject;

            // ---------------- end: level complete / level failed ----------------
            var end = Img("EndPanel", c, new Color(0.024f, 0.04f, 0.03f, 0.8f), null, true); Stretch(end.rectTransform);
            var ef = Frame("Frame", end.transform);
            AddPanelFx(end.gameObject, ef);
            var ed = Box("Dialog", ef, Panel, Clear, 250, 92, 780, 540, 8f);
            refs["_endBorder"] = Border(ed.transform, Gold);
            Weave(ed.transform, 0, 0, 780);
            refs["_endTitle"] = TxtAt("Title", ed.transform, "LEVEL COMPLETE", Pixel, 50, Gold, 0, 34, 780, 62, TextAnchor.MiddleCenter);
            var stars = new Image[3];
            for (int s3 = 0; s3 < 3; s3++)
            {
                float size = s3 == 1 ? 68 : 56;
                stars[s3] = Icon($"Star_{s3 + 1}", ed.transform, "icon_star", Gold, 390 - 34 + (s3 - 1) * 84 + (68 - size) / 2, 104 + (s3 == 1 ? 0 : 10), size);
                stars[s3].rectTransform.pivot = new Vector2(0.5f, 0.5f);
                stars[s3].rectTransform.anchoredPosition += new Vector2(size / 2, -size / 2);
            }
            var badge = At(Img("NewBest", ed.transform, Red), 560, 120, 130, 30);
            Border(badge.transform, Ink, "ui_frame2");
            var bt = Txt("Text", badge.transform, "NEW BEST!", BodyBold, 15, Hex("#FFF6E6"), TextAnchor.MiddleCenter); Stretch(bt.rectTransform);
            refs["_endNewBest"] = badge.gameObject;
            refs["_endBody"] = TxtAt("Body", ed.transform, "", Body, 17, Cream, 50, 186, 680, 110, TextAnchor.UpperCenter);
            refs["_endStarHint"] = TxtAt("StarHint", ed.transform, "", BodyBold, 14, GoldLight, 50, 300, 680, 22, TextAnchor.MiddleCenter);

            var primary = Btn("PrimaryButton", ed.transform, "", Gold, Ink, Ink, 50, 340, 680, 62, 24);
            refs["_endPrimary"] = primary;
            refs["_endPrimaryIcon"] = Icon("Icon", primary.transform, "icon_next", Ink, 240, 18, 26);
            refs["_endPrimaryLabel"] = TxtAt("Label", primary.transform, "NEXT LEVEL", Pixel, 26, Ink, 276, 0, 300, 62, TextAnchor.MiddleLeft);
            var secondary = new Button[3]; var secLabels = new Text[3]; var secIcons = new Image[3];
            for (int b = 0; b < 3; b++)
            {
                var sb = Btn($"SecondaryButton_{b + 1}", ed.transform, "", new Color(0.078f, 0.11f, 0.09f, 1f), Cream, Slate, 50 + b * 232, 420, 216, 54, 18, 0f);
                secIcons[b] = Icon("Icon", sb.transform, "icon_restart", Cream, 18, 15, 24);
                secLabels[b] = TxtAt("Label", sb.transform, "RETRY", Pixel, 18, Cream, 52, 0, 160, 54, TextAnchor.MiddleLeft);
                secondary[b] = sb;
            }
            refs["_endResultsButton"] = Btn("ResultsButton", ed.transform, "See all results", Clear, CreamDim, Clear, 290, 486, 200, 30, 14, 0f, Body);
            refs["_endPanel"] = end.gameObject;
            _endArrays = (stars, secondary, secLabels, secIcons);

            // ---------------- stage complete ----------------
            var stagePanel = Img("StageCompletePanel", c, new Color(0.024f, 0.04f, 0.03f, 0.86f), null, true); Stretch(stagePanel.rectTransform);
            var sf = Frame("Frame", stagePanel.transform);
            AddPanelFx(stagePanel.gameObject, sf);
            var sd = Box("Dialog", sf, Panel, Gold, 170, 70, 940, 580, 8f);
            Weave(sd.transform, 0, 0, 940);
            TxtAt("Eyebrow", sd.transform, "STAGE COMPLETE", BodyBold, 16, GoldLight, 40, 40, 860, 22, TextAnchor.MiddleCenter);
            refs["_stageTitle"] = TxtAt("Title", sd.transform, "BAGUIO FOREST CLEARED", Pixel, 44, Gold, 0, 64, 940, 56, TextAnchor.MiddleCenter);
            var thumbFrame = At(Img("ThumbFrame", sd.transform, Gold), 40, 140, 344, 230);
            At(Img("Thumb", thumbFrame.transform, Color.white, Ui("stage1_thumb")), 4, 4, 336, 222).preserveAspect = false;
            var lvlStars = new Image[3];
            TxtAt("BossLabel", sd.transform, "LEVEL 10 · BOSS", BodyBold, 13, CreamDim, 40, 384, 344, 18, TextAnchor.MiddleCenter);
            for (int s3 = 0; s3 < 3; s3++)
            {
                lvlStars[s3] = Icon($"LevelStar_{s3 + 1}", sd.transform, "icon_star", Gold, 160 + s3 * 52, 432, 44);
                lvlStars[s3].rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            refs["_stageStats"] = TxtAt("Stats", sd.transform, "", Body, 18, Cream, 412, 140, 488, 110);
            TxtAt("StarsLabel", sd.transform, "STAGE STARS", BodyBold, 14, CreamDim, 412, 262, 240, 20, TextAnchor.MiddleLeft);
            Icon("StarIcon", sd.transform, "icon_star", Gold, 412, 288, 34);
            refs["_stageStarCount"] = TxtAt("StarCount", sd.transform, "0 / 30", Pixel, 30, Cream, 454, 284, 200, 40, TextAnchor.MiddleLeft);
            Bar("StarBar", sd.transform, Hex("#2A3A30"), Gold, 412, 332, 488, 14, out Image starFill);
            starFill.type = Image.Type.Filled; starFill.fillMethod = Image.FillMethod.Horizontal; starFill.fillAmount = 0.5f;
            refs["_stageStarFill"] = starFill;
            var next = At(Img("Next", sd.transform, new Color(0.184f, 0.365f, 0.27f, 0.25f)), 412, 360, 488, 92);
            Border(next.transform, Moss, "ui_frame2");
            Icon("Icon", next.transform, "icon_lock", Mint, 14, 14, 22);
            refs["_stageNext"] = TxtAt("Text", next.transform, "", Body, 15, Cream, 46, 10, 430, 74);

            refs["_stageSelectButton"] = StageBtn(sd.transform, "STAGE SELECT", "icon_map", Gold, Ink, 40, 486, 300, 60, 22, 4f);
            refs["_stageLevelSelectButton"] = StageBtn(sd.transform, "LEVEL SELECT", "icon_list", new Color(0.078f, 0.11f, 0.09f, 1f), Cream, 356, 486, 260, 60, 19, 0f);
            refs["_stageRetryButton"] = StageBtn(sd.transform, "RETRY BOSS", "icon_restart", new Color(0.078f, 0.11f, 0.09f, 1f), Cream, 632, 486, 268, 60, 19, 0f);
            refs["_stageResultsButton"] = Btn("ResultsButton", sd.transform, "See all results", Clear, CreamDim, Clear, 370, 548, 200, 26, 14, 0f, Body);
            refs["_stagePanel"] = stagePanel.gameObject;
            _stageStars = lvlStars;

            refs["_iconNext"] = Ui("icon_next"); refs["_iconRetry"] = Ui("icon_restart"); refs["_iconLevels"] = Ui("icon_list");
            refs["_iconStages"] = Ui("icon_map"); refs["_iconResults"] = Ui("icon_list");

            // ---------------- post-quiz (after Level 10, before Stage Complete) ----------------
            refs["_quiz"] = EducationUiBuilder.BuildQuiz(c);

            // ---------------- results ----------------
            refs["_results"] = MenuSceneBuilder.BuildResults(c);

            return canvas;
        }

        // Arrays made inside BuildBattleUI, wired to UIManager after it is added.
        private static (Image[] stars, Button[] secondary, Text[] labels, Image[] icons) _endArrays;
        private static Image[] _stageStars;

        private static Button StageBtn(Transform parent, string label, string icon, Color fill, Color text, float x, float y, float w, float h, int size, float shadow)
        {
            var b = Btn(label.Replace(" ", "") + "Button", parent, "", fill, text, fill.a > 0.5f && fill.r > 0.5f ? Ink : Slate, x, y, w, h, size, shadow);
            Icon("Icon", b.transform, icon, text, 20, (h - 26) / 2, 26);
            TxtAt("Label", b.transform, label, Pixel, size, text, 56, 0, w - 64, h, TextAnchor.MiddleLeft);
            return b;
        }

        private static DeployCard Card(Transform parent, int index)
        {
            var frame = At(Img($"Card_{index + 1}", parent, Hex("#3B5446"), Ui("ui_frame3"), true), 20 + index * 82, 9, 70, 70);
            frame.type = Image.Type.Sliced;
            var back = Img("Back", frame.transform, MossDim); Stretch(back.rectTransform, 3); back.transform.SetAsFirstSibling();
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            var cb = btn.colors; cb.disabledColor = new Color(0.7f, 0.7f, 0.7f, 1f); btn.colors = cb;
            var icon = At(Img("Icon", frame.transform, Color.white), 3, 3, 64, 64);
            icon.preserveAspect = true;
            var badge = AtRight(Img("CostBadge", frame.transform, Ink), -2, 50, 22, 22);
            var cost = Txt("Cost", badge.transform, "2", BodyBold, 14, Mint, TextAnchor.MiddleCenter); Stretch(cost.rectTransform);
            var card = frame.gameObject.AddComponent<DeployCard>();
            Ref(card, "_button", btn); Ref(card, "_icon", icon); Ref(card, "_frame", frame); Ref(card, "_cost", cost);
            return card;
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

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
