using System.Collections.Generic;
using System.Linq;

namespace ThermoTactics
{
    /// <summary>One concept in the pre → post comparison (questions right / asked, in each quiz).</summary>
    public class TopicResult
    {
        public string TopicId;
        public string TopicName;
        public int PreCorrect, PreTotal;     // PreTotal 0: no pre-quiz on record (or no pre question on this topic)
        public int PostCorrect, PostTotal;
        public string ReviewFact;            // title of a fact that teaches this topic (for "review" hints)
        public bool Scored = true;           // false for the game comprehension row (reported apart from the score)

        public bool HasPre => PreTotal > 0;
        public float PreShare => PreTotal > 0 ? PreCorrect / (float)PreTotal : 0f;
        public float PostShare => PostTotal > 0 ? PostCorrect / (float)PostTotal : 0f;
        public bool Improved => HasPre && PostTotal > 0 && PostShare > PreShare + 0.001f;
        public bool Mastered => PostTotal > 0 && PostCorrect == PostTotal;
        public bool Dropped => HasPre && PostTotal > 0 && PostShare < PreShare - 0.001f;
    }

    /// <summary>What the quiz screen shows after Submit.</summary>
    public class QuizOutcome
    {
        public string Type;
        public int Score;            // this attempt
        public int KeptScore;        // the stored (first) score, the one the study uses
        public int MaxScore;         // scored questions only (the matched pairs)
        public bool IsRetake;
        public bool Saved;
        public int? PreScore;        // post-quiz: the player's pre-quiz score for the stage
        public float? Gain;          // post-quiz: normalized gain (post − pre) / (max − pre), null when pre is missing or full
        public TopicResult[] Topics = new TopicResult[0];   // post-quiz: one row per topic
        public int ImprovedCount => Topics.Count(t => t.Scored && t.Improved);
    }

    /// <summary>
    /// Scores the pre- and post-quizzes and compares them (Parts II and IV, Process 2). The quizzes are matched pairs:
    /// each concept has one pre question and one post twin on the same topic, so the comparison is per concept.
    /// Only scored questions count; game comprehension questions are reported apart. Gain is the normalized gain
    /// (post − pre) / (max − pre), computed from the stored scores, never stored itself.
    /// </summary>
    public static class QuizManager
    {
        public const string Pre = "pre";
        public const string Post = "post";

        public static bool IsComplete(int[] answers, QuizData quiz, string type)
        {
            if (quiz == null || answers == null || answers.Length != quiz.Count(type)) return false;
            foreach (int a in answers) if (a < 0 || a >= QuizData.ChoiceCount) return false;
            return true;
        }

        public static bool[] Correct(int[] answers, QuizQuestion[] questions)
        {
            var result = new bool[questions.Length];
            for (int i = 0; i < questions.Length; i++)
                result[i] = answers != null && i < answers.Length && answers[i] == questions[i].correctIndex;
            return result;
        }

        /// <summary>Right answers among the scored questions (game comprehension questions do not count).</summary>
        public static int Score(int[] answers, QuizQuestion[] questions)
        {
            bool[] ok = Correct(answers, questions);
            int n = 0;
            for (int i = 0; i < questions.Length; i++) if (ok[i] && questions[i].scored) n++;
            return n;
        }

        public static string FormatAnswers(int[] answers) => string.Join(",", answers);

        /// <summary>"2,0,1,3,1" → {2,0,1,3,1}; bad entries become -1.</summary>
        public static int[] ParseAnswers(string s)
        {
            if (string.IsNullOrEmpty(s)) return new int[0];
            return s.Split(',').Select(p => int.TryParse(p.Trim(), out int v) ? v : -1).ToArray();
        }

        /// <summary>
        /// Normalized gain: the share of the room left after the pre-quiz that the player gained, (post − pre) / (max − pre).
        /// Null when a score is missing or the pre-quiz was already full (no room to gain). Negative when the score dropped.
        /// </summary>
        public static float? Gain(int? pre, int? post, int max)
        {
            if (!pre.HasValue || !post.HasValue || max <= 0 || pre.Value >= max) return null;
            return (post.Value - pre.Value) / (float)(max - pre.Value);
        }

        public static string GainText(float? gain) => gain.HasValue ? gain.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : "N/A";

        public static bool NeedsPre(ResultsDatabase db, string player, StageData stage)
            => Ready(stage, Pre) && db.GetQuiz(player, stage.stageId, Pre) == null;

        public static bool NeedsPost(ResultsDatabase db, string player, StageData stage)
            => Ready(stage, Post) && db.GetQuiz(player, stage.stageId, Post) == null;

        private static bool Ready(StageData stage, string type) => stage != null && stage.quiz != null && stage.quiz.Count(type) > 0;

        /// <summary>Validates, scores and stores a quiz. Returns null if the answers are incomplete (nothing is saved).</summary>
        public static QuizOutcome Submit(ResultsDatabase db, string player, StageData stage, string type, int[] answers)
        {
            QuizData quiz = stage.quiz;
            if (!IsComplete(answers, quiz, type)) return null;
            QuizQuestion[] questions = quiz.Questions(type);
            int score = Score(answers, questions);
            QuizRecord stored = db.SaveQuiz(player, stage.stageId, type, FormatAnswers(answers), score, quiz.ScoredCount(type), out bool retake);
            QuizOutcome o = Build(db, player, stage, type, stored != null ? ParseAnswers(stored.Answers) : answers);
            o.Score = score;
            o.KeptScore = stored != null ? stored.Score : score;
            o.IsRetake = retake;
            o.Saved = stored != null;
            if (type == Post) o.Gain = Gain(o.PreScore, o.KeptScore, o.MaxScore);
            return o;
        }

        /// <summary>The stored post-quiz outcome (for the Stage Complete screen), or null if it was not taken.</summary>
        public static QuizOutcome LoadPost(ResultsDatabase db, string player, StageData stage)
        {
            if (!Ready(stage, Post)) return null;
            QuizRecord post = db.GetQuiz(player, stage.stageId, Post);
            if (post == null) return null;
            QuizOutcome o = Build(db, player, stage, Post, ParseAnswers(post.Answers));
            o.Score = o.KeptScore = post.Score;
            o.Saved = true;
            o.Gain = Gain(o.PreScore, post.Score, o.MaxScore);
            return o;
        }

        /// <summary>Per-topic comparison: the stored pre answers against the pre questions, these answers against the post questions.</summary>
        private static QuizOutcome Build(ResultsDatabase db, string player, StageData stage, string type, int[] answers)
        {
            QuizData quiz = stage.quiz;
            var o = new QuizOutcome { Type = type, MaxScore = quiz.ScoredCount(type) };
            if (type != Post) return o;

            QuizRecord preRecord = db.GetQuiz(player, stage.stageId, Pre);
            o.PreScore = preRecord?.Score;
            bool[] post = Correct(answers, quiz.postQuestions);
            bool[] pre = preRecord != null ? Correct(ParseAnswers(preRecord.Answers), quiz.preQuestions) : null;

            var topics = new List<TopicResult>();
            foreach (QuizTopic t in quiz.topics)
            {
                var r = new TopicResult { TopicId = t.id, TopicName = t.name, ReviewFact = FactFor(stage, t.id)?.title };
                for (int i = 0; i < quiz.postQuestions.Length; i++)
                    if (quiz.postQuestions[i].topicId == t.id)
                    {
                        r.PostTotal++;
                        if (post[i]) r.PostCorrect++;
                        if (!quiz.postQuestions[i].scored) r.Scored = false;
                    }
                if (pre != null)
                    for (int i = 0; i < quiz.preQuestions.Length; i++)
                        if (quiz.preQuestions[i].topicId == t.id) { r.PreTotal++; if (pre[i]) r.PreCorrect++; }
                if (r.PostTotal > 0 || r.PreTotal > 0) topics.Add(r);
            }
            o.Topics = topics.ToArray();
            return o;
        }

        /// <summary>The first fact of the stage that teaches this topic.</summary>
        public static PopupData FactFor(StageData stage, string topicId)
        {
            if (stage?.facts == null || string.IsNullOrEmpty(topicId)) return null;
            foreach (PopupData f in stage.facts)
                if (f != null && f.topicId == topicId) return f;
            return null;
        }
    }
}
