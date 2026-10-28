using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// The STORE: the 10 allies that are not starters, bought with Carbon Credits. A tile shows the card, name, tier
    /// and price; the side panel shows the selected ally's stats, category, trait and pair synergies, and the button
    /// for its state: Locked (its stage is not unlocked yet), Buy, Too expensive or Owned. Buying asks first.
    /// A stage opens its allies once the stage before it is fully cleared (Stage 1 allies are open from the start).
    /// </summary>
    public class StorePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Text _wallet;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private GameObject _tileTemplate;
        [SerializeField] private Image _detailCard;
        [SerializeField] private Text _detailName;
        [SerializeField] private Text _detailTier;
        [SerializeField] private Text _detailStats;
        [SerializeField] private Text _detailTrait;
        [SerializeField] private Text _detailState;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Text _buyLabel;
        [SerializeField] private Button _closeButton;

        private MainMenuController _menu;
        private readonly List<GameObject> _tiles = new List<GameObject>();
        private UnitData _selected;

        public static readonly Color CommonColor = new Color(0.8f, 0.78f, 0.7f);
        public static readonly Color RareColor = new Color(0.36f, 0.66f, 0.92f);
        public static readonly Color EpicColor = new Color(0.72f, 0.5f, 0.95f);
        public static readonly Color LegendaryColor = new Color(0.95f, 0.75f, 0.2f);

        public static Color TierColor(UnitTier t) => t == UnitTier.Legendary ? LegendaryColor : t == UnitTier.Epic ? EpicColor : t == UnitTier.Rare ? RareColor : CommonColor;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Hide);
            _buyButton.onClick.AddListener(OnBuy);
            _tileTemplate.SetActive(false);
            _root.SetActive(false);
        }

        public void Show(MainMenuController menu)
        {
            _menu = menu;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Rebuild();
        }

        public void Hide()
        {
            _root.SetActive(false);
            if (_menu != null) _menu.RefreshProfile();
        }

        /// <summary>The highest stage whose allies are on sale: Stage 1, plus one more for each stage fully cleared.</summary>
        public static int UnlockedStage(ResultsDatabase db, string player, StageData stage)
        {
            if (db == null || stage == null) return 1;
            return db.CountCleared(player, stage.stageId) >= stage.LevelCount ? stage.stageId + 1 : stage.stageId;
        }

        /// <summary>The allies sold in the store, cheapest stage first, then by price.</summary>
        public static List<UnitData> ForSale(StageData stage)
        {
            var list = new List<UnitData>();
            foreach (UnitData u in PlayerSquad.AllAllies(stage)) if (!u.isStarter) list.Add(u);
            list.Sort((a, b) => a.unlockStage != b.unlockStage ? a.unlockStage.CompareTo(b.unlockStage)
                              : a.priceCredits != b.priceCredits ? a.priceCredits.CompareTo(b.priceCredits)
                              : string.CompareOrdinal(a.unitId, b.unitId));
            return list;
        }

        private void Rebuild()
        {
            foreach (GameObject t in _tiles) Destroy(t);
            _tiles.Clear();
            string player = _menu.PlayerName;
            ResultsDatabase db = _menu.Database;
            int credits = db.GetCredits(player);
            HashSet<string> owned = db.GetOwned(player);
            int open = UnlockedStage(db, player, _menu.Stage);
            List<UnitData> units = ForSale(_menu.Stage);

            int ownedCount = 0;
            foreach (UnitData u in units)
            {
                bool has = owned.Contains(u.unitId);
                if (has) ownedCount++;
                GameObject tile = Instantiate(_tileTemplate, _grid);
                tile.SetActive(true);
                tile.name = $"Tile_{u.unitId}";
                Find<Image>(tile, "Card").sprite = u.card;
                Find<Image>(tile, "Stripe").color = TierColor(u.tier);
                Find<Text>(tile, "Name").text = u.displayName;
                Find<Text>(tile, "Price").text = has ? "<color=#8FE09B>OWNED</color>"
                    : u.unlockStage > open ? $"<color=#8C8676>Stage {u.unlockStage}</color>"
                    : credits >= u.priceCredits ? $"<color=#C9F5CF>{CreditRules.Format(u.priceCredits)}</color>"
                    : $"<color=#CFC6B2>{CreditRules.Format(u.priceCredits)}</color>";
                Find<Image>(tile, "Card").color = u.unlockStage > open && !has ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
                UnitData captured = u;
                tile.GetComponent<Button>().onClick.AddListener(() => Select(captured));
                _tiles.Add(tile);
            }
            _wallet.text = CreditRules.Format(credits);
            _subtitle.text = $"Carbon Credits buy new defenders. Earn them by clearing levels. {ownedCount} of {units.Count} bought.";
            if (_selected == null || !units.Contains(_selected)) _selected = units.Count > 0 ? units[0] : null;
            Select(_selected);
        }

        private void Select(UnitData u)
        {
            _selected = u;
            foreach (GameObject t in _tiles)
            {
                var border = t.transform.Find("Selected");
                if (border != null) border.gameObject.SetActive(u != null && t.name == $"Tile_{u.unitId}");
            }
            if (u == null) return;

            string player = _menu.PlayerName;
            ResultsDatabase db = _menu.Database;
            int credits = db.GetCredits(player);
            bool has = db.GetOwned(player).Contains(u.unitId);
            bool locked = u.unlockStage > UnlockedStage(db, player, _menu.Stage);

            _detailCard.sprite = u.card;
            _detailName.text = u.displayName.ToUpperInvariant();
            _detailTier.text = $"<color=#{ColorUtility.ToHtmlStringRGB(TierColor(u.tier))}>{u.tier.ToString().ToUpperInvariant()}</color> · {UnitCategoryInfo.Label(u.category)}"
                               + (UnitCategoryInfo.Counters(u.category) is UnitCategory c ? $" · strong vs {UnitCategoryInfo.Label(c)} (×1.5)" : "");
            _detailStats.text = $"<b>{u.opCost}</b> OP   ·   <b>{u.maxHp}</b> HP   ·   <b>{u.damage}</b> damage   ·   Speed <b>{u.speed}</b>";
            _detailTrait.text = $"<b>{u.traitText}</b>" + Pairs(u);

            if (has)
            {
                _detailState.text = "<color=#8FE09B>Owned.</color> Pick it in the squad screen before a level.";
                SetBuy(false, "OWNED");
            }
            else if (locked)
            {
                _detailState.text = $"Unlocks in Stage {u.unlockStage}" + (u.unlockStage == 2 ? ": clear all 10 Baguio levels." : ".");
                SetBuy(false, "LOCKED");
            }
            else if (credits < u.priceCredits)
            {
                _detailState.text = $"You need {CreditRules.Format(u.priceCredits - credits)} more. Clear levels and earn stars to get Carbon Credits.";
                SetBuy(false, CreditRules.Format(u.priceCredits));
            }
            else
            {
                _detailState.text = $"You have {CreditRules.Format(credits)}.";
                SetBuy(true, $"BUY · {CreditRules.Format(u.priceCredits)}");
            }
        }

        /// <summary>The pair synergies this ally is part of, as extra lines.</summary>
        private static string Pairs(UnitData u)
        {
            var lines = new List<string>();
            string id = SynergyRules.BaseId(u);
            foreach (SynergyRules.Pair p in SynergyRules.Pairs)
                if (p.Side == Faction.Ally && (p.A == id || System.Array.IndexOf(p.B, id) >= 0))
                    lines.Add($"<color=#F3C95A>{p.Name}</color>: {p.Text}");
            if (u.trait == UnitTrait.Canopy) lines.Add("<color=#F3C95A>Canopy</color>: allies next to it take 1 less damage.");
            return lines.Count > 0 ? "\n\n" + string.Join("\n", lines) : "";
        }

        private void SetBuy(bool interactable, string label)
        {
            _buyButton.interactable = interactable;
            _buyLabel.text = label;
        }

        private void OnBuy()
        {
            UnitData u = _selected;
            if (u == null) return;
            string player = _menu.PlayerName;
            _menu.Confirm($"BUY {u.displayName.ToUpperInvariant()}?",
                $"Spend {CreditRules.Format(u.priceCredits)} on {u.displayName}? You can then pick it for your squad before any level.",
                "BUY",
                () =>
                {
                    bool ok = _menu.Database.Buy(player, u.unitId, u.priceCredits);
                    Rebuild();
                    _detailState.text = ok ? $"<color=#8FE09B>{u.displayName} joined your defenders!</color> Pick it in the squad screen before a level."
                                           : "<color=#FF9A86>Could not buy it. Check your Carbon Credits and try again.</color>";
                    _root.transform.SetAsLastSibling();
                });
        }

        private static T Find<T>(GameObject root, string child) where T : Component
        {
            Transform t = root.transform.Find(child);
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
