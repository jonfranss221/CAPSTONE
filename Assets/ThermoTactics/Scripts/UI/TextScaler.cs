using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>Settings ▸ Large text: makes every Text under this canvas 12% larger.</summary>
    public class TextScaler : MonoBehaviour
    {
        public const float LargeScale = 1.12f;

        private readonly Dictionary<Text, int> _baseSizes = new Dictionary<Text, int>();

        private void Awake()
        {
            foreach (Text t in GetComponentsInChildren<Text>(true)) _baseSizes[t] = t.fontSize;
            Apply();
            SettingsStore.Changed += Apply;
        }

        private void OnDestroy() => SettingsStore.Changed -= Apply;

        private void Apply()
        {
            float k = SettingsStore.LargeText ? LargeScale : 1f;
            foreach (var kv in _baseSizes)
                if (kv.Key != null) kv.Key.fontSize = Mathf.RoundToInt(kv.Value * k);
        }
    }
}
