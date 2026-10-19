#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ThermoTactics.Dev
{
    /// <summary>
    /// Editor-only quick check of the screens added on 13 October (menu Thermo-Tactics ▸ Test ▸ Quick Check):
    /// title with the Baguio progress strip, Stage Select, level path with stars, Settings, Level 1 with the lane camera
    /// and Level Complete, a forced loss (Level Failed), and the Level 10 boss wave (resumed from a prepared save with
    /// low boss HP) through to Stage Complete. Uses the player "Playtest"; screenshots and report go to Logs/qa/check/.
    /// </summary>
    public class QuickCheck : MonoBehaviour
    {
        public const string FlagKey = "tt.quickcheck";
        private const string Player = "Playtest";
        private const string OutDir = "Logs/qa/check";

        private readonly StringBuilder _report = new StringBuilder();
        private readonly List<string> _errors = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!UnityEditor.SessionState.GetBool(FlagKey, false)) return;
            UnityEditor.SessionState.SetBool(FlagKey, false);
            var go = new GameObject("QuickCheck");
            DontDestroyOnLoad(go);
            go.AddComponent<QuickCheck>();
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;
        private void OnLog(string m, string st, LogType t)
        {
            if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert) _errors.Add($"{t}: {m}\n{st}");
        }

        private IEnumerator Start()
        {
            Time.maximumDeltaTime = 1f;
            Directory.CreateDirectory(OutDir);
            foreach (string f in Directory.GetFiles(OutDir)) File.Delete(f);
            Line($"Thermo-Tactics quick check · {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            SettingsStore.LaneCamera = true;
            SettingsStore.BattleSpeedIndex = 0;

            // 1. Main Menu with some progress (Levels 1–4 cleared with 3, 2, 1, 3 stars).
            yield return Load(GameSession.MenuScene);
            var menu = FindFirstObjectByType<MainMenuController>();
            StageData stage = menu.Stage;
            var db = menu.Database;
            db.ResetProgress(Player);
            int[] stars = { 3, 2, 1, 3 };
            for (int i = 0; i < stars.Length; i++) db.RecordAttempt(Player, stage.stageId, i + 1, true, 1.0f, stars[i]);
            GameSession.PlayerName = Player; SettingsStore.LastPlayer = Player;
            yield return Load(GameSession.MenuScene);
            menu = FindFirstObjectByType<MainMenuController>();
            yield return Hold(2f);
            yield return Shot("01_title_progress");
            Check("Title progress strip visible", GameObject.Find("StageProgress") != null && GameObject.Find("StageProgress").activeInHierarchy);
            Check("Title background moves (MenuAmbience)", FindFirstObjectByType<MenuAmbience>() != null);
            menu.ShowStageSelect(); yield return Hold(0.8f); yield return Shot("02_stage_select");
            menu.ShowLevelSelect(3); yield return Hold(0.8f); yield return Shot("03_level_path_stars");
            menu.ShowTitle(); FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include).Open(); yield return Hold(0.8f); yield return Shot("04_settings_lane_camera");
            FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include).Close();

            // 1b. Pre-quiz on the first entry into Baguio (2 of 5 right), then the level path.
            db.DevClearEducation(Player);
            SettingsStore.PopupsEnabled = true;
            menu.ShowStageSelect(); yield return Hold(0.5f);
            menu.EnterStage(); yield return Hold(0.8f);
            var quiz = FindFirstObjectByType<QuizPanel>(FindObjectsInactive.Include);
            Check("Pre-quiz shown on first stage entry", quiz.IsOpen);
            yield return Shot("05_pre_quiz");
            quiz.DevSubmit(PartlyRight(stage.quiz.preQuestions, 2)); yield return Hold(0.6f); yield return Shot("06_pre_quiz_result");
            quiz.DevContinue(); yield return Hold(0.6f);
            Check("Level path after the pre-quiz", Active("LevelSelect"));
            Check("Pre-quiz saved (2 / 5)", db.GetQuiz(Player, stage.stageId, QuizManager.Pre)?.Score == 2);
            menu.ShowStageSelect(); menu.EnterStage(); yield return Hold(0.4f);
            Check("No pre-quiz on the second entry", !quiz.IsOpen);

            // 2. Level 1 played with the lane camera, to Level Complete.
            GameSession.Play(Player, stage, 1);
            yield return Load(GameSession.BattleScene);
            bool won = false;
            yield return Play(1, true, w => won = w);
            Check("Climate fact pop-up shown in Level 1", _factShots > 0);
            Check("Level 1 won", won);
            Check("Level Complete screen shown", Active("EndPanel"));
            yield return Hold(2.2f); yield return Shot("10_level_complete");

            // 3. A forced loss: temperature pushed near the limit, End Turn with an empty board.
            GameSession.Play(Player, stage, 2);
            yield return Load(GameSession.BattleScene);
            var gm = GameManager.Instance;
            while (gm.Current != GameManager.State.Playing) yield return null;
            yield return null;
            while (PopupManager.IsOpen) yield return DismissFact();   // Level 2's climate fact
            var temp = FindFirstObjectByType<TemperatureSystem>();
            temp.Add(0.42f);
            FindFirstObjectByType<TurnManager>().EndTurn();
            float t0 = Time.realtimeSinceStartup;
            while (gm.Current != GameManager.State.Ended && Time.realtimeSinceStartup - t0 < 120f) yield return null;
            Check("Level Failed after reaching 1.5 °C", gm.Current == GameManager.State.Ended && temp.HasReachedThreshold);
            yield return Hold(1f); yield return Shot("11_level_failed");

            // 4. Level 10 boss wave from a prepared save (boss at 40 HP), to Stage Complete.
            for (int l = 5; l <= 9; l++) db.RecordAttempt(Player, stage.stageId, l, true, 1.0f, 2);
            var save = new SessionSnapshot
            {
                playerName = Player, stageId = stage.stageId, levelNumber = 10, waveNumber = 5, turnNumber = 1,
                oxygenLeft = 8, oxygenAllowance = 8, oxygenPenalty = 0, temperature = 1.1f, peakTemperature = 1.15f, bossTurn = 0,
            };
            save.enemies.Add(new SavedUnit { lane = 0, unitId = "B01", hp = 40, maxHp = 300 });
            GameSession.Play(Player, stage, 10, save);
            yield return Load(GameSession.BattleScene);
            yield return Play(10, true, w => won = w);
            Check("Boss defeated, Level 10 won", won);

            // Post-quiz (Q6–Q10), all right: 2 / 5 → 5 / 5 = +150%. Pre had Q1 (climate) and Q2 (forests) right, so
            // climate 1/3 → 1/1 and game 0/1 → 2/2 improve; forests 1/1 → 2/2 stays at 100%.
            yield return Hold(0.8f);
            quiz = FindFirstObjectByType<QuizPanel>(FindObjectsInactive.Include);
            Check("Post-quiz shown before Stage Complete", quiz.IsOpen && !Active("StageCompletePanel"));
            yield return Shot("20_post_quiz");
            quiz.DevSubmit(); yield return Hold(0.8f); yield return Shot("21_post_quiz_compare");
            QuizOutcome post = QuizManager.LoadPost(db, Player, stage);
            Check("Post-quiz compared with the pre-quiz (gain +150%, 2 of 3 topics improved)",
                post != null && post.Gain.HasValue && Mathf.Approximately(post.Gain.Value, 150f) && post.ImprovedCount == 2 && post.Topics.Length == 3);
            if (post != null)
                foreach (TopicResult t in post.Topics)
                    Line($"  {t.TopicName}: pre {t.PreCorrect}/{t.PreTotal} → post {t.PostCorrect}/{t.PostTotal}{(t.Improved ? " · improved" : "")}");
            quiz.DevContinue();
            Check("Stage Complete screen shown", Active("StageCompletePanel"));
            yield return Hold(2.2f); yield return Shot("22_stage_complete");

            // 5. Menu afterwards.
            GameSession.OpenLevelSelect = GameSession.OpenStageSelect = false;
            yield return Load(GameSession.MenuScene);
            menu = FindFirstObjectByType<MainMenuController>();
            yield return Hold(1.5f); yield return Shot("30_title_after");
            menu.ShowStageSelect(); yield return Hold(0.8f); yield return Shot("31_stage_select_after");
            menu.ShowLevelSelect(10); yield return Hold(0.8f); yield return Shot("32_level_path_after");
            menu.OpenFacts(); yield return Hold(0.8f); yield return Shot("33_climate_facts");
            Check("Climate Facts lists the seen facts", Active("FactsPanel"));
            var export = QuizResultExporter.Export(db, stage, "QC");
            Check("Quiz CSV exported", export.Success);
            Line("  " + export.Message);

            int total = db.GetProgress(Player, stage.stageId).Sum(p => StarRating.For(p));
            Line($"Stars in Baguio after the check: {total} / 30");
            Line(_errors.Count == 0 ? "Console errors: none" : $"Console errors: {_errors.Count}");
            foreach (string e in _errors.Distinct().Take(15)) Line("  " + e.Replace("\n", "\n    "));
            Line("DONE");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        private IEnumerator Play(int level, bool shots, System.Action<bool> done)
        {
            var gm = GameManager.Instance;
            var turns = FindFirstObjectByType<TurnManager>();
            var grid = FindFirstObjectByType<GridManager>();
            var oxygen = FindFirstObjectByType<OxygenPointSystem>();
            var deploy = FindFirstObjectByType<DeploySystem>();
            var waves = FindFirstObjectByType<WaveManager>();
            while (gm.Current != GameManager.State.Playing) yield return null;
            int turn = 0;
            while (gm.Current == GameManager.State.Playing && turn < 60)
            {
                while (!gm.CanPlayerAct && gm.Current == GameManager.State.Playing)
                {
                    if (PopupManager.IsOpen) yield return DismissFact();
                    yield return null;
                }
                if (gm.Current != GameManager.State.Playing) break;
                turn++;
                Deploy(gm.Level, grid, oxygen, deploy);
                yield return null;
                turns.EndTurn();
                if (shots && turn == 1)
                {
                    float[] at = { 1.2f, 3.5f, 6.5f, 9.5f };
                    float t0 = Time.time;
                    for (int i = 0; i < at.Length && turns.IsResolving; i++)
                    {
                        while (Time.time - t0 < at[i] && turns.IsResolving) yield return null;
                        if (turns.IsResolving) yield return Shot($"L{level}_lane_{i + 1}");
                    }
                }
                while (turns.IsResolving)
                {
                    if (PopupManager.IsOpen) { yield return DismissFact(); continue; }   // a fact after a cleared wave
                    if (!shots || turn > 1) Time.timeScale = 6f;
                    yield return null;
                }
            }
            Time.timeScale = 1f;
            bool won = gm.Current == GameManager.State.Ended && waves.IsCurrentWaveCleared && !waves.HasNextWave;
            Line($"Level {level}: {(won ? "WIN" : "LOSS")} in {turn} turns");
            done(won);
        }

        private int _factShots;

        /// <summary>Photographs the fact card on screen (the first two), then closes it.</summary>
        private IEnumerator DismissFact()
        {
            yield return Hold(0.5f);
            if (_factShots < 2) yield return Shot($"fact_popup_{++_factShots}");
            else _factShots++;
            GameManager.Instance.Popups.DevDismiss();
            yield return null;
        }

        /// <summary>Answers with the first <paramref name="right"/> questions correct and the rest wrong.</summary>
        private static int[] PartlyRight(QuizQuestion[] questions, int right)
        {
            var a = new int[questions.Length];
            for (int i = 0; i < a.Length; i++)
                a[i] = i < right ? questions[i].correctIndex : (questions[i].correctIndex + 1) % QuizData.ChoiceCount;
            return a;
        }

        private static void Deploy(LevelData level, GridManager grid, OxygenPointSystem oxygen, DeploySystem deploy)
        {
            var lanes = new List<int>();
            if (grid.Boss != null) lanes.AddRange(grid.Boss.PlanNext(grid).Lanes);
            for (int l = 0; l < grid.LaneCount; l++) if (grid.GetEnemy(l) != null && !lanes.Contains(l)) lanes.Add(l);
            for (int l = 0; l < grid.LaneCount; l++) if (!lanes.Contains(l)) lanes.Add(l);
            foreach (int lane in lanes)
            {
                if (!grid.IsAllySlotFree(lane)) continue;
                UnitData best = null;
                foreach (UnitData u in level.allyRoster)
                    if (u != null && oxygen.CanAfford(u.opCost) && (best == null || u.damage * u.maxHp / (float)u.opCost > best.damage * best.maxHp / (float)best.opCost)) best = u;
                if (best == null) break;
                deploy.Select(best);
                deploy.TryDeploy(lane);
            }
        }

        private static bool Active(string name)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<RectTransform>())
                if (t.name == name && t.gameObject.scene.IsValid() && t.gameObject.activeInHierarchy) return true;
            return false;
        }

        private void Check(string what, bool ok) => Line($"[{(ok ? "PASS" : "FAIL")}] {what}");

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null; yield return null;
            yield return Hold(0.5f);
        }

        private static IEnumerator Hold(float s) { for (float t = 0f; t < s; t += Time.unscaledDeltaTime) yield return null; }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, name + ".png"));
            yield return null;
        }

        private void Line(string s)
        {
            _report.AppendLine(s);
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), _report.ToString());
        }
    }
}
#endif
