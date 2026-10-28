using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Squad pick before a level: the player chooses up to 5 of the allies they own (the starters plus the ones bought
    /// in the store) for the deploy bar. It opens with the last squad used (PlayerPrefs tt.squad.&lt;name&gt;), or the
    /// starters. Tapping a tile adds or removes it; the line under the grid explains the last ally tapped.
    /// </summary>
    public class SquadPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _subtitle;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private GameObject _tileTemplate;
        [SerializeField] private Text _count;
        [SerializeField] private Text _info;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _defaultButton;

        private readonly List<GameObject> _tiles = new List<GameObject>();
        private readonly List<UnitData> _picked = new List<UnitData>();
        private List<UnitData> _available = new List<UnitData>();
        private Action<UnitData[]> _onStart;
        private LevelData _level;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            _cancelButton.onClick.AddListener(Hide);
            _startButton.onClick.AddListener(OnStart);
            if (_defaultButton != null) _defaultButton.onClick.AddListener(UseStarters);
            _tileTemplate.SetActive(false);
            _root.SetActive(false);
        }

        public void Show(MainMenuController menu, int levelNumber, Action<UnitData[]> onStart)
        {
            _onStart = onStart;
            _level = menu.Stage.GetLevel(levelNumber);
            HashSet<string> owned = menu.Database.GetOwned(menu.PlayerName);
            _available = PlayerSquad.Available(menu.Stage, owned);
            _picked.Clear();
            _picked.AddRange(PlayerSquad.Load(menu.PlayerName, menu.Stage, owned, _level));

            foreach (GameObject t in _tiles) Destroy(t);
            _tiles.Clear();
            foreach (UnitData u in _available)
            {
                GameObject tile = Instantiate(_tileTemplate, _grid);
                tile.SetActive(true);
                tile.name = $"Squad_{u.unitId}";
                Set<Image>(tile, "Card", i => i.sprite = u.card);
                Set<Image>(tile, "Stripe", i => i.color = StorePanel.TierColor(u.tier));
                Set<Text>(tile, "Name", t => t.text = u.displayName);
                Set<Text>(tile, "Stats", t => t.text = $"{u.opCost} OP · Spd {u.speed}");
                UnitData captured = u;
                tile.GetComponent<Button>().onClick.AddListener(() => Toggle(captured));
                _tiles.Add(tile);
            }
            string where = _level != null ? $"Level {levelNumber} · {_level.locationName}" : $"Level {levelNumber}";
            _subtitle.text = $"{where}. Pick up to {PlayerSquad.MaxSize} allies for the deploy bar. Buy more in the Store.";
            _info.text = "Tap an ally to add or remove it. The order you pick is the order in the deploy bar.";
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Hide() => _root.SetActive(false);

        private void Toggle(UnitData u)
        {
            if (_picked.Contains(u)) _picked.Remove(u);
            else if (_picked.Count < PlayerSquad.MaxSize) _picked.Add(u);
            else
            {
                _info.text = $"<color=#FF9A86>Your squad is full ({PlayerSquad.MaxSize}).</color> Tap an ally in the squad to remove it first.";
                Refresh();
                return;
            }
            _info.text = $"<b>{u.displayName}</b> · {UnitCategoryInfo.Label(u.category)} · {u.maxHp} HP · {u.damage} dmg · Speed {u.speed}. {u.traitText}";
            Refresh();
        }

        private void UseStarters()
        {
            _picked.Clear();
            _picked.AddRange(PlayerSquad.Default(_level));
            _info.text = "Back to the 5 starting defenders.";
            Refresh();
        }

        private void Refresh()
        {
            foreach (GameObject t in _tiles)
            {
                UnitData u = _available.Find(x => t.name == $"Squad_{x.unitId}");
                int index = u != null ? _picked.IndexOf(u) : -1;
                Transform sel = t.transform.Find("Selected");
                if (sel != null) sel.gameObject.SetActive(index >= 0);
                Set<Text>(t, "Order", x => x.text = index >= 0 ? (index + 1).ToString() : "");
            }
            _count.text = $"Squad {_picked.Count} / {PlayerSquad.MaxSize}";
            _startButton.interactable = _picked.Count > 0;
        }

        private void OnStart()
        {
            if (_picked.Count == 0) return;
            UnitData[] squad = _picked.ToArray();
            Hide();
            _onStart?.Invoke(squad);
        }

        private static void Set<T>(GameObject root, string child, Action<T> apply) where T : Component
        {
            Transform t = root.transform.Find(child);
            T c = t != null ? t.GetComponent<T>() : null;
            if (c != null) apply(c);
        }
    }
}
