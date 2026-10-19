using System.Collections.Generic;
using System.Linq;

namespace ThermoTactics
{
    /// <summary>One quiz topic in the pre → post comparison (questions right / asked, in each quiz).</summary>
    public class TopicResult
    {
        public string TopicId;
        public string TopicName;
        public int PreCorrect, PreTotal;     // PreTotal 0: no pre-quiz on record (or no pre question on this topic)
        public int PostCorrect, PostTotal;
        public string ReviewFact;            // title of a fact that teaches this topic (for "review" hints)

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
        public int MaxScore;
        public bool IsRetake;
        public bool Saved;
        public int? PreScore;        // post-quiz: the player's pre-quiz score for the stage
        public float? Gain;          // post-quiz: ((post − pre) / pre) × 100, null when pre is missing or 0
        public TopicResult[] Topics = new TopicResult[0];   // post-quiz: one row per topic
        public int ImprovedCount => Topics.Count(t => t.Improved);
    }

    /// <summary>
    /// Scores the pre- and post-quizzes and compares them (Parts II and IV, Process 2). The two quizzes have
    /// different questions on the same topics, so the comparison is per topic: questions right before vs after.
    /// Gain is computed from the total scores, never stored.
    /// </summary>
    public static class QuizManager
    {
        public const string Pre = "pre";
        public const string Post = "post";
        public const float GainGoal = 30f;

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

        public static int Score(int[] answers, QuizQuestion[] questions) => Correct(answers, questions).Count(c => c);

        public static string FormatAnswers(int[] answers) => string.Join(",", answers);

        /// <summary>"2,0,1,3,1" → {2,0,1,3,1}; bad entries become -1.</summary>
        public static int[] ParseAnswers(string s)
        {
            if (string.IsNullOrEmpty(s)) return new int[0];
            return s.Split(',').Select(p => int.TryParse(p.Trim(), out int v) ? v : -1).ToArray();
        }

        public static float? Gain(int? pre, int? post)
        {
            if (!pre.HasValue || !post.HasValue || pre.Value <= 0) return null;
            return (post.Value - pre.Value) / (float)pre.Value * 100f;
        }

        public static string GainText(float? gain) => gain.HasValue ? $"{(gain.Value >= 0 ? "+" : "")}{gain.Value:0.0}%" : "N/A";

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
            QuizRecord stored = db.SaveQuiz(player, stage.stageId, type, FormatAnswers(answers), score, questions.Length, out bool retake);
            QuizOutcome o = Build(db, player, stage, type, stored != null ? ParseAnswers(stored.Answers) : answers);
            o.Score = score;
            o.KeptScore = stored != null ? stored.Score : score;
            o.IsRetake = retake;
            o.Saved = stored != null;
            if (type == Post) o.Gain = Gain(o.PreScore, o.KeptScore);
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
            o.Gain = Gain(o.PreScore, post.Score);
            return o;
        }

        /// <summary>Per-topic comparison: the stored pre answers against the pre questions, these answers against the post questions.</summary>
        private static QuizOutcome Build(ResultsDatabase db, string player, StageData stage, string type, int[] answers)
        {
            QuizData quiz = stage.quiz;
            var o = new QuizOutcome { Type = type, MaxScore = quiz.Count(type) };
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
                    if (quiz.postQuestions[i].topicId == t.id) { r.PostTotal++; if (post[i]) r.PostCorrect++; }
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
