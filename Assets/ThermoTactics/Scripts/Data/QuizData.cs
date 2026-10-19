using System;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>One multiple-choice question (4 choices). Its topic links it to the other quiz and to the climate facts.</summary>
    [Serializable]
    public class QuizQuestion
    {
        public string questionId = "";
        [Tooltip("Id of one of the quiz's topics (QuizData.topics), shared by pre and post questions and by the facts.")]
        public string topicId = "";
        [TextArea(2, 4)] public string text = "";
        public string[] choices = new string[QuizData.ChoiceCount];
        [Range(0, QuizData.ChoiceCount - 1)] public int correctIndex;
    }

    /// <summary>A topic both quizzes cover, e.g. "Forests & carbon".</summary>
    [Serializable]
    public class QuizTopic
    {
        public string id = "";
        public string name = "";
    }

    /// <summary>
    /// A stage's two quizzes: 5 pre-quiz questions (before the stage) and 5 post-quiz questions (after its last level).
    /// The post questions revisit the pre-quiz topics with deeper application, so learning is compared topic by topic.
    /// </summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Quiz Data", fileName = "QuizData")]
    public class QuizData : ScriptableObject
    {
        public const int QuestionCount = 5;
        public const int ChoiceCount = 4;

        public string quizId = "Q1";
        [Min(1)] public int stageId = 1;
        public QuizTopic[] topics = new QuizTopic[0];
        public QuizQuestion[] preQuestions = new QuizQuestion[0];
        public QuizQuestion[] postQuestions = new QuizQuestion[0];

        public QuizQuestion[] Questions(string type) => type == QuizManager.Post ? postQuestions : preQuestions;

        public int Count(string type) { QuizQuestion[] q = Questions(type); return q != null ? q.Length : 0; }

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
