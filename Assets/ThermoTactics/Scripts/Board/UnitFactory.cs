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

            UnitBase unit = enemy ? root.AddComponent<EnemyUnit>() : (UnitBase)root.AddComponent<AllyUnit>();
            unit.Init(data, lane, maxHp, body, animator, fill.transform);
            return unit;
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
