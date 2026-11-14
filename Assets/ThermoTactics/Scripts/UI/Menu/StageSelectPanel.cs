using System;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Stage select: 5 stage cards. Baguio Forest is open from the start; each later stage unlocks
    /// when every level of the previous stage is cleared. Stages that are not built yet stay locked
    /// ("coming soon" once the stage before them is cleared).
    /// </summary>
    public class StageSelectPanel : MonoBehaviour
    {
        public const int TotalLevels = 50;

        /// <summary>One stage card. Cards of stages that are not built only have the locked parts.</summary>
        [Serializable]
        public class Card
        {
            public int stageId = 1;
            public Button button;
            public Text count;
            public Image fill;
            public Text stars;
            [Tooltip("Shown while the stage is open: PLAY button.")]
            public GameObject openGroup;
            [Tooltip("Shown while the stage is locked: lock icon, dimmed thumbnail and the locked note.")]
            public GameObject lockedGroup;
            public Text lockedText;
            public Image border;
            public Image tag;
            public Text tagText;
        }

        [SerializeField] private GameObject _root;
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _playerName;
        [SerializeField] private Text _playerProgress;
        [SerializeField] private Card[] _cards = new Card[0];

        private MainMenuController _menu;

        private static readonly string[] StageNames = { "Baguio Forest", "Sierra Madre Coast", "Metro Manila", "Batangas", "Boracay & Palawan" };

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

        private void Awake()
        {
            _backButton.onClick.AddListener(() => _menu.ShowTitle());
            foreach (Card card in _cards)
            {
                if (card.button == null) continue;
                int id = card.stageId;
                card.button.onClick.AddListener(() => _menu.EnterStage(_menu.FindStage(id)));
            }
        }

        private string NameOf(int stageId)
        {
            StageData s = _menu.FindStage(stageId);
            if (s != null) return s.stageName;
            return stageId >= 1 && stageId <= StageNames.Length ? StageNames[stageId - 1] : $"Stage {stageId}";
        }

        public void Show(MainMenuController menu)
        {
            _menu = menu;
            string name = menu.PlayerName;
            _playerName.text = name;
            _playerProgress.text = $"{menu.Database.CountClearedAll(name)} of {TotalLevels} levels cleared";

            foreach (Card card in _cards)
            {
                StageData stage = menu.FindStage(card.stageId);
                bool open = stage != null && menu.IsStageOpen(card.stageId);
                StageData prev = menu.FindStage(card.stageId - 1);
                bool prevDone = card.stageId <= 1 || (prev != null && menu.IsStageDone(prev));

                if (stage != null)
                {
                    int cleared = menu.Database.CountCleared(name, stage.stageId);
                    int levels = Mathf.Max(1, stage.LevelCount);
                    if (card.count != null) card.count.text = $"{cleared} / {levels}";
                    if (card.fill != null) card.fill.fillAmount = cleared / (float)levels;
                    if (card.stars != null)
                    {
                        int stars = 0;
                        foreach (LevelProgress p in menu.Database.GetProgress(name, stage.stageId)) stars += StarRating.For(p);
                        card.stars.text = $"{stars} / {levels * StarRating.MaxStars}";
                    }
                }
                if (card.button != null) card.button.interactable = open;
                if (card.openGroup != null) card.openGroup.SetActive(open);
                if (card.lockedGroup != null) card.lockedGroup.SetActive(!open);
                if (card.border != null) card.border.color = open ? Hex("#E2AE2C") : Hex("#33423B");
                if (card.tag != null) card.tag.color = open ? Hex("#E2AE2C") : Hex("#33423B");
                if (card.tagText != null) card.tagText.color = open ? Hex("#1A140A") : Hex("#E7DFCC");

                if (card.lockedText != null && !open)
                {
                    if (stage == null && prevDone)
                        card.lockedText.text = $"Unlocked! Stage {card.stageId} is still being built. Coming soon.";
                    else if (prev != null || card.stageId == 2)
                        card.lockedText.text = $"Locked. Clear all {(prev != null ? prev.LevelCount : 10)} levels of <b>{NameOf(card.stageId - 1)}</b>.";
                    else
                        card.lockedText.text = $"Locked. Clear <b>{NameOf(card.stageId - 1)}</b> first.";
                }
            }
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);
    }
}
