using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>The colour family of a stage's level path screen.</summary>
    public enum StageTheme { Forest, Coast, Metro }

    /// <summary>
    /// Maps the menu's forest-green palette (Baguio) to another stage's palette, colour by colour, so the level path
    /// screen keeps the same layout and only changes colour. Gold, cream and the warning reds stay the same.
    /// </summary>
    public static class StageThemes
    {
        /// <summary>The theme used by C() (set by the level path screen when it opens a stage).</summary>
        public static StageTheme Current = StageTheme.Forest;

        // One row per role in the UI; one column per theme, in StageTheme order:
        // Forest (Baguio) · Coast (Sierra Madre, sea teal) · Metro (Metro Manila, concrete grey with MRT blue).
        private static readonly string[,] Palette =
        {
            { "#0E1511", "#0E2A33", "#121517" },   // Night: screen background
            { "#12201A", "#0F2C36", "#171B1D" },   // Forest: panel background
            { "#0E1712", "#0D2530", "#131618" },   // PanelDeep: detail panel
            { "#2F5D45", "#2E8C99", "#4A6F8F" },   // Moss: accents, normal level tag
            { "#15231B", "#123340", "#1B2022" },   // MossDim: stat boxes
            { "#22332A", "#1E4A55", "#2C3234" },   // Line: dividers, empty threat bars
            { "#4A5A4E", "#3E6A72", "#555B5C" },   // Slate: borders
            { "#1A2620", "#12303A", "#1D2224" },   // locked node
            { "#9F978A", "#B5C9C9", "#B3B6B2" },   // locked text
            { "#A3241C", "#C8553D", "#B0332A" },   // current node, special levels, Play
            { "#3B4A40", "#24505A", "#3A4044" },   // node star (empty)
            { "#2E3B33", "#234B55", "#2F3538" },   // detail star (empty)
            { "#3B5446", "#2E6E78", "#4D5A60" },   // enemy ring
        };

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

        /// <summary>A Baguio palette colour translated into the current theme.</summary>
        public static Color C(string forestHex) => Map(Hex(forestHex), StageTheme.Forest, Current);

        /// <summary>Translates a colour from one theme to another (alpha kept). Colours not in the palette stay.</summary>
        public static Color Map(Color c, StageTheme from, StageTheme to)
        {
            if (from == to) return c;
            int fromCol = (int)from, toCol = (int)to;
            for (int i = 0; i < Palette.GetLength(0); i++)
            {
                Color k = Hex(Palette[i, fromCol]);
                if (Mathf.Abs(k.r - c.r) < 0.006f && Mathf.Abs(k.g - c.g) < 0.006f && Mathf.Abs(k.b - c.b) < 0.006f)
                {
                    Color t = Hex(Palette[i, toCol]);
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
