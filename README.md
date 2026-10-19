# Thermo-Tactics

A Philippine climate-themed 2D turn-based strategy game for Android, built in Unity.
Players defend Philippine ecosystems from fossil-fuel threats while keeping the global
temperature below the **1.5 °C** limit. Capstone project.

> **Status (v0.2.0):** Stage 1 (Baguio) playable from the Main Menu: 10 levels with unlocking and 1–3 stars,
> a lane-by-lane battle camera, a difficulty spike at Level 5, the Clearcut Behemoth boss at Level 10,
> a Stage Complete screen, and save / Continue.

All names, text and dialogue are in English. The Filipino theme is in the environments and UI art.

---

## Requirements

| Item | Version |
| --- | --- |
| Unity | **6 (6000.0.84f1)** with the Universal Render Pipeline 2D |
| Target | Android, minimum API 23 (Android 6.0), IL2CPP, ARM64 |
| Input | Unity Input System (touch and mouse) |
| Packages | 2D feature set, uGUI, Input System, SQLite-net (installed through `Packages/manifest.json`; Git must be installed) |

## Getting started

1. Clone the repository:
   ```bash
   git clone https://github.com/jonfranss221/CAPSTONE.git
   ```
2. Open the folder in **Unity Hub → Add → Add project from disk** (Unity 6000.0.84f1).
   The first open rebuilds the `Library/` folder and takes a few minutes.
3. Open `Assets/ThermoTactics/Scenes/MainMenu.unity` and press **Play**.
   (Playing `Baguio_Battle.unity` directly starts the level set on its GameManager, Level 1 by default.)

## How to play

1. On the title screen, type a **player name** (2–20 characters). A tester code is optional.
2. Press **Start**, pick **Baguio Forest**, then a level on the path. Level 1 is open; clearing a level opens the next.
3. Tap a unit card in the deploy bar, then a glowing ally slot on the road. Each level offers up to 5 unit types.
4. Tap **End Turn**. The camera zooms in on lanes 1 to 6 in turn: in each lane your unit attacks, then the enemy
   (if still standing) hits back. A banner shows what happened in each lane. Settings can turn the zoom off.
5. Clear every wave while the temperature stays **below 1.5 °C** to win. You earn **1–3 stars** from the highest
   temperature reached: 3 stars at 1.20 °C or below, 2 at 1.35 °C or below.
6. After a level: **Next Level**, **Retry**, **Level Select** or **Stage Select**. Beating Level 10 shows the
   **Stage Complete** screen with your star total.
7. The game saves itself at the start of every turn, after every deploy and when the app is paused or closed.
   Next time, type the same name and press **Continue** to return to the same level, wave and turn.

| Rule | Value |
| --- | --- |
| Starting temperature | 1.00 °C |
| Enemy hits an undefended lane | +0.10 °C |
| Ally attacks an empty enemy slot | −0.05 °C (at most once per turn) |
| Loss | temperature reaches 1.50 °C |
| Oxygen per turn | set per wave: 5–11 (Level 1: 5 · 6 · 7); unspent Oxygen does not carry over |
| Oxygen penalty | turn 2: −1 · turn 3: −2 · turn 4: −4 · turn 5+: −8 (by turn within a wave), never below 2; none in the boss wave |

### Baguio levels

| Level | Location | Waves | Oxygen per wave | Notes |
| --- | --- | --- | --- | --- |
| 1 | Kennon Road | 3 | 5 · 6 · 7 | |
| 2 | Lion's Head | 3 | 5 · 7 · 8 | |
| 3 | Camp John Hay | 3 | 5 · 7 · 10 | |
| 4 | Burnham Park | 3 | 6 · 8 · 10 | |
| 5 | Session Road | 4 | 6 · 8 · 10 · 11 | Difficulty spike: elite enemies |
| 6 | Mines View Park | 3 | 6 · 8 · 10 | |
| 7 | Strawberry Farm | 3 | 6 · 8 · 10 | |
| 8 | Busol Watershed | 4 | 7 · 9 · 11 · 11 | |
| 9 | Tam-awan Village | 4 | 7 · 9 · 10 · 11 | |
| 10 | Mt. Santo Tomas | 5 | 7 · 9 · 10 · 11 · 8 | Wave 5: Clearcut Behemoth (boss) |

**Clearcut Behemoth:** 300 HP and fills all 6 enemy slots. **Cleave** hits the lane of your strongest unit and the lanes beside it for 8.
**Landslide Slam** hits all 6 lanes for 7 every 3rd turn. At 40% HP it **enrages**: cleave 10, slam every 2nd turn.
The boss panel always shows its next move.

## Implemented features

- Main Menu: living background (pan, fog, pollen), player profile, Continue, Start, Settings, Quit (with confirmation),
  Credits, Results, and a Baguio progress strip with stars; buttons and screens animate
- Stage Select (Baguio open; four more stages locked) and the 10-node Baguio level path with stars and best temperatures
- Settings: music and effects volume (used once audio is added), battle speed (1× / 1.5× / 2×), large text, high contrast, climate pop-ups,
  damage numbers, boss warning, lane camera zoom, and resetting a player's progress
- 13 unit types (5 allies, 8 enemies) and 6 elite variants, each with **idle**, **attack** and **hurt** animations
- Clearcut Behemoth boss with cleave, Landslide Slam and enrage; HUD boss panel and danger rings on target lanes
- Oxygen system with turn penalty, minimum of 2 and no penalty in the boss wave
- Lane-by-lane End Turn with a zooming camera and a lane banner
- Pause menu; Level Complete (stars) / Level Failed screens with Next Level, Retry, Level Select and Stage Select;
  Stage Complete screen after Level 10
- Local **SQLite** database: results, level progress (unlocks, best stars) and the saved game behind Continue

- Education: pre-quiz before Baguio, climate-fact pop-ups during Levels 1–9 (one per quiz topic) with a Climate Facts
  review list, post-quiz after Level 10 with a topic-by-topic comparison and knowledge gain, and a quiz CSV export for
  the study (database schema version 4: `quiz_results`, `facts_seen`)

Planned: audio, enemy movement (EnemyAI), stages 2–5 with their own bosses.

## Project structure

```
Assets/ThermoTactics/
├── Art/            Units/<Unit>/ · Bosses/ClearcutBehemoth/ · Environment/ (Baguio) · UI/ (icons, thumbnails, level path)
├── Animations/     Idle / Attack / Hurt clips + Animator controller per unit and for the boss
├── Fonts/          Pixelify Sans, Atkinson Hyperlegible (SIL Open Font Licence)
├── GameData/       Stage1_Baguio · Levels/ (Baguio_Level1–10) · Waves/ (35) · Units/ (20 incl. elites and boss)
├── Scenes/         MainMenu.unity · Baguio_Battle.unity
├── Scripts/
│   ├── Core/          GameManager, GameSession, SettingsStore, StarRating, TurnManager, WaveManager, OxygenPointSystem, TemperatureSystem
│   ├── Board/         GridManager, BoardSlot, IsoBoardLayout, LaneController, DeploySystem, UnitFactory
│   ├── Units/         UnitBase, AllyUnit, EnemyUnit, BossUnit
│   ├── Data/          StageData, LevelData, WaveData, UnitData, BossData (ScriptableObjects)
│   ├── UI/            UIManager, DeployCard, ResultsPanel, TextScaler, ButtonFx, PanelFx
│   │   └── Menu/      MainMenuController, MenuAmbience, SettingsPanel, StageSelectPanel, LevelSelectPanel, LevelNode
│   ├── Persistence/   ResultsDatabase, SessionSnapshot
│   ├── Presentation/  FogScroller, BattleCamera
│   └── Dev/           AutoPlaytest, QuickCheck (editor only)
└── Editor/         ThermoTacticsBuilder, MenuSceneBuilder, UiKit, PlaytestMenu
```

**Thermo-Tactics ▸ Build Everything** imports the art, builds the animations, the 10 levels and both scenes.
Single steps are under **Thermo-Tactics ▸ Steps**. **Thermo-Tactics ▸ Test ▸ Play Stage 1 Automatically** plays all 10 levels
by itself as the player "Playtest" and saves a report and screenshots to `Logs/qa/auto/` (not committed).
**Thermo-Tactics ▸ Test ▸ Quick Check of New Screens** checks the menus, Level 1, a loss and the Level 10 boss in about a minute (`Logs/qa/check/`). Rebuilding replaces the scenes, so make manual scene edits after rebuilding.
Tune levels and units in the `GameData` assets.

## Database (SQLite)

Player data is stored offline in **`thermo_tactics.db`** in `Application.persistentDataPath`
(Windows editor: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Thermo-Tactics\`; Android: the app's private storage).
It is accessed with [SQLite-net for Unity](https://github.com/gilzoide/unity-sqlite-net) (`com.gilzoide.sqlite-net` 1.3.2, MIT)
by one script, `Scripts/Persistence/ResultsDatabase.cs`. The schema version is kept in `PRAGMA user_version` (now **3**).

| Table | One row per | Used for |
| --- | --- | --- |
| `game_results` | finished level | Results list (name, WIN / LOSS, date, final °C, waves cleared) |
| `level_progress` | player + stage + level | Unlocking levels; attempts; best (lowest) winning °C; best stars |
| `saved_session` | player | Continue: level, wave, turn, Oxygen, °C and peak °C, boss turn, units with HP (JSON) |

Player names match without case ("Jon" = "jon"). Values are bound as parameters (`?`), never concatenated into the SQL.
A saved game from a different app version is discarded. Results from the older JSON version (`results_db.json`) are imported once.
The file can be opened with [DB Browser for SQLite](https://sqlitebrowser.org/).

Settings are kept in PlayerPrefs (keys starting with `tt.`) by `Scripts/Core/SettingsStore.cs`.

## Documentation

`Docs/` holds the architecture documents (Parts I–VI, .docx) and `Docs/Figures/` the diagrams.

## Version control

| Item | Status |
| --- | --- |
| `.gitignore` | Unity template; excludes `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `UserSettings/`, IDE files and builds |
| `.gitattributes` | Text/LF rules for Unity YAML; Git LFS rules for audio and layered art |
| Git LFS | Configured for `*.wav *.mp3 *.ogg *.psd *.aseprite *.mp4` (no such files yet) |
| Branches | `main` = stable · `develop` = integration · `feature/<name>` per system |
| Unity settings | Asset Serialization **Force Text**; Version Control mode **Visible Meta Files** |

Commit `.meta` files together with their assets.

## Team

| Member | Role |
| --- | --- |
| [ Edz Steinar Bederico ] | [ role ] |
| [ Jon Francis Gellido ] | [ role ] |
| [ Rodge Emanuel Ramos ] | [ role ] |
| [ Jherwin Reyes ] | [ role ] |
