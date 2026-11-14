#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace ThermoTactics.Dev
{
    /// <summary>
    /// Play-mode half of Thermo-Tactics ▸ Test ▸ Performance Check (Activity 10). Measures, on the same scenario every run:
    /// Main Menu load and 10 s idle, Battle scene load, a full Baguio Level 1 played at normal speed (frame rate, frame time,
    /// main-thread time, garbage per frame, memory), then three more battle loads to see whether memory keeps growing.
    /// Uses the dev player "Playtest". Writes Logs/qa/perf/report.txt and stops Play mode.
    /// </summary>
    public class PerformanceRun : MonoBehaviour
    {
        public const string FlagKey = "tt.perfcheck";
        private const string Player = "Playtest";
        private readonly StringBuilder _r = new StringBuilder();
        private int _unfocusedFrames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!UnityEditor.SessionState.GetBool(FlagKey, false)) return;
            UnityEditor.SessionState.SetBool(FlagKey, false);
            var go = new GameObject("PerformanceRun");
            DontDestroyOnLoad(go);
            go.AddComponent<PerformanceRun>();
        }

        private void Update() { if (!Application.isFocused) _unfocusedFrames++; }

        private IEnumerator Start()
        {
            Line($"Thermo-Tactics performance check · {DateTime.Now:yyyy-MM-dd HH:mm:ss} · v{Application.version}");
            Line($"Device: {SystemInfo.deviceModel} · {SystemInfo.operatingSystem}");
            Line($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} threads, {SystemInfo.processorFrequency} MHz) · RAM {SystemInfo.systemMemorySize} MB");
            Line($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB) · Unity {Application.unityVersion} Editor, Play mode");
            Line($"Game view: {Screen.width} x {Screen.height}");
            Line("");
            string dbPart = Path.Combine("Logs/qa/perf", "database.txt");
            if (File.Exists(dbPart)) Line(File.ReadAllText(dbPart).TrimEnd());
            Line("");

            LevelIntro.DevSkip = TutorialController.DevSkip = true;
            SettingsStore.LaneCamera = true;
            SettingsStore.BattleSpeedIndex = 0;
            GameSession.PlayerName = Player;

            // 1. Main Menu
            float t = Time.realtimeSinceStartup;
            yield return LoadAsync(GameSession.MenuScene);
            float menuLoad = (Time.realtimeSinceStartup - t) * 1000f;
            yield return Wait(2f);
            Line("MAIN MENU");
            Line($"  Scene load (MainMenu, until the first frame is drawn): {menuLoad:0} ms");
            Line($"  Frame rate limit on this screen: {(Application.targetFrameRate > 0 ? Application.targetFrameRate + " fps" : "none set (Android default is 30 fps)")}");
            yield return Sample("  Idle on the title screen, 10 s", 10f, null);
            Line($"  Memory after the menu: {Mem()}");
            Line("");

            // 2. Battle: Baguio Level 1 at normal speed
            var menu = FindFirstObjectByType<MainMenuController>();
            StageData stage = menu.FirstStage;
            menu.Database.ResetProgress(Player);
            menu.Database.DeleteSession(Player);
            GameSession.Play(Player, stage, 1);
            t = Time.realtimeSinceStartup;
            yield return LoadAsync(GameSession.BattleScene);
            float battleLoad = (Time.realtimeSinceStartup - t) * 1000f;
            Line("BATTLE (Baguio Level 1, played automatically at 1x speed with the lane camera on)");
            Line($"  Scene load (Baguio_Battle, until the first frame is drawn): {battleLoad:0} ms");
            Line($"  Frame rate limit in battle: {Application.targetFrameRate} fps");
            bool done = false; int turns = 0;
            StartCoroutine(PlayLevel(n => { turns = n; done = true; }));
            yield return Sample("  Whole level, all turns", 600f, () => done);
            Line($"  Level 1 finished in {turns} turns, result: {(GameManager.Instance != null && GameManager.Instance.Current == GameManager.State.Ended ? "ended" : "not finished")}");
            Line($"  Memory after the level: {Mem()}");
            Line("");

            // 3. Memory over repeated loads
            Line("MEMORY OVER REPEATED LOADS (garbage collected before each reading)");
            for (int i = 1; i <= 3; i++)
            {
                yield return LoadAsync(GameSession.MenuScene);
                GameSession.Play(Player, stage, 1);
                yield return LoadAsync(GameSession.BattleScene);
                yield return Wait(1.5f);
                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
                yield return null;
                Line($"  After battle load {i}: {Mem()}");
            }
            Line("");
            Line(_unfocusedFrames == 0 ? "Unity window focused for the whole run: results valid."
                                       : $"WARNING: Unity was in the background for {_unfocusedFrames} frames; frame-rate results may be too low.");
            Line("DONE");
            menu = null;
            LevelIntro.DevSkip = TutorialController.DevSkip = false;
            UnityEditor.EditorApplication.isPlaying = false;
        }

        // ------------------------------------------------------------ measuring

        private IEnumerator Sample(string label, float maxSeconds, Func<bool> stop)
        {
            var frames = new List<float>();
            var mainThread = new List<double>();
            long gcBytes = 0; int gcFrames = 0;
            using var mt = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            using var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            int collections = GC.CollectionCount(0);
            float t0 = Time.realtimeSinceStartup;
            yield return null;
            while (Time.realtimeSinceStartup - t0 < maxSeconds && (stop == null || !stop()))
            {
                frames.Add(Time.unscaledDeltaTime * 1000f);
                if (mt.Valid && mt.LastValue > 0) mainThread.Add(mt.LastValue / 1e6);
                if (gc.Valid) { gcBytes += gc.LastValue; gcFrames++; }
                yield return null;
            }
            float secs = Time.realtimeSinceStartup - t0;
            if (frames.Count == 0) { Line(label + ": no frames"); yield break; }
            var sorted = frames.OrderBy(x => x).ToList();
            float avg = frames.Average();
            Line($"{label} ({secs:0.0} s, {frames.Count} frames)");
            Line($"    Average frame rate: {frames.Count / secs:0.0} fps");
            Line($"    Frame time: average {avg:0.0} ms, 95th percentile {sorted[(int)(sorted.Count * 0.95f)]:0.0} ms, slowest {sorted[sorted.Count - 1]:0.0} ms");
            Line($"    Frames slower than 33 ms (below 30 fps): {frames.Count(f => f > 33.4f)} of {frames.Count}");
            if (mainThread.Count > 0) Line($"    Main-thread time per frame: average {mainThread.Average():0.00} ms");
            // In the Editor this counter also includes the Editor's own allocations; the game's own share is read with the
            // Unity Profiler (Profile Editor off). See Desktop/Activity10/evidence/profiler_findings for 14 November 2026.
            if (gcFrames > 0) Line($"    Garbage allocated per frame (Editor + game): average {gcBytes / (double)gcFrames / 1024.0:0.00} KB");
            Line($"    Garbage collections during the sample: {GC.CollectionCount(0) - collections}");
        }

        private static string Mem()
        {
            double mb(long b) => b / 1048576.0;
            return $"total allocated {mb(Profiler.GetTotalAllocatedMemoryLong()):0.0} MB, reserved {mb(Profiler.GetTotalReservedMemoryLong()):0.0} MB, managed heap used {mb(Profiler.GetMonoUsedSizeLong()):0.0} MB";
        }

        private static IEnumerator LoadAsync(string scene)
        {
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return new WaitForEndOfFrame();
        }

        private static IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.unscaledDeltaTime) yield return null; }

        /// <summary>Plays the level like a player: fills lanes with the best affordable ally each turn, then ends the turn.</summary>
        private IEnumerator PlayLevel(Action<int> done)
        {
            var gm = GameManager.Instance;
            while (gm == null) { yield return null; gm = GameManager.Instance; }
            var turns = FindFirstObjectByType<TurnManager>();
            var grid = FindFirstObjectByType<GridManager>();
            var oxygen = FindFirstObjectByType<OxygenPointSystem>();
            var deploy = FindFirstObjectByType<DeploySystem>();
            while (gm.Current != GameManager.State.Playing) yield return null;
            int turn = 0;
            while (gm.Current == GameManager.State.Playing && turn < 60)
            {
                while (!gm.CanPlayerAct && gm.Current == GameManager.State.Playing)
                {
                    if (PopupManager.IsOpen) { yield return Wait(1f); gm.Popups.DevDismiss(); }
                    yield return null;
                }
                if (gm.Current != GameManager.State.Playing) break;
                turn++;
                var lanes = new List<int>();
                for (int l = 0; l < grid.LaneCount; l++) if (grid.GetEnemy(l) != null) lanes.Add(l);
                for (int l = 0; l < grid.LaneCount; l++) if (!lanes.Contains(l)) lanes.Add(l);
                foreach (int lane in lanes)
                {
                    if (!grid.IsAllySlotFree(lane)) continue;
                    UnitData best = null;
                    foreach (UnitData u in gm.Squad)
                        if (u != null && oxygen.CanAfford(u.opCost) && (best == null || u.damage * u.maxHp / (float)u.opCost > best.damage * best.maxHp / (float)best.opCost)) best = u;
                    if (best == null) break;
                    deploy.Select(best);
                    deploy.TryDeploy(lane);
                }
                yield return null;
                turns.EndTurn();
                while (turns.IsResolving)
                {
                    if (PopupManager.IsOpen) { yield return Wait(1f); gm.Popups.DevDismiss(); }
                    yield return null;
                }
            }
            done(turn);
        }

        private void Line(string s)
        {
            _r.AppendLine(s);
            File.WriteAllText("Logs/qa/perf/report.txt", _r.ToString());
        }
    }
}
#endif
