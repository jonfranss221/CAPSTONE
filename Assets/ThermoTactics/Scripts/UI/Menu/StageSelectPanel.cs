using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Stage select: 5 stage cards. Baguio Forest is open from the start; each later stage unlocks
    /// when every level of the previous stage is cleared (stages 2–5 are not built yet).
    /// </summary>
    public class StageSelectPanel : MonoBehaviour
    {
        public const int TotalLevels = 50;

        [SerializeField] private GameObject _root;
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _playerName;
        [SerializeField] private Text _playerProgress;
        [SerializeField] private Button _stage1Button;
        [SerializeField] private Text _stage1Count;
        [SerializeField] private Image _stage1Fill;
        [SerializeField] private Text _stage1Stars;
        [Tooltip("Status text of stages 2–5 (\"Locked. Clear …\").")]
        [SerializeField] private Text[] _lockedTexts = new Text[4];

        private MainMenuController _menu;

        private static readonly string[] PreviousStage = { "Baguio Forest", "Sierra Madre", "Metro Manila", "Batangas" };

        private void Awake()
        {
            _backButton.onClick.AddListener(() => _menu.ShowTitle());
            _stage1Button.onClick.AddListener(() => _menu.ShowLevelSelect());
        }

        public void Show(MainMenuController menu)
        {
            _menu = menu;
            string name = menu.PlayerName;
            int cleared = menu.Database.CountCleared(name, menu.Stage.stageId);
            int total = menu.Database.CountClearedAll(name);
            int levels = Mathf.Max(1, menu.Stage.LevelCount);
            _playerName.text = name;
            _playerProgress.text = $"{total} of {TotalLevels} levels cleared";
            _stage1Count.text = $"{cleared} / {levels}";
            _stage1Fill.fillAmount = cleared / (float)levels;
            if (_stage1Stars != null)
            {
                int stars = 0;
                foreach (LevelProgress p in menu.Database.GetProgress(name, menu.Stage.stageId)) stars += StarRating.For(p);
                _stage1Stars.text = $"{stars} / {levels * StarRating.MaxStars}";
            }

            bool baguioDone = cleared >= levels;
            for (int i = 0; i < _lockedTexts.Length; i++)
            {
                if (_lockedTexts[i] == null) continue;
                if (i == 0 && baguioDone)
                    _lockedTexts[i].text = "Unlocked! Stage 2 is still being built. Coming soon.";
                else
                    _lockedTexts[i].text = i == 0
                        ? $"Locked. Clear all {levels} levels of <b>{PreviousStage[i]}</b>."
                        : $"Locked. Clear <b>{PreviousStage[i]}</b> first.";
            }
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);
    }
}
