using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Applies lane rules, including the climate result of each attack.
    /// Units only report their attacks; this script alone decides the temperature effect.
    /// Each report returns the °C change it caused, so the lane camera can show the lane's result.
    /// Unit traits that change the amounts (Blue Carbon, Greenhouse) are read from the attacking unit.
    /// </summary>
    public class LaneController : MonoBehaviour
    {
        [SerializeField] private TemperatureSystem _temperature;
        [SerializeField] private UIManager _ui;

        private LevelData _level;
        private bool _cooledThisPhase;

        public void Init(LevelData level) => _level = level;

        /// <summary>Call before each ally phase. Cooling counts at most once per turn.</summary>
        public void BeginAllyPhase() => _cooledThisPhase = false;

        /// <summary>True once an ally has already cooled the planet this turn.</summary>
        public bool CooledThisTurn => _cooledThisPhase;

        /// <summary>
        /// Ally attacked its lane. An empty enemy slot cools the planet, but only once per turn:
        /// otherwise allies in empty lanes would cancel an undefended enemy and the wave could never end.
        /// Mangrove Guardian (Blue Carbon) cools more: −0.08 °C, or −0.10 °C with the Blue Carbon Belt.
        /// Returns the change in °C (negative = cooler, 0 = nothing changed).
        /// </summary>
        public float ReportAllyAttack(int lane, UnitBase attacker, Vector3 worldPos)
        {
            if (_level == null) return 0f;
            if (_cooledThisPhase)
            {
                _ui.ShowFloatingText(worldPos, "already cooled", new Color(0.7f, 0.82f, 0.9f));
                return 0f;
            }
            _cooledThisPhase = true;
            return AddClimate(-CoolingFor(attacker), worldPos);
        }

        /// <summary>
        /// Enemy attacked its lane with no ally in it: the planet heats up. Methane Cloud (Greenhouse) adds
        /// +0.15 °C instead of the level's amount. Returns the change in °C.
        /// </summary>
        public float ReportEnemyAttack(int lane, UnitBase attacker, Vector3 worldPos)
        {
            if (_level == null) return 0f;
            float delta = AddClimate(HeatFor(attacker), worldPos);
            _ui.FlashTemperature();
            return delta;
        }

        /// <summary>Changes the temperature from a trait (Soot, Canopy, Methane Leak, Smokestacks) and shows it. Returns the change.</summary>
        public float AddClimate(float delta, Vector3 worldPos, string label = null)
        {
            float before = _temperature.Current;
            _temperature.Add(delta);
            float applied = _temperature.Current - before;
            string text = delta < 0f ? $"-{-delta:0.00}°C" : $"+{delta:0.00}°C";
            if (label != null) text = $"{text} {label}";
            _ui.ShowFloatingText(worldPos, text, delta < 0f ? new Color(0.45f, 0.85f, 1f) : new Color(1f, 0.35f, 0.25f));
            return applied;
        }

        public float CoolingFor(UnitBase ally)
        {
            if (ally != null && ally.Data.trait == UnitTrait.BlueCarbon)
                return ally.Synergy.BlueCarbonBelt ? 0.10f : ally.Data.traitValue;
            return _level != null ? _level.coolingPerEmptyLaneAttack : 0.05f;
        }

        public float HeatFor(UnitBase enemy)
        {
            if (enemy != null && enemy.Data.trait == UnitTrait.Greenhouse) return enemy.Data.traitValue;
            return _level != null ? _level.heatPerUndefendedHit : 0.10f;
        }
    }
}
