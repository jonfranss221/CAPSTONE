using UnityEngine;
using UnityEngine.UI;
using static ThermoTactics.EditorTools.UiKit;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Builds the onboarding screens of the battle scene in the design's style: the level intro (black fade and
    /// title card) and the Level 1 tutorial (coach box and highlight frame). The behaviours (LevelIntro,
    /// TutorialController) live on GameSystems, so their coroutines run while these screens are hidden.
    /// </summary>
    internal static class OnboardingUiBuilder
    {
        // =====================================================================
        // Level intro: full-screen black fade + centred title card
        // =====================================================================
        public static void BuildLevelIntro(Transform c, LevelIntro intro)
        {
            var root = NewRect("LevelIntro", c); Stretch(root);

            // A transparent full-screen button: any tap skips the card (it also blocks the board underneath).
            var skip = Img("SkipArea", root, Clear, null, true); Stretch(skip.rectTransform);
            var skipButton = skip.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skip;
            skipButton.transition = Selectable.Transition.None;

            var fade = Img("Fade", root, Color.black); Stretch(fade.rectTransform);

            var f = Frame("Frame", root);
            var cardGroup = NewRect("Card", f);
            At(cardGroup, 280, 196, 720, 300);
            var group = cardGroup.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var card = Img("Back", cardGroup, new Color(0.071f, 0.102f, 0.082f, 0.94f)); Stretch(card.rectTransform);
            HardShadow(card, 8f, 0.5f);
            Border(card.transform, Gold);
            Weave(cardGroup, 0, 0, 720);

            Ref(intro, "_eyebrow", TxtAt("Eyebrow", cardGroup, "STAGE 1 · BAGUIO FOREST", Pixel, 22, GoldLight, 0, 40, 720, 30, TextAnchor.MiddleCenter));
            var level = TxtAt("Level", cardGroup, "LEVEL 1", Pixel, 64, Gold, 0, 74, 720, 76, TextAnchor.MiddleCenter);
            HardShadow(level, 4f, 0.8f);
            Ref(intro, "_level", level);
            Ref(intro, "_place", TxtAt("Place", cardGroup, "KENNON ROAD", Pixel, 40, Cream, 0, 150, 720, 50, TextAnchor.MiddleCenter));
            Ref(intro, "_subtitle", TxtAt("Subtitle", cardGroup, "", Body, 18, CreamDim, 40, 202, 640, 26, TextAnchor.MiddleCenter));
            At(Img("Divider", cardGroup, Line), 200, 238, 320, 2);
            Ref(intro, "_tag", TxtAt("Tag", cardGroup, "3 WAVES · KEEP BELOW 1.5 °C", BodyBold, 14, Mint, 0, 248, 720, 26, TextAnchor.MiddleCenter));

            Ref(intro, "_root", root.gameObject);
            Ref(intro, "_fade", fade);
            Ref(intro, "_card", group);
            Ref(intro, "_skipButton", skipButton);
            root.gameObject.SetActive(false);
        }

        // =====================================================================
        // Level 1 tutorial: coach box + pulsing highlight frame
        // =====================================================================
        public static void BuildTutorial(Transform c, TutorialController tutorial,
                                         RectTransform temperature, RectTransform oxygen, RectTransform cards, RectTransform endTurn)
        {
            var root = NewRect("Tutorial", c); Stretch(root);

            // Highlight: a gold frame drawn around the target (its size and place are set at runtime).
            var highlight = NewRect("Highlight", root);
            highlight.anchorMin = highlight.anchorMax = highlight.pivot = new Vector2(0.5f, 0.5f);
            highlight.sizeDelta = new Vector2(200, 80);
            var glow = Img("Glow", highlight, Alpha(GoldLight, 0.08f)); Stretch(glow.rectTransform);
            var frame = Border(highlight, Gold, "ui_frame2");
            frame.pixelsPerUnitMultiplier = 0.5f;   // a thicker line than the 2 px panels
            highlight.gameObject.SetActive(false);

            // Coach box (docked bottom-left above the footer; moved to the top at runtime when the target is low).
            var coach = (RectTransform)Img("Coach", root, new Color(0.071f, 0.102f, 0.082f, 0.97f), null, true).transform;
            coach.anchorMin = coach.anchorMax = coach.pivot = new Vector2(0f, 0f);
            coach.sizeDelta = new Vector2(520, 200);
            coach.anchoredPosition = new Vector2(20, 101);
            HardShadow(coach.GetComponent<Image>(), 6f, 0.5f);
            Border(coach, Gold);
            Weave(coach, 0, 0, 520, 10);

            Ref(tutorial, "_stepLabel", TxtAt("Step", coach, "STEP 1 / 9", BodyBold, 13, GoldLight, 22, 22, 300, 18, TextAnchor.MiddleLeft));
            Ref(tutorial, "_title", TxtAt("Title", coach, "WELCOME", Pixel, 24, Cream, 22, 42, 476, 32, TextAnchor.MiddleLeft));
            Ref(tutorial, "_body", TxtAt("Body", coach, "", Body, 15, Cream, 22, 78, 476, 66, TextAnchor.UpperLeft));
            var skip = Btn("SkipButton", coach, "SKIP TUTORIAL", Clear, CreamDim, Slate, 22, 146, 170, 38, 14, 0f, BodyBold);
            var next = Btn("NextButton", coach, "NEXT", Gold, Ink, Ink, 300, 146, 198, 38, 18);
            Ref(tutorial, "_skipButton", skip);
            Ref(tutorial, "_nextButton", next);
            Ref(tutorial, "_nextLabel", next.GetComponentInChildren<Text>());

            Ref(tutorial, "_root", root.gameObject);
            Ref(tutorial, "_coach", coach);
            Ref(tutorial, "_highlight", highlight);
            Ref(tutorial, "_temperatureTarget", temperature);
            Ref(tutorial, "_oxygenTarget", oxygen);
            Ref(tutorial, "_cardsTarget", cards);
            Ref(tutorial, "_endTurnTarget", endTurn);
            root.gameObject.SetActive(false);
        }
    }
}
