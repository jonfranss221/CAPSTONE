using System;
using System.Collections;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Shared stats, health and animation for every unit.</summary>
    public abstract class UnitBase : MonoBehaviour
    {
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private static readonly int HurtTrigger = Animator.StringToHash("Hurt");

        public UnitData Data { get; private set; }
        public int Lane { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead => Hp <= 0;
        public Faction Faction => Data != null ? Data.faction : Faction.Ally;
        public int Damage => Data != null ? Data.damage : 0;

        private SpriteRenderer _body;
        private Animator _animator;
        private Transform _hpFill;
        private float _hpFillWidth;

        public void Init(UnitData data, int lane, int maxHp, SpriteRenderer body, Animator animator, Transform hpFill)
        {
            Data = data;
            Lane = lane;
            MaxHp = Mathf.Max(1, maxHp);
            Hp = MaxHp;
            _body = body;
            _animator = animator;
            _hpFill = hpFill;
            _hpFillWidth = hpFill != null ? hpFill.localScale.x : 1f;
            UpdateHealthBar();
        }

        /// <summary>Plays the attack animation and calls onHit on the frame the blow lands.</summary>
        public IEnumerator AttackRoutine(Action onHit)
        {
            if (_animator != null) _animator.SetTrigger(AttackTrigger);
            yield return new WaitForSeconds(Data.hitDelay);
            onHit?.Invoke();
            yield return new WaitForSeconds(Mathf.Max(0f, Data.attackDuration - Data.hitDelay));
        }

        /// <summary>Applies damage, plays the hurt animation, and fades out if HP reaches 0.</summary>
        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            Hp = Mathf.Max(0, Hp - Mathf.Max(0, amount));
            UpdateHealthBar();
            if (_animator != null) _animator.SetTrigger(HurtTrigger);
            if (IsDead) StartCoroutine(DieRoutine());
        }

        private IEnumerator DieRoutine()
        {
            yield return new WaitForSeconds(0.45f);
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            for (float t = 0f; t < 0.45f; t += Time.deltaTime)
            {
                float a = 1f - t / 0.45f;
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    Color c = r.color; c.a = Mathf.Min(c.a, a); r.color = c;
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        private void UpdateHealthBar()
        {
            if (_hpFill == null) return;
            Vector3 s = _hpFill.localScale;
            s.x = _hpFillWidth * Hp / (float)MaxHp;
            _hpFill.localScale = s;
        }

        /// <summary>World position slightly above the unit, for floating text.</summary>
        public Vector3 TopPosition => transform.position + Vector3.up * 1.35f;
    }
}
