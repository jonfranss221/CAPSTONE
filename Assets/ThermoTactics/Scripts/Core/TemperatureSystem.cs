using System;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Owns the global temperature. Only this script changes the number.</summary>
    public class TemperatureSystem : MonoBehaviour
    {
        public event Action<float> Changed;

        public float Current { get; private set; } = 1f;
        public float Baseline { get; private set; } = 1f;
        public float Threshold { get; private set; } = 1.5f;

        /// <summary>True once the temperature has reached the loss threshold.</summary>
        public bool HasReachedThreshold => Current >= Threshold - 0.0001f;

        /// <summary>0 at the starting temperature, 1 at the threshold.</summary>
        public float Normalized => Mathf.InverseLerp(Baseline, Threshold, Current);

        public void Init(float baseline, float threshold)
        {
            Baseline = baseline;
            Threshold = threshold;
            Current = baseline;
            Changed?.Invoke(Current);
        }

        /// <summary>Adds (or removes, if negative) degrees. Never drops below the starting temperature.</summary>
        public void Add(float delta)
        {
            Current = Mathf.Round(Mathf.Max(Baseline, Current + delta) * 100f) / 100f;
            Changed?.Invoke(Current);
        }
    }
}
