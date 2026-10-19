using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Writes the quiz results for the study as a CSV file the researcher copies off the phone (USB or Android share).
    /// One row per player and stage: pre / post score, gain, and each question's pre / post correctness by topic.
    /// Reads the records through ResultsDatabase only; never opens the database itself.
    /// </summary>
    public static class QuizResultExporter
    {
        public class ExportResult
        {
            public bool Success;
            public string Path;
            public string Message;
        }

        public static ExportResult Export(ResultsDatabase db, StageData stage, string testerCode)
        {
            List<QuizRecord> records = db.GetAllQuizzes();
            if (records.Count == 0) return new ExportResult { Message = "No quiz results to export yet." };

            string tester = SafeFile(string.IsNullOrWhiteSpace(testerCode) ? "device" : testerCode.Trim());
            string csv = BuildCsv(db, records, stage, tester);
            string file = $"quiz_results_{tester}_{DateTime.Now:yyyy-MM-dd}.csv";

            foreach (string dir in Folders())
            {
                try
                {
                    Directory.CreateDirectory(dir);
                    string path = System.IO.Path.Combine(dir, file);
                    File.WriteAllText(path, csv, new UTF8Encoding(true));   // BOM so Excel reads ° and accents
                    return new ExportResult { Success = true, Path = path, Message = $"Saved to {path}" };
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

        private static string BuildCsv(ResultsDatabase db, List<QuizRecord> records, StageData stage, string tester)
        {
            QuizData stageQuiz = stage != null ? stage.quiz : null;
            int preCount = stageQuiz != null ? stageQuiz.Count(QuizManager.Pre) : QuizData.QuestionCount;
            int postCount = stageQuiz != null ? stageQuiz.Count(QuizManager.Post) : QuizData.QuestionCount;
            QuizTopic[] topics = stageQuiz != null && stageQuiz.topics != null ? stageQuiz.topics : new QuizTopic[0];
            var sb = new StringBuilder();
            var header = new List<string> { "tester", "player_name", "stage_id", "stage_name", "pre_score", "post_score", "gain_pct" };
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
                float? gain = QuizManager.Gain(pre?.Score, post?.Score);
                HashSet<string> seen = db.GetSeenFacts(player);
                int facts = sameStage && stage.facts != null ? stage.facts.Count(f => f != null && seen.Contains(f.factId)) : 0;

                var row = new List<string>
                {
                    tester, player, stageId.ToString(), sameStage ? stage.stageName : "",
                    pre != null ? pre.Score.ToString() : "", post != null ? post.Score.ToString() : "",
                    gain.HasValue ? gain.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "N/A",
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
