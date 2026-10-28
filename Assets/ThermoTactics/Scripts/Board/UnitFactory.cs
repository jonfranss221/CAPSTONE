using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Builds unit GameObjects at runtime: sprite + animator + shadow + health bar.</summary>
    public class UnitFactory : MonoBehaviour
    {
        [SerializeField] private Sprite _shadowSprite;
        [SerializeField] private Sprite _pixelSprite;

        public UnitBase Spawn(UnitData data, int lane, int maxHp)
        {
            bool enemy = data.faction == Faction.Enemy;
            Vector3 pos = IsoBoardLayout.SlotPosition(lane, enemy);
            int order = IsoBoardLayout.SortingOrderFor(pos);

            var root = new GameObject($"{(enemy ? "Enemy" : "Ally")}_{data.displayName}_L{lane}");
            root.transform.position = pos;

            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(root.transform, false);
            shadow.sprite = _shadowSprite;
            shadow.sortingOrder = order - 1;

            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(root.transform, false);
            body.sprite = data.idlePose;
            body.color = data.tint;
            body.flipX = enemy;
            body.sortingOrder = order;
            var animator = body.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = data.animator;

            // Health bar: dark back + coloured fill, both from a 1-unit white pixel sprite (pivot left).
            var bar = new GameObject("HealthBar");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(-0.45f, 1.5f, 0f);
            var back = NewBar(bar.transform, "Back", new Color(0.08f, 0.08f, 0.1f, 0.85f), order + 2, 0.9f, 0.11f);
            var fill = NewBar(bar.transform, "Fill", enemy ? new Color(0.9f, 0.3f, 0.25f) : new Color(0.35f, 0.85f, 0.4f), order + 3, 0.86f, 0.07f);
            fill.transform.localPosition = new Vector3(0.02f, 0f, 0f);
            back.transform.localPosition = Vector3.zero;

            // Status pips just above the health bar (Poison, Burn, Weaken, Slow, Dodge, Synergy); hidden until active.
            var pipRow = new GameObject("StatusPips");
            pipRow.transform.SetParent(root.transform, false);
            pipRow.transform.localPosition = new Vector3(-0.45f, 1.66f, 0f);
            var pips = new SpriteRenderer[6];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = NewBar(pipRow.transform, $"Pip{i}", Color.white, order + 4, 0.12f, 0.12f);
                pips[i].enabled = false;
            }

            UnitBase unit = enemy ? root.AddComponent<EnemyUnit>() : (UnitBase)root.AddComponent<AllyUnit>();
            unit.Init(data, lane, maxHp, body, animator, fill.transform, pips);
            return unit;
        }

        /// <summary>
        /// Builds a boss: one large sprite standing over the enemy half of the board.
        /// It has no world health bar; the HUD boss panel shows its HP.
        /// </summary>
        public BossUnit SpawnBoss(BossData data, int maxHp)
        {
            Vector3 pos = IsoBoardLayout.BoardToWorld(data.boardAnchor.x, data.boardAnchor.y);
            int order = IsoBoardLayout.SortingOrderFor(pos);

            var root = new GameObject($"Boss_{data.displayName}");
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * data.worldScale;

            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localScale = new Vector3(3.2f, 2.2f, 1f);
            shadow.sprite = _shadowSprite;
            shadow.sortingOrder = order - 1;

            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(root.transform, false);
            body.sprite = data.idlePose;
            body.color = data.tint;
            body.flipX = true;   // drawn facing right like every unit; the enemy side faces left
            body.sortingOrder = order;
            var animator = body.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = data.animator;

            var boss = root.AddComponent<BossUnit>();
            boss.Init(data, 0, maxHp, body, animator, null);
            return boss;
        }

        private SpriteRenderer NewBar(Transform parent, string name, Color color, int order, float width, float height)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.sprite = _pixelSprite;
            sr.color = color;
            sr.sortingOrder = order;
            sr.transform.localScale = new Vector3(width, height, 1f);
            return sr;
        }
    }
}
