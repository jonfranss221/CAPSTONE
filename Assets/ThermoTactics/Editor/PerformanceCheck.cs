using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Repeatable performance baseline (Activity 10). Menu: Thermo-Tactics ▸ Test ▸ Performance Check.
    /// Step 1 (edit mode): times the database operations the game uses most, on a copy of the real save file in Temp/,
    /// so the real file is never changed, and times the admin-code check. Step 2 (Play mode, see Dev.PerformanceRun):
    /// scene loading, frame rate and frame time on the Main Menu and during a real Baguio Level 1, memory and garbage
    /// collection, and memory after loading the battle three times. Report: Logs/qa/perf/report.txt.
    /// Keep the Unity window focused and do not touch it while it runs: Unity slows down when it is in the background.
    /// </summary>
    public static class PerformanceCheck
    {
        public const string OutDir = "Logs/qa/perf";
        public const string DbPart = OutDir + "/database.txt";
        private const string Copy = "Temp/perf_copy.db";
        private const int Runs = 50;

        [MenuItem("Thermo-Tactics/Test/Performance Check")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(OutDir);
            foreach (string f in Directory.GetFiles(OutDir)) File.Delete(f);
            File.WriteAllText(DbPart, MeasureDatabase());
            SessionState.SetBool(Dev.PerformanceRun.FlagKey, true);
            EditorSceneManager.OpenScene(MenuSceneBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static string MeasureDatabase()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"DATABASE (copy of the real save file, {Runs} runs each, Stopwatch)");
            string real = Path.Combine(Application.persistentDataPath, "thermo_tactics.db");
            if (!File.Exists(real)) { sb.AppendLine("  real save file not found; skipped"); return sb.ToString(); }
            File.Copy(real, Copy, true);
            sb.AppendLine($"  Save file size: {new FileInfo(real).Length / 1024.0:0.0} KB");

            var go = new GameObject("PerfDb") { hideFlags = HideFlags.HideAndDontSave };
            var db = go.AddComponent<ResultsDatabase>();
            db.DevPathOverride = Copy;
            try
            {
                const string p = "Perf Tester";
                db.RegisterPlayer(p);
                int players = db.GetPlayers().Count, results = db.GetAll().Count, quizzes = db.GetAllQuizzes().Count;
                sb.AppendLine($"  Records: {players} players, {results} game results, {quizzes} quiz records");
                var snap = new SessionSnapshot { playerName = p, stageId = 1, levelNumber = 3, waveNumber = 2, turnNumber = 2,
                    oxygenLeft = 3, oxygenAllowance = 6, temperature = 1.1f, peakTemperature = 1.2f, squad = "A01,A02,A03,A04,A05" };
                for (int l = 0; l < 6; l++)
                {
                    snap.allies.Add(new SavedUnit { lane = l, unitId = "A01", hp = 20, maxHp = 30 });
                    snap.enemies.Add(new SavedUnit { lane = l, unitId = "E01", hp = 25, maxHp = 25 });
                }
                db.SaveSession(snap);   // warm-up (first call opens the file)
                Time("Save game (SaveSession, 6 allies + 6 enemies)", () => db.SaveSession(snap), sb);
                Time("Load game (LoadSession)", () => db.LoadSession(p), sb);
                Time("Record a level result (RecordAttempt)", () => db.RecordAttempt(p, 1, 1, true, 1.1f, 3), sb);
                Time("Read stage progress (GetProgress)", () => db.GetProgress(p, 1), sb);
                Time("Build the leaderboard (GetLeaderboard)", () => db.GetLeaderboard(), sb);
                Time("Find a player (FindPlayer)", () => db.FindPlayer(p), sb);
                Time("Save a quiz attempt (SaveQuiz)", () => db.SaveQuiz(p, 1, QuizManager.Pre, "0,1,2,3,0,1", 3, 6, out _), sb);
            }
            finally
            {
                db.DevClose();
                UnityEngine.Object.DestroyImmediate(go);
                File.Delete(Copy);
            }

            var w = Stopwatch.StartNew();
            AdminSession.Hash("timing only");
            sb.AppendLine($"  Admin code check (one PBKDF2 hash): {w.Elapsed.TotalMilliseconds:0} ms");
            return sb.ToString();
        }

        private static void Time(string what, Action act, StringBuilder sb)
        {
            var ms = new double[Runs];
            for (int i = 0; i < Runs; i++)
            {
                var w = Stopwatch.StartNew();
                act();
                ms[i] = w.Elapsed.TotalMilliseconds;
            }
            Array.Sort(ms);
            sb.AppendLine($"  {what}: average {ms.Average():0.00} ms, median {ms[Runs / 2]:0.00} ms, slowest {ms[Runs - 1]:0.00} ms");
        }
    }
}
