using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// The Baguio level path: 10 levels in a line; beating a level unlocks the next.
    /// The side panel shows the selected level (waves, Oxygen, best temperature, threat, enemies) and Play.
    /// Progress comes from the level_progress table; a saved game turns Play into Continue.
    /// </summary>
    public class LevelSelectPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _factsButton;
        [SerializeField] private Text _title;
        [SerializeField] private Text _subtitle;
        [SerializeField] private LevelNode[] _nodes = new LevelNode[0];
        [Tooltip("Road segment i joins node i and node i+1; gold once level i is cleared.")]
        [SerializeField] private Image[] _segments = new Image[0];

        [Header("Detail panel")]
        [SerializeField] private Image _detailBorder;
        [SerializeField] private Image _tagBack;
        [SerializeField] private Text _tag;
        [SerializeField] private Text _status;
        [SerializeField] private Image[] _detailStars = new Image[3];
        [SerializeField] private Text _starHint;
        [SerializeField] private Text _name;
        [SerializeField] private Text _description;
        [SerializeField] private Text _waves;
        [SerializeField] private Text _oxygen;
        [SerializeField] private Text _best;
        [SerializeField] private Text _threatLabel;
        [SerializeField] private Image[] _threatBars = new Image[10];
        [SerializeField] private GameObject[] _enemySlots = new GameObject[4];
        [SerializeField] private Image[] _enemyIcons = new Image[4];
        [SerializeField] private Image[] _enemyRings = new Image[4];
        [SerializeField] private Text[] _enemyNames = new Text[4];
        [SerializeField] private Text _note;
        [SerializeField] private Text _squad;
        [SerializeField] private Text _saveNote;
        [SerializeField] private Button _playButton;
        [SerializeField] private Text _playLabel;
        [SerializeField] private GameObject _lockedBox;
        [SerializeField] private Text _lockedText;

        private MainMenuController _menu;
        private readonly HashSet<int> _cleared = new HashSet<int>();
        private readonly Dictionary<int, LevelProgress> _progress = new Dictionary<int, LevelProgress>();
        private int _selected = 1;

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

        private void Awake()
        {
            _backButton.onClick.AddListener(() => _menu.ShowStageSelect());
            if (_factsButton != null) _factsButton.onClick.AddListener(() => _menu.OpenFacts());
            foreach (LevelNode node in _nodes)
            {
                LevelNode n = node;
                n.Button.onClick.AddListener(() => Select(n.LevelNumber));
            }
            _playButton.onClick.AddListener(OnPlay);
        }

        public void Show(MainMenuController menu, int selectLevel)
        {
            _menu = menu;
            _cleared.Clear();
            _progress.Clear();
            foreach (LevelProgress p in menu.Database.GetProgress(menu.PlayerName, menu.Stage.stageId))
            {
                _progress[p.LevelNumber] = p;
                if (p.IsCleared == 1) _cleared.Add(p.LevelNumber);
            }
            _title.text = $"STAGE {menu.Stage.stageId} · {menu.Stage.stageName.ToUpperInvariant()}";
            _subtitle.text = $"Beat a level to open the next one · {_cleared.Count} of {menu.Stage.LevelCount} cleared";
            for (int i = 0; i < _segments.Length; i++)
                _segments[i].color = _cleared.Contains(i + 1) ? Hex("#E2AE2C") : new Color(0, 0, 0, 0);

            SessionSnapshot save = menu.SavedGame;
            int pick = selectLevel > 0 && IsUnlocked(selectLevel) ? selectLevel
                     : save != null ? save.levelNumber
                     : CurrentLevel();
            _root.SetActive(true);
            Select(pick);
        }

        public void Hide() => _root.SetActive(false);

        private bool IsUnlocked(int level) => level == 1 || _cleared.Contains(level - 1);

        /// <summary>The first level not cleared yet (the red "next to play" node).</summary>
        private int CurrentLevel()
        {
            for (int l = 1; l <= _menu.Stage.LevelCount; l++)
                if (!_cleared.Contains(l)) return l;
            return _menu.Stage.LevelCount;
        }

        private void Select(int level)
        {
            _selected = level;
            int current = CurrentLevel();
            foreach (LevelNode node in _nodes)
            {
                LevelData d = _menu.Stage.GetLevel(node.LevelNumber);
                var state = _cleared.Contains(node.LevelNumber) ? LevelNode.NodeState.Cleared
                          : IsUnlocked(node.LevelNumber) ? LevelNode.NodeState.Current
                          : LevelNode.NodeState.Locked;
                if (state == LevelNode.NodeState.Current && node.LevelNumber != current) state = LevelNode.NodeState.Cleared;
                _progress.TryGetValue(node.LevelNumber, out LevelProgress np);
                node.Set(state, node.LevelNumber == level, d != null ? d.pathCaption : "", StarRating.For(np));
            }
            ShowDetail(_menu.Stage.GetLevel(level));
        }

        private void ShowDetail(LevelData l)
        {
            if (l == null) return;
            bool cleared = _cleared.Contains(l.levelNumber);
            bool unlocked = IsUnlocked(l.levelNumber);
            bool special = l.isBossLevel || l.isEliteSpike;
            Color red = Hex("#A3241C"), green = Hex("#2F5D45");

            _detailBorder.color = special ? red : green;
            _tagBack.color = special ? red : green;
            _tag.text = l.isBossLevel ? "BOSS LEVEL" : l.isEliteSpike ? "ELITE SPIKE" : $"LEVEL {l.levelNumber}";
            _status.text = cleared ? "Cleared" : unlocked ? "Next to play" : "Locked";
            _progress.TryGetValue(l.levelNumber, out LevelProgress lp);
            int stars = StarRating.For(lp);
            for (int i = 0; i < _detailStars.Length; i++)
                if (_detailStars[i] != null) _detailStars[i].color = i < stars ? Hex("#F3C95A") : Hex("#2E3B33");
            if (_starHint != null) _starHint.text = stars == 0 ? $"Stars: keep the temperature at {StarRating.TwoStarAt:0.00} °C or lower for 2, {StarRating.ThreeStarAt:0.00} °C for 3." : StarRating.NextStarHint(stars);
            _name.text = $"LEVEL {l.levelNumber} · {l.locationName.ToUpperInvariant()}";
            _description.text = l.subtitle;
            _waves.text = l.waves.Length.ToString();
            _oxygen.text = l.OxygenSummary();
            _best.text = _progress.TryGetValue(l.levelNumber, out LevelProgress p) && p.BestFinalTemp.HasValue
                ? $"{p.BestFinalTemp.Value:0.00}°" : "—";
            _threatLabel.text = $"{l.threat} / 10";
            for (int i = 0; i < _threatBars.Length; i++)
                _threatBars[i].color = i < l.threat ? (i >= 6 ? Hex("#C0453A") : Hex("#E2AE2C")) : Hex("#22332A");

            List<UnitData> enemies = FeaturedEnemies(l, _enemySlots.Length);
            for (int i = 0; i < _enemySlots.Length; i++)
            {
                bool has = i < enemies.Count;
                _enemySlots[i].SetActive(has);
                if (!has) continue;
                UnitData e = enemies[i];
                _enemyIcons[i].sprite = e.card;
                _enemyIcons[i].color = e.tint;
                _enemyRings[i].color = e is BossData || e.isElite ? Hex("#C0453A") : Hex("#3B5446");
                _enemyNames[i].text = e.displayName;
            }
            _note.text = l.note;
            _squad.text = $"Squad for this level: {l.allyRoster.Length} unit types in the deploy bar";

            SessionSnapshot save = _menu.SavedGame;
            bool savedHere = save != null && save.levelNumber == l.levelNumber;
            _saveNote.gameObject.SetActive(save != null);
            _saveNote.text = savedHere
                ? $"Saved game here: Wave {save.waveNumber}, Turn {save.turnNumber}."
                : save != null ? $"Your saved game is at Level {save.levelNumber}." : "";

            _playButton.gameObject.SetActive(unlocked);
            _lockedBox.SetActive(!unlocked);
            _playLabel.text = savedHere ? "CONTINUE" : cleared ? "REPLAY" : "PLAY";
            _lockedText.text = $"Clear Level {l.levelNumber - 1} to unlock";
        }

        /// <summary>Up to n enemies of the level: the strongest first, the boss last.</summary>
        private static List<UnitData> FeaturedEnemies(LevelData l, int n)
        {
            var unique = new List<UnitData>();
            BossData boss = null;
            foreach (WaveData w in l.waves)
                foreach (UnitData e in w.enemies)
                {
                    if (e == null) continue;
                    if (e is BossData b) { boss = b; continue; }
                    if (!unique.Contains(e)) unique.Add(e);
                }
            unique.Sort((a, b) => (b.maxHp * b.damage).CompareTo(a.maxHp * a.damage));
            int keep = boss != null ? n - 1 : n;
            if (unique.Count > keep) unique.RemoveRange(keep, unique.Count - keep);
            if (boss != null) unique.Add(boss);
            return unique;
        }

        private void OnPlay()
        {
            if (!IsUnlocked(_selected)) return;
            SessionSnapshot save = _menu.SavedGame;
            if (save != null && save.levelNumber == _selected) _menu.PlayLevel(_selected, save);
            else _menu.StartFresh(_selected);
        }
    }
}
