using System;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// One multiple-choice question (4 choices). Its topic links it to its twin in the other quiz and to the climate facts.
    /// Unscored questions (game comprehension) are asked and stored but never count toward the awareness score.
    /// </summary>
    [Serializable]
    public class QuizQuestion
    {
        public string questionId = "";
        [Tooltip("Id of one of the quiz's topics (QuizData.topics), shared by pre and post questions and by the facts.")]
        public string topicId = "";
        [TextArea(2, 4)] public string text = "";
        public string[] choices = new string[QuizData.ChoiceCount];
        [Range(0, QuizData.ChoiceCount - 1)] public int correctIndex;
        [Tooltip("Off for game comprehension questions: asked after the post-quiz pairs, reported apart, not in the score.")]
        public bool scored = true;
    }

    /// <summary>A topic both quizzes cover, e.g. "Forests & carbon".</summary>
    [Serializable]
    public class QuizTopic
    {
        public string id = "";
        public string name = "";
    }

    /// <summary>
    /// A stage's two quizzes as matched pairs: one pre question (before the stage) and one post twin (after its last level)
    /// per concept, both tagged with the concept's topic, so learning is compared concept by concept. The post-quiz may end
    /// with unscored game comprehension questions.
    /// </summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Quiz Data", fileName = "QuizData")]
    public class QuizData : ScriptableObject
    {
        /// <summary>Most questions one quiz can show (progress dots on the quiz screen).</summary>
        public const int MaxQuestions = 8;
        /// <summary>Most topics in the before → after table of the result screen.</summary>
        public const int MaxTopics = 7;
        public const int ChoiceCount = 4;

        public string quizId = "Q1";
        [Min(1)] public int stageId = 1;
        public QuizTopic[] topics = new QuizTopic[0];
        public QuizQuestion[] preQuestions = new QuizQuestion[0];
        public QuizQuestion[] postQuestions = new QuizQuestion[0];

        public QuizQuestion[] Questions(string type) => type == QuizManager.Post ? postQuestions : preQuestions;

        public int Count(string type) { QuizQuestion[] q = Questions(type); return q != null ? q.Length : 0; }

        /// <summary>Questions that count toward the awareness score (the matched pairs).</summary>
        public int ScoredCount(string type)
        {
            int n = 0;
            QuizQuestion[] q = Questions(type);
            if (q != null) foreach (QuizQuestion x in q) if (x != null && x.scored) n++;
            return n;
        }

        public string TopicName(string topicId)
        {
            if (topics != null)
                foreach (QuizTopic t in topics)
                    if (t != null && t.id == topicId) return t.name;
            return string.IsNullOrEmpty(topicId) ? "Climate" : topicId;
        }

        public int TopicIndex(string topicId)
        {
            if (topics != null)
                for (int i = 0; i < topics.Length; i++)
                    if (topics[i] != null && topics[i].id == topicId) return i;
            return 99;
        }
    }
}
