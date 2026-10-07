using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Applies lane rules, including the climate result of each attack.
    /// Units only report their attacks; this script alone decides the temperature effect.
    /// </summary>
    public class LaneController : MonoBehaviour
    {
        [SerializeField] private TemperatureSystem _temperature;
        [SerializeField] private UIManager _ui;

        private LevelData _level;

        public void Init(LevelData level) => _level = level;

        /// <summary>Ally attacked its lane. An empty lane cools the planet.</summary>
        public void ReportAllyAttack(int lane, bool hadTarget, Vector3 worldPos)
        {
            if (hadTarget || _level == null) return;
            _temperature.Add(-_level.coolingPerEmptyLaneAttack);
            _ui.ShowFloatingText(worldPos, $"-{_level.coolingPerEmptyLaneAttack:0.00}°C", new Color(0.45f, 0.85f, 1f));
        }

        /// <summary>Enemy attacked its lane. An undefended lane heats the planet.</summary>
        public void ReportEnemyAttack(int lane, bool hadTarget, Vector3 worldPos)
        {
            if (hadTarget || _level == null) return;
            _temperature.Add(_level.heatPerUndefendedHit);
            _ui.ShowFloatingText(worldPos, $"+{_level.heatPerUndefendedHit:0.00}°C", new Color(1f, 0.35f, 0.25f));
            _ui.FlashTemperature();
        }
    }
}
