using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Applies lane rules, including the climate result of each attack.
    /// Units only report their attacks; this script alone decides the temperature effect.
    /// Each report returns the °C change it caused, so the lane camera can show the lane's result.
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
        /// Returns the change in °C (negative = cooler, 0 = nothing changed).
        /// </summary>
        public float ReportAllyAttack(int lane, bool hadTarget, Vector3 worldPos)
        {
            if (hadTarget || _level == null) return 0f;
            if (_cooledThisPhase)
            {
                _ui.ShowFloatingText(worldPos, "already cooled", new Color(0.7f, 0.82f, 0.9f));
                return 0f;
            }
            _cooledThisPhase = true;
            float before = _temperature.Current;
            _temperature.Add(-_level.coolingPerEmptyLaneAttack);
            float delta = _temperature.Current - before;
            _ui.ShowFloatingText(worldPos, $"-{_level.coolingPerEmptyLaneAttack:0.00}°C", new Color(0.45f, 0.85f, 1f));
            return delta;
        }

        /// <summary>Enemy attacked its lane. An undefended lane heats the planet. Returns the change in °C.</summary>
        public float ReportEnemyAttack(int lane, bool hadTarget, Vector3 worldPos)
        {
            if (hadTarget || _level == null) return 0f;
            float before = _temperature.Current;
            _temperature.Add(_level.heatPerUndefendedHit);
            _ui.ShowFloatingText(worldPos, $"+{_level.heatPerUndefendedHit:0.00}°C", new Color(1f, 0.35f, 0.25f));
            _ui.FlashTemperature();
            return _temperature.Current - before;
        }
    }
}
