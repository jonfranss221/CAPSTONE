using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Settings ▸ Large text: makes every Text under this canvas 12% larger. Best-fit labels scale their
    /// size range too, so they still shrink to fit their box.
    /// </summary>
    public class TextScaler : MonoBehaviour
    {
        public const float LargeScale = 1.12f;

        private struct Sizes { public int Size, Min, Max; }
        private readonly Dictionary<Text, Sizes> _base = new Dictionary<Text, Sizes>();

        private void Awake()
        {
            foreach (Text t in GetComponentsInChildren<Text>(true))
                _base[t] = new Sizes { Size = t.fontSize, Min = t.resizeTextMinSize, Max = t.resizeTextMaxSize };
            Apply();
            SettingsStore.Changed += Apply;
        }

        private void OnDestroy() => SettingsStore.Changed -= Apply;

        private void Apply()
        {
            float k = SettingsStore.LargeText ? LargeScale : 1f;
            foreach (var kv in _base)
            {
                Text t = kv.Key;
                if (t == null) continue;
                t.fontSize = Mathf.RoundToInt(kv.Value.Size * k);
                if (t.resizeTextForBestFit)
                {
                    t.resizeTextMinSize = Mathf.RoundToInt(kv.Value.Min * k);
                    t.resizeTextMaxSize = Mathf.RoundToInt(kv.Value.Max * k);
                }
            }
        }
    }
}
