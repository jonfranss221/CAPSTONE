using System;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// The Pre-Quiz / Post-Quiz screen (Main Menu and battle scene). One question per page, 4 choices,
    /// Back / Next, Submit only when all 5 are answered. Answers are not marked during the quiz.
    /// The result page shows the score; after the post-quiz it compares every topic with the pre-quiz
    /// (before → after) and shows the knowledge gain.
    /// </summary>
    public class QuizPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _intro;

        [Header("Question page")]
        [SerializeField] private GameObject _questionPage;
        [SerializeField] private Text _eyebrow;
        [SerializeField] private Text _question;
        [SerializeField] private Button[] _choices = new Button[QuizData.ChoiceCount];
        [SerializeField] private Text[] _choiceLabels = new Text[QuizData.ChoiceCount];
        [SerializeField] private Text[] _choiceLetters = new Text[QuizData.ChoiceCount];
        [SerializeField] private Image[] _dots = new Image[QuizData.QuestionCount];
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _nextLabel;
        [SerializeField] private Text _message;

        [Header("Result page")]
        [SerializeField] private GameObject _resultPage;
        [SerializeField] private Text _resultScore;
        [SerializeField] private Text _resultBody;
        [SerializeField] private GameObject _compareTable;
        [SerializeField] private Text[] _rowTopic = new Text[QuizData.QuestionCount];
        [SerializeField] private Text[] _rowBefore = new Text[QuizData.QuestionCount];
        [SerializeField] private Text[] _rowAfter = new Text[QuizData.QuestionCount];
        [SerializeField] private Text[] _rowStatus = new Text[QuizData.QuestionCount];
        [SerializeField] private Text _gain;
        [SerializeField] private Button _continueButton;

        private static readonly Color Gold = new Color(0.886f, 0.682f, 0.173f);
        private static readonly Color Cream = new Color(0.953f, 0.918f, 0.843f);
        private static readonly Color Ink = new Color(0.102f, 0.078f, 0.039f);
        private static readonly Color Choice = new Color(0.078f, 0.11f, 0.09f);
        private static readonly Color DotOff = new Color(0.165f, 0.227f, 0.188f);
        private static readonly Color DotDone = new Color(0.184f, 0.365f, 0.27f);

        private StageData _stage;
        private string _type;
        private string _player;
        private ResultsDatabase _db;
        private Action<QuizOutcome> _onDone;
        private QuizOutcome _outcome;
        private int[] _answers;
        private int _page;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            for (int i = 0; i < _choices.Length; i++)
            {
                int index = i;
                _choices[i].onClick.AddListener(() => Choose(index));
            }
            _backButton.onClick.AddListener(() => ShowPage(_page - 1));
            _nextButton.onClick.AddListener(OnNext);
            _continueButton.onClick.AddListener(Finish);
            _root.SetActive(false);
        }

        public void Show(StageData stage, string type, string player, ResultsDatabase db, Action<QuizOutcome> onDone)
        {
            _stage = stage; _type = type; _player = player; _db = db; _onDone = onDone; _outcome = null;
            int n = stage.quiz.Count(type);
            _answers = new int[n];
            for (int i = 0; i < n; i++) _answers[i] = -1;
            for (int i = 0; i < _dots.Length; i++) _dots[i].gameObject.SetActive(i < n);

            bool post = type == QuizManager.Post;
            _heading.text = post ? "POST-QUIZ" : "PRE-QUIZ";
            _intro.text = post
                ? $"You cleared {stage.stageName}! {n} questions on the same topics as the pre-quiz. Let's see what you learned."
                : $"Before you play {stage.stageName}: {n} quick questions. It is not graded, so just pick what you think is right.";
            _questionPage.SetActive(true);
            _resultPage.SetActive(false);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            ShowPage(0);
        }

        private void ShowPage(int page)
        {
            QuizData quiz = _stage.quiz;
            QuizQuestion[] questions = quiz.Questions(_type);
            _page = Mathf.Clamp(page, 0, questions.Length - 1);
            QuizQuestion q = questions[_page];
            _eyebrow.text = $"QUESTION {_page + 1} OF {questions.Length} · {quiz.TopicName(q.topicId).ToUpperInvariant()}";
            _question.text = q.text;
            for (int i = 0; i < _choices.Length; i++)
            {
                bool has = q.choices != null && i < q.choices.Length && !string.IsNullOrEmpty(q.choices[i]);
                _choices[i].gameObject.SetActive(has);
                if (has) _choiceLabels[i].text = q.choices[i];
            }
            _backButton.gameObject.SetActive(_page > 0);
            _message.text = "";
            Refresh();
        }

        private void Choose(int choice)
        {
            _answers[_page] = choice;
            _message.text = "";
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < _choices.Length; i++)
            {
                bool on = _answers[_page] == i;
                _choices[i].GetComponent<Image>().color = on ? Gold : Choice;
                _choiceLabels[i].color = on ? Ink : Cream;
                if (_choiceLetters[i] != null) _choiceLetters[i].color = on ? Ink : Gold;
            }
            for (int i = 0; i < _answers.Length && i < _dots.Length; i++)
                _dots[i].color = i == _page ? Gold : _answers[i] >= 0 ? DotDone : DotOff;

            bool last = _page == _answers.Length - 1;
            _nextLabel.text = last ? "SUBMIT" : "NEXT";
            _nextButton.interactable = last ? QuizManager.IsComplete(_answers, _stage.quiz, _type) : _answers[_page] >= 0;
            if (last && !_nextButton.interactable && _answers[_page] >= 0) _message.text = "Answer all 5 questions.";
        }

        private void OnNext()
        {
            if (_page < _answers.Length - 1) { ShowPage(_page + 1); return; }
            Submit(_answers);
        }

        private void Submit(int[] answers)
        {
            QuizOutcome o = QuizManager.Submit(_db, _player, _stage, _type, answers);
            if (o == null) { _message.text = "Answer all 5 questions."; return; }
            _outcome = o;
            ShowResult(o);
        }

        private void ShowResult(QuizOutcome o)
        {
            _questionPage.SetActive(false);
            _resultPage.SetActive(true);
            bool post = o.Type == QuizManager.Post;
            _resultScore.text = $"{(post ? "Post-quiz" : "Pre-quiz")}: {o.Score} / {o.MaxScore}";

            string note = o.IsRetake ? "<color=#F3C95A>Practice round: your first score is kept.</color>\n" : "";
            if (!o.Saved) note += "<color=#FF9A86>Not saved: the database could not be written.</color>\n";
            if (!post)
            {
                _resultBody.text = note + $"Watch for <b>climate facts</b> while you play {_stage.stageName}. " +
                                   $"After Level {_stage.LevelCount} a post-quiz on the same topics shows what you learned.";
                _compareTable.SetActive(false);
                _gain.text = "";
                return;
            }

            _compareTable.SetActive(true);
            for (int i = 0; i < _rowTopic.Length; i++)
            {
                bool has = i < o.Topics.Length;
                _rowTopic[i].transform.parent.gameObject.SetActive(has);
                if (!has) continue;
                TopicResult t = o.Topics[i];
                _rowTopic[i].text = t.TopicName;
                _rowBefore[i].text = t.HasPre ? $"{t.PreCorrect} / {t.PreTotal}" : "—";
                _rowBefore[i].color = !t.HasPre ? Cream : Tone(t.PreShare);
                _rowAfter[i].text = t.PostTotal > 0 ? $"{t.PostCorrect} / {t.PostTotal}" : "—";
                _rowAfter[i].color = t.PostTotal > 0 ? Tone(t.PostShare) : Cream;
                _rowStatus[i].text = t.Improved ? "<color=#F3C95A><b>Improved!</b></color>"
                    : t.Mastered ? "<color=#9BD3B0>Mastered</color>"
                    : !string.IsNullOrEmpty(t.ReviewFact) ? $"<color=#CFC6B2>Review: {t.ReviewFact}</color>"
                    : "<color=#CFC6B2>Review this topic</color>";
            }
            int improved = o.ImprovedCount;
            _resultBody.text = note + (o.PreScore.HasValue
                ? $"Pre-quiz {o.PreScore.Value} / {o.MaxScore}, post-quiz {o.KeptScore} / {o.MaxScore}. " +
                  (improved > 0 ? $"You improved in <b>{improved} topic{(improved == 1 ? "" : "s")}</b>." : "Your answers are on record.")
                : "No pre-quiz on record, so there is nothing to compare with.");
            _gain.text = o.Gain.HasValue
                ? $"Knowledge gain <b>{QuizManager.GainText(o.Gain)}</b> (goal: at least {QuizManager.GainGoal:0}%)"
                : $"Knowledge gain: N/A{(o.PreScore.HasValue ? "" : " (no pre-quiz)")}";
        }

        /// <summary>Green when all right, red when none, cream in between.</summary>
        private static Color Tone(float share) => share >= 0.999f ? new Color(0.61f, 0.83f, 0.69f) : share <= 0.001f ? new Color(1f, 0.6f, 0.53f) : Cream;

        private void Finish()
        {
            _root.SetActive(false);
            Action<QuizOutcome> done = _onDone;
            _onDone = null;
            done?.Invoke(_outcome);
        }

#if UNITY_EDITOR
        /// <summary>Editor play-tests: submits these answers (or the correct ones) and shows the result page.</summary>
        public void DevSubmit(int[] answers = null)
        {
            if (answers == null)
            {
                QuizQuestion[] questions = _stage.quiz.Questions(_type);
                answers = new int[questions.Length];
                for (int i = 0; i < answers.Length; i++) answers[i] = questions[i].correctIndex;
            }
            Submit(answers);
        }

        public void DevContinue() => Finish();
#endif
    }
}
