using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Writes the quiz results for the study as CSV files the researcher copies off the phone (USB or Android share):
    /// one file per stage (quiz_results_phone{letter}_stage{n}_{date}.csv), each with that stage's question and topic
    /// columns. One row per player: the player's code (A-001 ...) and name, pre / post score, normalized gain, and each
    /// question's pre / post correctness by topic. Admin only: Settings enables the button once the admin code is entered.
    /// Reads the records through ResultsDatabase only; never opens the database itself.
    /// </summary>
    public static class QuizResultExporter
    {
        public class ExportResult
        {
            public bool Success;
            public string Path;
            public string Message;
            /// <summary>Every file written (one per stage with results).</summary>
            public List<string> Files = new List<string>();
        }

        /// <summary>One stage (or, with null, every record without question columns).</summary>
        public static ExportResult Export(ResultsDatabase db, StageData stage)
            => ExportStages(db, stage != null ? new[] { stage } : null);

        /// <summary>Every given stage that has quiz results gets its own file with its own question columns.</summary>
        public static ExportResult ExportStages(ResultsDatabase db, StageData[] stages)
        {
            // Checked here, where the data is read, not only by greying out the button: no screen can export without admin.
            if (!AdminSession.Unlocked)
            {
                Debug.LogWarning("[QuizResultExporter] Export refused: admin tools are locked.");
                return new ExportResult { Message = "Admin only: enter the admin code in Settings first." };
            }
            List<QuizRecord> records = db.GetAllQuizzes();
            if (records.Count == 0) return new ExportResult { Message = "No quiz results to export yet." };

            // The phone letter in the name keeps files from different phones apart when they are merged.
            string date = $"{DateTime.Now:yyyy-MM-dd}";
            string letter = SafeFile(SettingsStore.PhoneLetter);
            var files = new List<(string name, string csv)>();
            if (stages == null || stages.Length == 0)
                files.Add(($"quiz_results_phone{letter}_{date}.csv", BuildCsv(db, records, null)));
            else
                foreach (StageData st in stages)
                {
                    if (st == null) continue;
                    List<QuizRecord> mine = records.Where(r => r.StageId == st.stageId).ToList();
                    if (mine.Count > 0) files.Add(($"quiz_results_phone{letter}_stage{st.stageId}_{date}.csv", BuildCsv(db, mine, st)));
                }
            if (files.Count == 0) return new ExportResult { Message = "No quiz results to export yet." };

            foreach (string dir in Folders())
            {
                try
                {
                    Directory.CreateDirectory(dir);
                    var result = new ExportResult { Success = true };
                    foreach (var f in files)
                    {
                        string path = System.IO.Path.Combine(dir, f.name);
                        File.WriteAllText(path, f.csv, new UTF8Encoding(true));   // BOM so Excel reads ° and accents
                        result.Files.Add(path);
                    }
                    result.Path = result.Files[0];
                    result.Message = files.Count == 1 ? $"Saved to {result.Path}" : $"Saved {files.Count} files (one per stage) to {dir}";
                    return result;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[QuizResultExporter] Could not write to {dir}: {e.Message}");
                }
            }
            return new ExportResult { Message = "Export failed: the file could not be written." };
        }

        /// <summary>Shared Documents/ThermoTactics first; the app's own folder (still reachable by USB) if that is not writable.</summary>
        private static IEnumerable<string> Folders()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            yield return "/storage/emulated/0/Documents/ThermoTactics";
#else
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(docs)) yield return System.IO.Path.Combine(docs, "ThermoTactics");
#endif
            yield return System.IO.Path.Combine(Application.persistentDataPath, "Exports");
        }

        private static string BuildCsv(ResultsDatabase db, List<QuizRecord> records, StageData stage)
        {
            QuizData stageQuiz = stage != null ? stage.quiz : null;
            int preCount = stageQuiz != null ? stageQuiz.Count(QuizManager.Pre) : 0;
            int postCount = stageQuiz != null ? stageQuiz.Count(QuizManager.Post) : 0;
            QuizTopic[] topics = stageQuiz != null && stageQuiz.topics != null ? stageQuiz.topics : new QuizTopic[0];
            var sb = new StringBuilder();
            var header = new List<string> { "participant_code", "player_name", "stage_id", "stage_name", "pre_score", "post_score", "max_score", "normalized_gain" };
            for (int q = 0; q < preCount; q++) header.Add(ColumnId(stageQuiz?.preQuestions, q, "pre"));
            for (int q = 0; q < postCount; q++) header.Add(ColumnId(stageQuiz?.postQuestions, q, "post"));
            foreach (QuizTopic t in topics) header.AddRange(new[] { $"topic_{t.id}_pre", $"topic_{t.id}_post" });
            header.AddRange(new[] { "pre_answers", "post_answers", "pre_attempts", "post_attempts", "facts_seen", "pre_taken_at", "post_taken_at", "app_version" });
            sb.AppendLine(string.Join(",", header));

            var groups = records.GroupBy(r => (r.PlayerName.ToLowerInvariant(), r.StageId));
            foreach (var g in groups)
            {
                QuizRecord pre = g.FirstOrDefault(r => r.QuizType == QuizManager.Pre);
                QuizRecord post = g.FirstOrDefault(r => r.QuizType == QuizManager.Post);
                string player = (pre ?? post).PlayerName;
                int stageId = g.Key.Item2;
                bool sameStage = stage != null && stage.stageId == stageId;
                QuizData quiz = sameStage ? stage.quiz : null;
                int max = quiz != null ? quiz.ScoredCount(QuizManager.Post) : (post ?? pre).MaxScore;
                float? gain = QuizManager.Gain(pre?.Score, post?.Score, max);
                HashSet<string> seen = db.GetSeenFacts(player);
                int facts = sameStage && stage.facts != null ? stage.facts.Count(f => f != null && seen.Contains(f.factId)) : 0;

                var row = new List<string>
                {
                    db.CodeOf(player), player, stageId.ToString(), sameStage ? stage.stageName : "",
                    pre != null ? pre.Score.ToString() : "", post != null ? post.Score.ToString() : "", max.ToString(),
                    QuizManager.GainText(gain),
                };
                // Per question: 1 = right, 0 = wrong, blank = not taken.
                bool[] preOk = pre != null && quiz != null ? QuizManager.Correct(QuizManager.ParseAnswers(pre.Answers), quiz.preQuestions) : null;
                bool[] postOk = post != null && quiz != null ? QuizManager.Correct(QuizManager.ParseAnswers(post.Answers), quiz.postQuestions) : null;
                for (int q = 0; q < preCount; q++) row.Add(preOk != null && q < preOk.Length ? (preOk[q] ? "1" : "0") : "");
                for (int q = 0; q < postCount; q++) row.Add(postOk != null && q < postOk.Length ? (postOk[q] ? "1" : "0") : "");
                // Per topic: right / asked in each quiz, e.g. "1/3".
                foreach (QuizTopic t in topics)
                {
                    row.Add(TopicScore(quiz?.preQuestions, preOk, t.id));
                    row.Add(TopicScore(quiz?.postQuestions, postOk, t.id));
                }
                row.AddRange(new[]
                {
                    pre?.Answers ?? "", post?.Answers ?? "",
                    pre != null ? pre.Attempts.ToString() : "", post != null ? post.Attempts.ToString() : "",
                    facts.ToString(), pre?.TakenAt ?? "", post?.TakenAt ?? "", Application.version,
                });
                sb.AppendLine(string.Join(",", row.Select(Quote)));
            }
            return sb.ToString();
        }

        private static string ColumnId(QuizQuestion[] questions, int i, string type)
            => questions != null && i < questions.Length && !string.IsNullOrEmpty(questions[i].questionId)
                ? questions[i].questionId : $"{type}_q{i + 1}";

        private static string TopicScore(QuizQuestion[] questions, bool[] ok, string topicId)
        {
            if (questions == null || ok == null) return "";
            int asked = 0, right = 0;
            for (int i = 0; i < questions.Length && i < ok.Length; i++)
                if (questions[i].topicId == topicId) { asked++; if (ok[i]) right++; }
            return asked > 0 ? $"{right}/{asked}" : "";
        }

        private static string Quote(string v)
        {
            v = v ?? "";
            return v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }

        private static string SafeFile(string s)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }
    }
}
