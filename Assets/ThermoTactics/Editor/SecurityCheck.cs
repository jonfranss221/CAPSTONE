using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Negative security tests (Activity 9): what happens when something invalid or unauthorized is tried.
    /// Runs in the editor without Play mode, on a separate test database in Temp/, so the real save file is never touched.
    /// Menu: Thermo-Tactics ▸ Test ▸ Security Checks. Report: Logs/qa/security/report.txt.
    /// The "right admin code" test needs the code in the git-ignored file UserSettings/admin_code.txt (or the TT_ADMIN_CODE
    /// environment variable); without it that test is marked SKIP. Never write the code in this file.
    /// </summary>
    public static class SecurityCheck
    {
        private const string TestDb = "Temp/security_test.db";
        private const string CorruptDb = "Temp/security_corrupt.db";
        private const string ReportDir = "Logs/qa/security";

        private static readonly StringBuilder Report = new StringBuilder();
        private static int _pass, _fail, _skip;

        [MenuItem("Thermo-Tactics/Test/Security Checks")]
        public static void Run()
        {
            Report.Clear(); _pass = _fail = _skip = 0;
            Report.AppendLine($"Thermo-Tactics security checks · {DateTime.Now:yyyy-MM-dd HH:mm:ss} · v{Application.version}");
            Report.AppendLine("Test database: " + TestDb + " (the real save file is not used)");
            Report.AppendLine();

            bool wasUnlocked = AdminSession.Unlocked;
            AdminSession.Lock();
            AdminSession.DevResetTries();
            ResultsDatabase db = NewDb(TestDb, fresh: true);
            try
            {
                Access(db);
                Authentication();
                Names(db);
                Scores(db);
                Missing(db);
                Corrupt();
            }
            catch (Exception e)
            {
                Fail("SEC-xx", "The check itself crashed", e.GetType().Name + ": " + e.Message);
            }
            finally
            {
                db.DevClose();
                UnityEngine.Object.DestroyImmediate(db.gameObject);
                AdminSession.Lock();
                AdminSession.DevResetTries();
                if (wasUnlocked) AdminSession.DevUnlock();
            }

            Report.AppendLine();
            Report.AppendLine($"PASS {_pass} · FAIL {_fail} · SKIP {_skip}");
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, "report.txt"), Report.ToString());
            Debug.Log("[SecurityCheck]\n" + Report);
        }

        // ---------------------------------------------------------------- tests

        /// <summary>SEC-01: a player (admin locked) cannot export the quiz results, even by calling the exporter directly.</summary>
        private static void Access(ResultsDatabase db)
        {
            db.RegisterPlayer("Export Tester");
            db.SaveQuiz("Export Tester", 1, QuizManager.Pre, "0,1,2,3,0,1", 3, 6, out _);
            QuizResultExporter.ExportResult r = QuizResultExporter.Export(db, null);
            Check("SEC-01", "Export quiz results while admin is locked",
                "Refused, no file written", !r.Success && r.Path == null, r.Message);
        }

        /// <summary>SEC-02 to SEC-04: the admin code.</summary>
        private static void Authentication()
        {
            bool wrong = AdminSession.TryUnlock("ADMIN123");
            Check("SEC-02", "Wrong admin code \"ADMIN123\"", "Refused, admin stays locked", !wrong && !AdminSession.Unlocked,
                AdminSession.Unlocked ? "unlocked" : "still locked");

            AdminSession.TryUnlock("guess1");
            AdminSession.TryUnlock("guess2");   // third wrong try in a row (with SEC-02)
            int wait = AdminSession.WaitLeft;
            string real = AdminSession.DevTestCode();
            bool duringWait = AdminSession.TryUnlock(string.IsNullOrEmpty(real) ? "guess3" : real);
            Check("SEC-03", "3 wrong codes in a row, then another try",
                "30-second wait; every code refused during it", wait > 0 && !duringWait && !AdminSession.Unlocked,
                $"wait {wait} s, next try {(duringWait ? "accepted" : "refused")}");
            AdminSession.DevResetTries();

            if (string.IsNullOrEmpty(real))
            {
                Skip("SEC-04", "Right admin code, then LOCK", "No local test code (UserSettings/admin_code.txt)");
                return;
            }
            bool ok = AdminSession.TryUnlock("  " + real.ToLowerInvariant() + " ");
            AdminSession.Lock();
            Check("SEC-04", "Right admin code (any capitals, extra spaces), then LOCK",
                "Unlocks; LOCK locks it again", ok && !AdminSession.Unlocked, ok ? "unlocked, then locked" : "refused");
        }

        /// <summary>SEC-05 to SEC-08: player names and codes.</summary>
        private static void Names(ResultsDatabase db)
        {
            int before = db.GetPlayers().Count;
            PlayerRecord tag = db.RegisterPlayer("<color=red>Hack");
            PlayerRecord sql = db.RegisterPlayer("Juan'); DROP TABLE players;--");
            bool tableOk = db.GetPlayers().Count == before;
            Check("SEC-05", "Names with symbols: \"<color=red>Hack\" and \"Juan'); DROP TABLE players;--\"",
                "Both refused; players table unchanged", tag == null && sql == null && tableOk,
                $"{(tag == null ? "refused" : "ACCEPTED")}, {(sql == null ? "refused" : "ACCEPTED")}, players {db.GetPlayers().Count}");

            PlayerRecord a = db.RegisterPlayer("Juan Dela Cruz");
            PlayerRecord b = db.RegisterPlayer("  juan   DELA cruz ");
            int count = db.GetPlayers().Count(p => p.Name.Equals("Juan Dela Cruz", StringComparison.OrdinalIgnoreCase));
            Check("SEC-06", "Same name with other capitals and spaces: \"  juan   DELA cruz \"",
                "Same player and code, no second record", a != null && b != null && a.Code == b.Code && count == 1,
                $"{a?.Code} and {b?.Code}, {count} record(s)");

            PlayerRecord c = db.RegisterPlayer("Maria Santos");
            db.ResetProgress("Juan Dela Cruz");
            PlayerRecord again = db.RegisterPlayer("Juan Dela Cruz");
            List<string> codes = db.GetPlayers().Select(p => p.Code).ToList();
            Check("SEC-07", "New player after others; reset a player's progress",
                "Next new code; reset keeps the code; no code used twice",
                c != null && again != null && again.Code == a.Code && codes.Distinct().Count() == codes.Count && c.Code != a.Code,
                $"Maria {c?.Code}, Juan still {again?.Code}, {codes.Count} codes all different");

            string longName = "Maximiliano Bartolome Santiago";
            PlayerRecord l = db.RegisterPlayer(longName);
            Check("SEC-08", $"Name longer than 20 characters ({longName.Length})", "Cut to 20 characters",
                l != null && l.Name.Length == ResultsDatabase.MaxNameLength, l != null ? $"saved as \"{l.Name}\"" : "refused");
        }

        /// <summary>SEC-09: impossible quiz scores are rejected by the database itself.</summary>
        private static void Scores(ResultsDatabase db)
        {
            QuizRecord neg = db.SaveQuiz("Maria Santos", 1, QuizManager.Pre, "0,0,0,0,0,0", -500, 6, out _);
            QuizRecord big = db.SaveQuiz("Maria Santos", 1, QuizManager.Post, "0,0,0,0,0,0", 99, 6, out _);
            QuizRecord ok = db.SaveQuiz("Maria Santos", 1, QuizManager.Pre, "0,1,2,3,0,1", 4, 6, out _);
            Check("SEC-09", "Quiz score -500, then 99 of 6, then a valid 4 of 6",
                "-500 and 99 rejected, nothing saved; 4 of 6 saved", neg == null && big == null && ok != null && ok.Score == 4,
                $"-500 {(neg == null ? "rejected" : "SAVED")}, 99 {(big == null ? "rejected" : "SAVED")}, 4/6 {(ok != null ? "saved" : "lost")}");
        }

        /// <summary>SEC-10: records that do not exist.</summary>
        private static void Missing(ResultsDatabase db)
        {
            try
            {
                PlayerRecord p = db.FindPlayer("Nobody Here");
                SessionSnapshot s = db.LoadSession("Nobody Here");
                QuizRecord q = db.GetQuiz("Nobody Here", 9, QuizManager.Post);
                int cleared = db.CountCleared("Nobody Here", 1);
                Check("SEC-10", "Load a player, saved game and quiz that do not exist",
                    "Nothing found, no crash", p == null && s == null && q == null && cleared == 0, "all empty, no error");
            }
            catch (Exception e) { Fail("SEC-10", "Load records that do not exist", e.GetType().Name); }
        }

        /// <summary>SEC-11: a damaged save file.</summary>
        private static void Corrupt()
        {
            File.WriteAllText(CorruptDb, "this is not a database file");
            ResultsDatabase bad = NewDb(CorruptDb, fresh: false);
            try
            {
                List<LeaderboardRow> rows = bad.GetLeaderboard();
                PlayerRecord p = bad.RegisterPlayer("After Damage");
                Check("SEC-11", "Save file replaced with text (damaged database)",
                    "Error logged, empty results, no crash", rows.Count == 0 && p == null, "logged, leaderboard empty, no crash");
            }
            catch (Exception e) { Fail("SEC-11", "Damaged database", e.GetType().Name + " escaped"); }
            finally
            {
                bad.DevClose();
                UnityEngine.Object.DestroyImmediate(bad.gameObject);
                File.Delete(CorruptDb);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static ResultsDatabase NewDb(string path, bool fresh)
        {
            if (fresh && File.Exists(path)) File.Delete(path);
            var go = new GameObject("SecurityCheckDb") { hideFlags = HideFlags.HideAndDontSave };
            var db = go.AddComponent<ResultsDatabase>();
            db.DevPathOverride = path;
            return db;
        }

        private static void Check(string id, string attempt, string expected, bool passed, string actual)
        {
            if (passed) _pass++; else _fail++;
            Report.AppendLine($"[{(passed ? "PASS" : "FAIL")}] {id} {attempt}");
            Report.AppendLine($"       expected: {expected}");
            Report.AppendLine($"       actual:   {actual}");
        }

        private static void Fail(string id, string attempt, string actual) => Check(id, attempt, "No crash", false, actual);

        private static void Skip(string id, string attempt, string why)
        {
            _skip++;
            Report.AppendLine($"[SKIP] {id} {attempt}: {why}");
        }
    }
}
