using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>The colour family of a stage's level path screen.</summary>
    public enum StageTheme { Forest, Coast }

    /// <summary>
    /// Maps the menu's forest-green palette (Baguio) to another stage's palette, colour by colour, so the level path
    /// screen keeps the same layout and only changes colour. Gold, cream and the warning reds stay the same.
    /// </summary>
    public static class StageThemes
    {
        /// <summary>The theme used by C() (set by the level path screen when it opens a stage).</summary>
        public static StageTheme Current = StageTheme.Forest;

        // Forest (Baguio) → Coast (Sierra Madre). Each row is one role in the UI.
        private static readonly string[,] CoastMap =
        {
            { "#0E1511", "#0E2A33" },   // Night: screen background
            { "#12201A", "#0F2C36" },   // Forest: panel background
            { "#0E1712", "#0D2530" },   // PanelDeep: detail panel
            { "#2F5D45", "#2E8C99" },   // Moss: accents, normal level tag
            { "#15231B", "#123340" },   // MossDim: stat boxes
            { "#22332A", "#1E4A55" },   // Line: dividers, empty threat bars
            { "#4A5A4E", "#3E6A72" },   // Slate: borders
            { "#1A2620", "#12303A" },   // locked node
            { "#9F978A", "#B5C9C9" },   // locked text
            { "#A3241C", "#C8553D" },   // current node, special levels, Play
            { "#3B4A40", "#24505A" },   // node star (empty)
            { "#2E3B33", "#234B55" },   // detail star (empty)
            { "#3B5446", "#2E6E78" },   // enemy ring
        };

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

        /// <summary>A Baguio palette colour translated into the current theme.</summary>
        public static Color C(string forestHex) => Map(Hex(forestHex), StageTheme.Forest, Current);

        /// <summary>Translates a colour from one theme to another (alpha kept). Colours not in the map stay.</summary>
        public static Color Map(Color c, StageTheme from, StageTheme to)
        {
            if (from == to) return c;
            int fromCol = from == StageTheme.Forest ? 0 : 1, toCol = 1 - fromCol;
            for (int i = 0; i < CoastMap.GetLength(0); i++)
            {
                Color k = Hex(CoastMap[i, fromCol]);
                if (Mathf.Abs(k.r - c.r) < 0.006f && Mathf.Abs(k.g - c.g) < 0.006f && Mathf.Abs(k.b - c.b) < 0.006f)
                {
                    Color t = Hex(CoastMap[i, toCol]);
                    t.a = c.a;
                    return t;
                }
            }
            return c;
        }

        /// <summary>Recolours every Image, Text and Outline under root from one theme to another.</summary>
        public static void Recolor(Transform root, StageTheme from, StageTheme to)
        {
            if (root == null || from == to) return;
            foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (g is Image img && img.sprite != null && img.sprite.name.StartsWith("levelpath")) continue;   // path art is per stage
                g.color = Map(g.color, from, to);
            }
            foreach (Shadow s in root.GetComponentsInChildren<Shadow>(true))
                s.effectColor = Map(s.effectColor, from, to);
        }
    }
}
