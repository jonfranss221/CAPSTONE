#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThermoTactics.Dev
{
    /// <summary>
    /// Editor-only automatic play-test (menu Thermo-Tactics ▸ Test ▸ Play Stage 1 Automatically).
    /// Plays Baguio Levels 1–10 in order as the player "Playtest" with a simple strategy (fill the lanes that
    /// have enemies first, then the rest), retries a lost level up to twice, and saves screenshots and a report
    /// to Logs/qa/auto/. Never included in a phone build.
    /// </summary>
    public class AutoPlaytest : MonoBehaviour
    {
        public const string FlagKey = "tt.autotest";
        public const string Player = "Playtest";
        private const string OutDir = "Logs/qa/auto";
        private const float FastSpeed = 8f;

        private readonly StringBuilder _report = new StringBuilder();
        private readonly List<string> _errors = new List<string>();
        private StageData _stage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!UnityEditor.SessionState.GetBool(FlagKey, false)) return;
            UnityEditor.SessionState.SetBool(FlagKey, false);   // one run per menu click
            var go = new GameObject("AutoPlaytest");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoPlaytest>();
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errors.Add($"{type}: {message}\n{stack}");
        }

        private IEnumerator Start()
        {
            Time.maximumDeltaTime = 1f;   // keep game time moving even when the editor draws few frames
            Directory.CreateDirectory(OutDir);
            foreach (string f in Directory.GetFiles(OutDir)) File.Delete(f);
            Line($"Thermo-Tactics automatic play-test · {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} · version {Application.version}");

            // Start from the Main Menu with a clean "Playtest" player.
            yield return LoadScene(GameSession.MenuScene);
            var menu = FindFirstObjectByType<MainMenuController>();
            _stage = menu.Stage;
            menu.Database.ResetProgress(Player);
            menu.Database.DeleteSession(Player);
            SettingsStore.LaneCamera = true;
            SettingsStore.BattleSpeedIndex = 0;
            Line($"Stage: {_stage.stageName}, {_stage.LevelCount} levels");
            Line("");

            for (int level = 1; level <= _stage.LevelCount; level++)
            {
                bool won = false;
                for (int attempt = 1; attempt <= 3 && !won; attempt++)
                {
                    // The lane camera is shown (and photographed) in Levels 1 and 10; elsewhere it is off to save time.
                    SettingsStore.LaneCamera = level == 1 || level == _stage.LevelCount;
                    GameSession.Play(Player, _stage, level);
                    yield return LoadScene(GameSession.BattleScene);
                    yield return PlayLevel(level, attempt, r => won = r);
                }
                if (!won) Line($"  !! Level {level} not won after 3 attempts; the play-test continues so later screens are still checked.");
                if (!won) ForceClear(level);
            }

            SettingsStore.LaneCamera = true;
            // Menu screens with the finished progress.
            GameSession.PlayerName = Player;
            SettingsStore.LastPlayer = Player;
            GameSession.OpenLevelSelect = GameSession.OpenStageSelect = false;
            yield return LoadScene(GameSession.MenuScene);
            menu = FindFirstObjectByType<MainMenuController>();
            yield return Hold(2.5f);
            yield return Shot("90_title_progress");
            menu.ShowStageSelect();
            yield return Hold(1f);
            yield return Shot("91_stage_select");
            menu.ShowLevelSelect(5);
            yield return Hold(1f);
            yield return Shot("92_level_select_L5");
            menu.ShowLevelSelect(10);
            yield return Hold(1f);
            yield return Shot("93_level_select_L10");

            Line("");
            Line("Stars per level (from level_progress):");
            int total = 0;
            foreach (LevelProgress p in menu.Database.GetProgress(Player, _stage.stageId))
            {
                int s = StarRating.For(p);
                total += s;
                Line($"  Level {p.LevelNumber,2}: cleared {p.IsCleared} · attempts {p.Attempts} · best stars {p.BestStars} · best final °C {p.BestFinalTemp:0.00}");
            }
            Line($"  Total stars: {total} / {_stage.LevelCount * StarRating.MaxStars}");
            Line("");
            Line(_errors.Count == 0 ? "Console errors: none" : $"Console errors: {_errors.Count}");
            foreach (string e in _errors.Distinct().Take(20)) Line("  " + e.Replace("\n", "\n    "));
            Line("DONE");
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), _report.ToString());
            Debug.Log("[AutoPlaytest] Finished. Report: " + Path.GetFullPath(Path.Combine(OutDir, "report.txt")));
            UnityEditor.EditorApplication.isPlaying = false;
        }

        private IEnumerator PlayLevel(int level, int attempt, System.Action<bool> done)
        {
            var gm = GameManager.Instance;
            var turns = FindFirstObjectByType<TurnManager>();
            var grid = FindFirstObjectByType<GridManager>();
            var oxygen = FindFirstObjectByType<OxygenPointSystem>();
            var temp = FindFirstObjectByType<TemperatureSystem>();
            var waves = FindFirstObjectByType<WaveManager>();
            var deploy = FindFirstObjectByType<DeploySystem>();
            while (gm.Current != GameManager.State.Playing) yield return null;

            int turn = 0;
            bool bossShot = false;
            while (gm.Current == GameManager.State.Playing && turn < 80)
            {
                while (!gm.CanPlayerAct && gm.Current == GameManager.State.Playing) yield return null;
                if (gm.Current != GameManager.State.Playing) break;
                turn++;
                Deploy(gm.Level, grid, oxygen, deploy);
                yield return null;

                // Screenshots: the first turn of Level 1 (lane zoom) and the first boss turn of Level 10.
                bool laneShots = level == 1 && turn == 1 && attempt == 1;
                bool bossShots = !bossShot && waves.IsBossWave && attempt == 1;
                if (laneShots || bossShots)
                {
                    yield return Shot(laneShots ? "01_L1_deployed" : "20_L10_boss_deployed");
                    Time.timeScale = 1f;
                    turns.EndTurn();
                    float[] at = { 1.1f, 3.4f, 6.5f, 9.5f, 12.5f };
                    float t0 = Time.time;
                    for (int i = 0; i < at.Length && turns.IsResolving; i++)
                    {
                        while (Time.time - t0 < at[i] && turns.IsResolving) yield return null;
                        if (turns.IsResolving) yield return Shot($"{(laneShots ? "02_L1_lane" : "21_L10_boss")}_{i + 1}");
                    }
                    bossShot |= bossShots;
                }
                else turns.EndTurn();

                while (turns.IsResolving) { Time.timeScale = FastSpeed; yield return null; }
            }
            Time.timeScale = 1f;

            bool won = gm.Current == GameManager.State.Ended && waves.IsCurrentWaveCleared && !waves.HasNextWave && !temp.HasReachedThreshold;
            yield return Hold(1.6f);   // let the stars pop in
            yield return Shot($"{(level == 10 ? "30" : "1" + (level - 1))}_L{level}_end_try{attempt}_{(won ? "win" : "loss")}");
            int stars = won ? StarRating.For(temp.Peak) : 0;
            Line($"Level {level,2} try {attempt}: {(won ? "WIN " : "LOSS")} · {turn,2} turns · waves {waves.WavesCleared}/{waves.TotalWaves} · peak {temp.Peak:0.00} °C · final {temp.Current:0.00} °C · stars {stars}");
            done(won);
        }

        /// <summary>A simple player: fill lanes with enemies first (strongest enemy first), then empty lanes.</summary>
        private static void Deploy(LevelData level, GridManager grid, OxygenPointSystem oxygen, DeploySystem deploy)
        {
            var lanes = new List<int>();
            BossUnit boss = grid.Boss;
            if (boss != null) lanes.AddRange(boss.PlanNext(grid).Lanes);
            for (int l = 0; l < grid.LaneCount; l++)
                if (grid.GetEnemy(l) != null && !lanes.Contains(l)) lanes.Add(l);
            lanes.Sort((a, b) => boss != null ? 0 : Damage(grid, b).CompareTo(Damage(grid, a)));
            for (int l = 0; l < grid.LaneCount; l++) if (!lanes.Contains(l)) lanes.Add(l);

            foreach (int lane in lanes)
            {
                if (!grid.IsAllySlotFree(lane)) continue;
                UnitData best = null;
                foreach (UnitData u in level.allyRoster)
                    if (u != null && oxygen.CanAfford(u.opCost) && (best == null || Value(u) > Value(best))) best = u;
                if (best == null) break;
                deploy.Select(best);
                deploy.TryDeploy(lane);
            }
        }

        private static int Damage(GridManager grid, int lane) { var e = grid.GetEnemy(lane); return e != null ? e.Damage : 0; }
        private static float Value(UnitData u) => u.damage * u.maxHp / (float)Mathf.Max(1, u.opCost);

        private void ForceClear(int level)
        {
            var db = FindFirstObjectByType<ResultsDatabase>();
            if (db != null) db.RecordAttempt(Player, _stage.stageId, level, true, 1.4f, 1);
            Line($"  (Level {level} marked cleared so Level {level + 1} can be tested.)");
        }

        private IEnumerator LoadScene(string name)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(name);
            yield return null;
            yield return null;
            yield return Hold(0.5f);
        }

        private static IEnumerator Hold(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, name + ".png"));
            yield return null;
        }

        private void Line(string s)
        {
            _report.AppendLine(s);
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), _report.ToString());   // keep the report current if Unity stops
        }
    }
}
#endif
