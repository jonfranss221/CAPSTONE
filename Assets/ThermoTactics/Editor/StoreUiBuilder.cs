using UnityEngine;
using UnityEngine.UI;
using static ThermoTactics.EditorTools.UiKit;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Builds the Main Menu's STORE (buy allies with Carbon Credits) and the squad pick shown before a level.
    /// Both use a tile template that the panel copies for each ally at runtime.
    /// </summary>
    public static class StoreUiBuilder
    {
        private static readonly Color Leaf = Hex("#8FE09B");
        private static readonly Color LeafLight = Hex("#C9F5CF");
        private static readonly Color LeafBorder = Hex("#4E9A5A");

        private static RectTransform Overlay(string name, Transform c, out RectTransform frame, float alpha = 0.8f)
        {
            var o = Img(name, c, new Color(0.024f, 0.04f, 0.03f, alpha), null, true); Stretch(o.rectTransform);
            frame = Frame("Frame", o.transform);
            AddPanelFx(o.gameObject, frame);
            return o.rectTransform;
        }

        /// <summary>A tile button: tier stripe, card, name and one more line (price, or OP and Speed).</summary>
        private static GameObject Tile(string name, Transform parent, string lowerChild, int cardSize, bool order)
        {
            var tile = Img(name, parent, new Color(0.078f, 0.11f, 0.09f, 1f), null, true);
            Border(tile.transform, Slate, "ui_frame2");
            var btn = tile.gameObject.AddComponent<Button>();
            btn.targetGraphic = tile;
            tile.gameObject.AddComponent<ButtonFx>();
            var stripe = Img("Stripe", tile.transform, Gold);
            var srt = stripe.rectTransform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1); srt.pivot = new Vector2(0.5f, 1);
            srt.sizeDelta = new Vector2(-8, 5); srt.anchoredPosition = new Vector2(0, -4);
            var card = Img("Card", tile.transform, Color.white);
            card.preserveAspect = true;
            var crt = card.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 1); crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(cardSize, cardSize); crt.anchoredPosition = new Vector2(0, -14);
            var nameText = Txt("Name", tile.transform, "Unit", BodyBold, 13, Cream, TextAnchor.UpperCenter);
            var nrt = nameText.rectTransform;
            nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(1, 1); nrt.pivot = new Vector2(0.5f, 1);
            nrt.sizeDelta = new Vector2(-10, 34); nrt.anchoredPosition = new Vector2(0, -(18 + cardSize));
            var lower = Txt(lowerChild, tile.transform, "", BodyBold, 13, CreamDim, TextAnchor.MiddleCenter);
            var lrt = lower.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0); lrt.pivot = new Vector2(0.5f, 0);
            lrt.sizeDelta = new Vector2(-10, 22); lrt.anchoredPosition = new Vector2(0, 6);
            var sel = Border(tile.transform, Gold);
            sel.name = "Selected";
            sel.gameObject.SetActive(false);
            if (order)
            {
                var badge = Img("OrderBack", sel.transform, Gold);
                var brt = badge.rectTransform;
                brt.anchorMin = brt.anchorMax = new Vector2(0, 1); brt.pivot = new Vector2(0, 1);
                brt.sizeDelta = new Vector2(24, 24); brt.anchoredPosition = new Vector2(6, -10);
                var o = Txt("Order", tile.transform, "", Pixel, 16, Ink, TextAnchor.MiddleCenter);
                var ort = o.rectTransform;
                ort.anchorMin = ort.anchorMax = new Vector2(0, 1); ort.pivot = new Vector2(0, 1);
                ort.sizeDelta = new Vector2(24, 24); ort.anchoredPosition = new Vector2(6, -10);
            }
            return tile.gameObject;
        }

        private static RectTransform Grid(string name, Transform parent, float x, float y, float w, float h, Vector2 cell, int columns)
        {
            var area = At(NewRect(name, parent), x, y, w, h);
            var grid = area.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell; grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            return area;
        }

        // =====================================================================
        // Store
        // =====================================================================
        public static StorePanel BuildStore(Transform c)
        {
            var root = Overlay("StorePanel", c, out var f);
            var panel = root.gameObject.AddComponent<StorePanel>();
            var d = Box("Dialog", f, Panel, Moss, 40, 36, 1200, 648, 8f);
            Weave(d.transform, 0, 0, 1200);
            Icon("Icon", d.transform, "icon_credit", Leaf, 28, 36, 34);
            TxtAt("Title", d.transform, "STORE", Pixel, 34, Cream, 72, 34, 400, 40, TextAnchor.MiddleLeft);
            Ref(panel, "_subtitle", TxtAt("Sub", d.transform, "", Body, 15, CreamDim, 28, 80, 760, 22));

            var wallet = Box("Wallet", d.transform, new Color(0.31f, 0.62f, 0.35f, 0.18f), LeafBorder, 952, 34, 220, 44, 0f, "ui_frame2");
            Icon("Icon", wallet.transform, "icon_credit", Leaf, 12, 10, 24);
            Ref(panel, "_wallet", TxtAt("Amount", wallet.transform, "0 CC", BodyBold, 19, LeafLight, 44, 0, 170, 44, TextAnchor.MiddleLeft));

            // 10 allies: 5 × 2 tiles on the left.
            var grid = Grid("Grid", d.transform, 28, 116, 730, 440, new Vector2(138, 200), 5);
            var tile = Tile("TileTemplate", grid, "Price", 96, false);
            Ref(panel, "_grid", grid);
            Ref(panel, "_tileTemplate", tile);

            // Detail panel on the right.
            var det = Box("Detail", d.transform, PanelDeep, Line, 782, 104, 390, 516, 0f, "ui_frame2");
            var big = Img("Card", det.transform, Color.white); big.preserveAspect = true;
            At(big, 20, 20, 108, 108);
            Ref(panel, "_detailCard", big);
            Ref(panel, "_detailName", TxtAt("Name", det.transform, "", Pixel, 22, Cream, 142, 24, 236, 52, TextAnchor.UpperLeft));
            Ref(panel, "_detailTier", TxtAt("Tier", det.transform, "", BodyBold, 13, CreamDim, 142, 80, 236, 44, TextAnchor.UpperLeft));
            Ref(panel, "_detailStats", TxtAt("Stats", det.transform, "", Body, 14, Cream, 20, 140, 350, 22, TextAnchor.MiddleLeft));
            Ref(panel, "_detailTrait", TxtAt("Trait", det.transform, "", Body, 14, CreamDim, 20, 172, 350, 210, TextAnchor.UpperLeft));
            Ref(panel, "_detailState", TxtAt("State", det.transform, "", Body, 14, Cream, 20, 388, 350, 44, TextAnchor.UpperLeft));
            var buy = Btn("BuyButton", det.transform, "", Gold, Ink, Ink, 20, 440, 350, 56, 22);
            var buyLabel = Txt("Label", buy.transform, "BUY", Pixel, 22, Ink, TextAnchor.MiddleCenter); Stretch(buyLabel.rectTransform);
            Ref(panel, "_buyButton", buy);
            Ref(panel, "_buyLabel", buyLabel);

            Ref(panel, "_closeButton", Btn("CloseButton", d.transform, "CLOSE", Forest, Cream, Slate, 28, 572, 180, 50, 20, 0f));
            TxtAt("Note", d.transform, "1 Carbon Credit = 1 tonne of CO2 kept out of the air. Earn them by clearing levels and earning stars.",
                Body, 13, CreamDim, 226, 572, 540, 50, TextAnchor.MiddleLeft);
            Ref(panel, "_root", root.gameObject);
            return panel;
        }

        // =====================================================================
        // Squad pick (before a level)
        // =====================================================================
        public static SquadPanel BuildSquad(Transform c)
        {
            var root = Overlay("SquadPanel", c, out var f, 0.84f);
            var panel = root.gameObject.AddComponent<SquadPanel>();
            var d = Box("Dialog", f, Panel, Gold, 140, 36, 1000, 648, 8f);
            Weave(d.transform, 0, 0, 1000);
            TxtAt("Title", d.transform, "CHOOSE YOUR SQUAD", Pixel, 32, Cream, 28, 34, 620, 40, TextAnchor.MiddleLeft);
            Ref(panel, "_subtitle", TxtAt("Sub", d.transform, "", Body, 15, CreamDim, 28, 78, 940, 22));
            Ref(panel, "_count", TxtAt("Count", d.transform, "Squad 0 / 5", BodyBold, 20, GoldLight, 760, 38, 212, 32, TextAnchor.MiddleRight));

            // Up to 15 allies: 5 × 3 tiles.
            var grid = Grid("Grid", d.transform, 28, 112, 944, 420, new Vector2(180, 124), 5);
            var tile = Tile("TileTemplate", grid, "Stats", 64, true);
            Ref(panel, "_grid", grid);
            Ref(panel, "_tileTemplate", tile);

            Ref(panel, "_info", TxtAt("Info", d.transform, "", Body, 14, CreamDim, 28, 512, 944, 44, TextAnchor.UpperLeft));
            Ref(panel, "_cancelButton", Btn("CancelButton", d.transform, "CANCEL", Forest, Cream, Slate, 28, 572, 180, 50, 20, 0f));
            Ref(panel, "_defaultButton", Btn("StartersButton", d.transform, "STARTERS", Forest, Cream, Slate, 222, 572, 200, 50, 20, 0f));
            Ref(panel, "_startButton", Btn("StartButton", d.transform, "START LEVEL", Red, Hex("#FFF6E6"), Ink, 692, 572, 280, 50, 22));
            Ref(panel, "_root", root.gameObject);
            return panel;
        }
    }
}
