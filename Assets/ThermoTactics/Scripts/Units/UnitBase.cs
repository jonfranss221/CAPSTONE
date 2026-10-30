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
        /// <summary>
        /// True for an ally placed during the current player turn. Until END TURN it can be moved for free,
        /// swapped for another unit or taken back with a full Oxygen refund (see DeploySystem).
        /// </summary>
        public bool DeployedThisTurn { get; set; }
        public Faction Faction => Data != null ? Data.faction : Faction.Ally;
        /// <summary>Temporary effects (Poison, Burn, Weaken, Slow, Dodge) and trait counters.</summary>
        public StatusEffects Status { get; private set; } = new StatusEffects();
        /// <summary>Bonuses from neighbouring units (pair synergies, Canopy). Set by GridManager.</summary>
        public SynergyBonus Synergy { get; private set; } = new SynergyBonus();

        /// <summary>Attack damage: base + synergy + Overcharge (+1 per turn survived, up to the trait value).</summary>
        public int Damage
        {
            get
            {
                if (Data == null) return 0;
                int overcharge = Data.trait == UnitTrait.Overcharge ? Mathf.Min(Status.turnsAlive, Mathf.RoundToInt(Data.traitValue)) : 0;
                return Data.damage + Synergy.Damage + overcharge;
            }
        }

        /// <summary>Who attacks first in a lane (1–10): base + synergy − Slow (Floodgate is immune to Speed debuffs).</summary>
        public int Speed
        {
            get
            {
                if (Data == null) return 1;
                int slow = Data.trait == UnitTrait.Floodgate ? 0 : Status.ActiveSlow;
                return Mathf.Clamp(Data.speed + Synergy.Speed - slow, 1, 10);
            }
        }

        /// <summary>Damage removed from this unit's attacks while Weakened.</summary>
        public int WeakenAmount => Status.ActiveWeaken;

        /// <summary>Extra "damage taken −N" from neighbours (Baseload Power, Garbage Patch, Understory, Canopy).</summary>
        public int ExtraArmour => Synergy.Armour;

        /// <summary>Raised after this unit takes damage (the boss panel listens to it).</summary>
        public event Action<UnitBase> Damaged;

        protected Animator Anim => _animator;
        protected SpriteRenderer Body => _body;

        private SpriteRenderer _body;
        private Animator _animator;
        private Transform _hpFill;
        private float _hpFillWidth;
        private SpriteRenderer[] _statusPips;

        // Pip colours, in order: Poison, Burn, Weaken, Slow, Dodge, Synergy.
        private static readonly Color[] PipColors =
        {
            new Color(0.45f, 0.9f, 0.3f), new Color(1f, 0.55f, 0.15f), new Color(0.7f, 0.45f, 1f),
            new Color(0.4f, 0.7f, 1f), new Color(0.95f, 0.95f, 0.95f), new Color(1f, 0.82f, 0.25f),
        };

        public void Init(UnitData data, int lane, int maxHp, SpriteRenderer body, Animator animator, Transform hpFill, SpriteRenderer[] statusPips = null)
        {
            Data = data;
            Lane = lane;
            MaxHp = Mathf.Max(1, maxHp);
            Hp = MaxHp;
            _body = body;
            _animator = animator;
            _hpFill = hpFill;
            _hpFillWidth = hpFill != null ? hpFill.localScale.x : 1f;
            _statusPips = statusPips;
            Status = new StatusEffects { dodge = data != null && data.trait == UnitTrait.NightEyes };
            UpdateHealthBar();
            RefreshStatusIcons();
        }

        /// <summary>Moves the unit to another lane of its side: position, draw order and name (GridManager keeps the slot table).</summary>
        public void MoveToLane(int lane)
        {
            if (lane == Lane) return;
            bool enemy = Faction == Faction.Enemy;
            int oldOrder = IsoBoardLayout.SortingOrderFor(transform.position);
            Vector3 pos = IsoBoardLayout.SlotPosition(lane, enemy);
            int delta = IsoBoardLayout.SortingOrderFor(pos) - oldOrder;
            transform.position = pos;
            foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder += delta;
            gameObject.name = $"{(enemy ? "Enemy" : "Ally")}_{Data.displayName}_L{lane}";
            Lane = lane;
        }

        /// <summary>Puts back the effects of a saved game.</summary>
        public void RestoreStatus(StatusEffects saved)
        {
            if (saved != null) Status = saved.Copy();
            RefreshStatusIcons();
        }

        public void SetSynergy(SynergyBonus bonus)
        {
            Synergy = bonus ?? new SynergyBonus();
            RefreshStatusIcons();
        }

        /// <summary>A new wave: Dive's first-attack bonus resets and Night Eyes can dodge again.</summary>
        public void OnWaveStart()
        {
            Status.attackedThisWave = false;
            if (Data != null && Data.trait == UnitTrait.NightEyes) Status.dodge = true;
            RefreshStatusIcons();
        }

        /// <summary>Heals up to max HP. Returns the HP actually restored.</summary>
        public int Heal(int amount)
        {
            if (IsDead || amount <= 0) return 0;
            int before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            UpdateHealthBar();
            OnHpChanged();
            return Hp - before;
        }

        /// <summary>Shows a small coloured pip above the health bar for each effect on the unit.</summary>
        public void RefreshStatusIcons()
        {
            if (_statusPips == null) return;
            bool[] on = { Status.Poisoned, Status.Burning, Status.Weakened, Status.Slowed, Status.dodge, Synergy.Any };
            int shown = 0;
            for (int i = 0; i < _statusPips.Length && i < on.Length; i++)
            {
                if (_statusPips[i] == null) continue;
                _statusPips[i].enabled = on[i];
                if (!on[i]) continue;
                _statusPips[i].color = PipColors[i];
                _statusPips[i].transform.localPosition = new Vector3(shown * 0.16f, 0f, 0f);
                shown++;
            }
        }

        /// <summary>Restores HP from a saved game (never above max, at least 1).</summary>
        public void SetHp(int hp)
        {
            Hp = Mathf.Clamp(hp, 1, MaxHp);
            UpdateHealthBar();
            OnHpChanged();
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
            OnHpChanged();
            Damaged?.Invoke(this);
            if (IsDead) StartCoroutine(DieRoutine());
        }

        /// <summary>Called whenever HP changes (the boss uses it to enrage).</summary>
        protected virtual void OnHpChanged() { }

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
