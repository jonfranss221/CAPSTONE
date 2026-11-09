# Changelog

Thermo-Tactics uses semantic versioning, **MAJOR.MINOR.PATCH**:

- **PATCH** (0.3.0 → 0.3.1): bug fixes only, no new features, saved games still load.
- **MINOR** (0.3.0 → 0.4.0): a new working feature that keeps existing saves working.
- **MAJOR** (1.x → 2.0.0): a change that breaks saved-data compatibility or redesigns the game.
- Versions below **1.0.0** are pre-release: features may still change between minor versions.

Each release is built with **Thermo-Tactics ▸ Release ▸ Build Android APK** from `main`, tagged `vX.Y.Z` in Git,
and published as `Thermo-Tactics-vX.Y.Z.apk` under the repository's GitHub Releases.

## [0.3.1] — patch

Release type: **Testing**. Found during release testing of 0.3.0 on a phone.

### Fixed
- The game opened in portrait on phones held upright or with auto-rotate turned off, so only a narrow slice of the
  screen showed. It is now landscape only: it always opens sideways and turns between landscape left and right.

### Known issues
- Same as 0.3.0, except the orientation problem above.

## [0.3.0] — first controlled release

Release type: **Testing** (first build given to testers outside the development team).

### Included
- Stage 1 (Baguio): 10 levels with unlocking and 1–3 stars, a difficulty spike at Level 5 and the Clearcut Behemoth boss at Level 10.
- Full roster data (roster v3): Speed and category counters, traits, status effects and 12 pair synergies.
- Carbon Credits earned on level wins, the Store with four tiers, and squad selection before each level.
- Moving units between lanes, and moving, swapping or taking back a unit placed by mistake until End Turn.
- Pre-quiz, climate-fact pop-ups, post-quiz with knowledge gain, and quiz CSV export for the study.
- Local SQLite save: results, level progress, wallet, owned units and Continue.
- Level intro and a hands-on Level 1 tutorial; Settings for battle speed, large text, high contrast and lane camera.

### Fixed (since 0.2.0)
- Saving a finished level moved out of GameManager into ProgressService; a failed save now shows "Result not saved" instead of failing silently.
- Units placed by mistake no longer cost Oxygen for the whole turn (take back with a full refund).

### Known issues
- A saved Continue game from 0.2.0 is discarded when 0.3.0 first opens (saves are tied to the version). Progress and stars are kept.
- The APK is signed with Unity's debug key: Android shows an "unknown app" warning when installing.
- The pick-up ring under a selected unit is tinted yellow-green instead of gold.
- No music or sound effects yet; the volume sliders have no effect.

### Not yet included
- Stages 2–5 (only Stage 1 is playable; the others are locked on Stage Select) and their bosses.
- Enemy movement, audio, and cloud save.

## [0.2.0] — internal milestone (October 13, 2026)
- Main Menu, Baguio Levels 1–10 with the boss, save and Continue. Not distributed.
