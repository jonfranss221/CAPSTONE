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
   Every level opens with a short intro: the screen fades in, the camera glides onto the road and a title card shows
   the stage, level and place (tap to skip). The first time a player starts Level 1, a **tutorial** walks through the
   screen step by step: you really pick a card, deploy and end a turn. It can be skipped and replayed from
   **Pause ▸ How to Play**.
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
- 13 unit types in play in Baguio (5 allies, 8 enemies) and 6 elite variants, each with **idle**, **attack** and **hurt** animations
- Full roster data (roster v3, Phase 1): all 30 characters (15 allies, 15 enemies) plus the Factory Titan and Typhoon Tyrant
  bosses exist as unit assets with art and animations; Baguio still uses the 5 starters and its 8 enemies (see *Roster* below)
- **Speed and counters** (Phase 2): in each lane the faster unit attacks first (a tie goes to the ally; Tarsier Scout's Night Eyes
  always goes first). Allies deal ×1.5 to the enemy category they counter (Forest > Extraction, Ocean > Waste, Renewable > Emissions);
  "+N vs X" traits, armour and Pierce apply. All damage comes from `Core/DamageRules.cs`. The lane banner says who strikes first and why;
  counters show a gold "×1.5!", armour a grey "−N"; the deploy hint shows Speed, category and trait
- **Traits, status effects and pair synergies** (Phase 3): Poison, Burn, Weaken, Slow and Dodge (coloured pips above the health bar,
  kept in the saved game); an end-of-turn step for damage over time, heals and °C traits (Canopy, Methane Leak, Smokestacks);
  Harvest Oxygen; Landslide splash; Greenhouse and Blue Carbon change the lane heat and cooling. 12 pairs in neighbouring lanes
  (`Core/SynergyRules.cs`), announced the first time they form in a level
- **Carbon Credits** (Phase 4): earned on every level win (first clear 50 + 10 × level, +100 on the boss level, +20 per new star;
  a replay pays 25%). The Level Complete screen counts up the reward and shows the wallet; the Main Menu shows the balance.
  1 CC = 1 tonne of CO2 kept out of the air
- **Store and squad selection** (Phase 5): the STORE button (Main Menu, under Start, and on the level path) stays grey and locked
  until Baguio Level 1 is cleared. The store lists the 10 non-starter allies by tier (Common, Rare, Epic, Legendary) with their stats,
  trait and price; Stage 2 allies appear once all 10 Baguio levels are cleared. Before each level the player picks a squad of up to
  5 owned allies (Level 1 uses the 5 starters while the guide is due); the last squad is remembered and saved with Continue
- Clearcut Behemoth boss with cleave, Landslide Slam and enrage; HUD boss panel and danger rings on target lanes
- Oxygen system with turn penalty, minimum of 2 and no penalty in the boss wave
- Lane-by-lane End Turn with a zooming camera and a lane banner
- Level intro (fade in, camera glide, title card with stage, level, place and waves) and a hands-on Level 1 tutorial
  with a coach box and highlight, shown once per player, skippable, replayable from Pause ▸ How to Play
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
├── Art/            Units/<Unit>/ (28) · Bosses/ClearcutBehemoth, FactoryTitan, TyphoonTyrant · Environment/ (Baguio) · UI/ (icons, thumbnails, level path)
├── Animations/     Idle / Attack / Hurt clips + Animator controller per unit and for the boss
├── Fonts/          Pixelify Sans, Atkinson Hyperlegible (SIL Open Font Licence)
├── GameData/       Stage1_Baguio · Levels/ (Baguio_Level1–10) · Waves/ (35) · Units/ (37: 28 units, 6 elites, 3 bosses)
├── Scenes/         MainMenu.unity · Baguio_Battle.unity
├── Scripts/
│   ├── Core/          GameManager, GameSession, SettingsStore, StarRating, TurnManager, WaveManager, OxygenPointSystem, TemperatureSystem,
│   │                  DamageRules, SynergyRules, CreditRules
│   ├── Board/         GridManager, BoardSlot, IsoBoardLayout, LaneController, DeploySystem, UnitFactory
│   ├── Units/         UnitBase, AllyUnit, EnemyUnit, BossUnit, StatusEffects
│   ├── Data/          StageData, LevelData, WaveData, UnitData, BossData (ScriptableObjects), UnitTraits (enums)
│   ├── UI/            UIManager, DeployCard, ResultsPanel, TextScaler, ButtonFx, PanelFx
│   │   └── Menu/      MainMenuController, MenuAmbience, SettingsPanel, StageSelectPanel, LevelSelectPanel, LevelNode
│   ├── Persistence/   ResultsDatabase, SessionSnapshot
│   ├── Presentation/  FogScroller, BattleCamera
│   └── Dev/           AutoPlaytest, QuickCheck (editor only)
└── Editor/         ThermoTacticsBuilder, MenuSceneBuilder, RosterArtImporter, UiKit, PlaytestMenu
```

**Thermo-Tactics ▸ Build Everything** imports the art, builds the animations, the 10 levels and both scenes.
Single steps are under **Thermo-Tactics ▸ Steps**. **Thermo-Tactics ▸ Test ▸ Play Stage 1 Automatically** plays all 10 levels
by itself as the player "Playtest" and saves a report and screenshots to `Logs/qa/auto/` (not committed).
**Thermo-Tactics ▸ Test ▸ Quick Check of New Screens** checks the menus, Level 1, a loss and the Level 10 boss in about a minute (`Logs/qa/check/`). Rebuilding replaces the scenes, so make manual scene edits after rebuilding.
Tune levels and units in the `GameData` assets.

### Roster

The full character art is in `Docs/NewCharacters/thermo_tactics_roster_v3/`. **Build Everything** first runs
**Thermo-Tactics ▸ Steps ▸ 0. Import Roster Art** (`Editor/RosterArtImporter.cs`), which slices it into `Art/Units/<Unit>/` and
`Art/Bosses/<Boss>/`: 12 idle, 12 attack and 8 hurt frames plus the card. The hurt frames are a white flash, then a fading red
tint with a 5 px knock-back, made from idle frame 0. Units that already have art are skipped.
`python Tools/import_roster_v3.py` does the same outside Unity (`--check` confirms the existing art still matches the roster).
Unit stats live in the `Units` table at the top of `Editor/ThermoTacticsBuilder.cs`.

| Field | Meaning |
| --- | --- |
| `speed` (1–10) | The faster unit in a lane attacks first; on a tie the ally goes first (Phase 2) |
| `category` | Allies: Forest, Ocean, Renewable. Enemies: Extraction, Waste, Emissions, Climate Disaster. Forest > Extraction, Ocean > Waste, Renewable > Emissions (×1.5 damage, Phase 2) |
| `isHover` | Flying or floating unit (some traits target hovering enemies) |
| `trait`, `traitValue`, `traitText` | The unit's special rule and its number (Phases 2–3) |
| `isStarter`, `tier`, `priceCredits`, `unlockStage` | Store data: the 5 starters are owned by everyone; the other 10 allies cost 300–1,600 Carbon Credits and appear in the store once their stage is unlocked (Phases 4–5) |

| Allies | Category | Cost | HP | Dmg | Speed | Store |
| --- | --- | --- | --- | --- | --- | --- |
| Mangrove Guardian · Coral Defender · Reef Sentinel | Ocean | 3 | 20 · 18 · 14 | 4 · 4 · 5 | 3 · 4 · 6 | Starters |
| Wind Caller · Geothermal Sentinel | Renewable | 2 · 4 | 10 · 22 | 4 · 6 | 8 · 3 | Starters |
| Bamboo Warden · Tarsier Scout · Rice Terrace Warden · Forest Spirit | Forest | 2 · 2 · 3 · 3 | 16 · 8 · 16 · 18 | 3 · 4 · 4 · 6 | 4 · 10 · 5 · 6 | Stage 1: 300 · 300 · 350 · 600 |
| Hydro Guardian · Seagrass Healer · Sea Turtle Guardian · Eagle Striker | Renewable · Ocean · Ocean · Forest | 4 · 3 · 4 · 4 | 24 · 12 · 32 · 14 | 5 · 3 · 4 · 8 | 3 · 5 · 1 · 9 | Stage 2: 600 · 650 · 650 · 1,000 |
| Solar Sentinel · Rainforest Ancient | Renewable · Forest | 4 · 6 | 16 · 40 | 7 · 9 | 5 · 2 | Stage 3: 700 · 1,600 |

Enemies keep their Baguio stats. New enemies: Coal Brute, Diesel Drone, Toxic Runoff (Stage 2+), Incinerator Fiend,
Landfill Leviathan (Stage 3+). Bosses: Clearcut Behemoth (Stage 1), Factory Titan (Overdrive, Stage 4), Typhoon Tyrant
(Storm Surge, Stage 5). Elites keep +50% HP and +1 damage, take their base unit's category and trait, and get Speed +1.

## Database (SQLite)

Player data is stored offline in **`thermo_tactics.db`** in `Application.persistentDataPath`
(Windows editor: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Thermo-Tactics\`; Android: the app's private storage).
It is accessed with [SQLite-net for Unity](https://github.com/gilzoide/unity-sqlite-net) (`com.gilzoide.sqlite-net` 1.3.2, MIT)
by one script, `Scripts/Persistence/ResultsDatabase.cs`. The schema version is kept in `PRAGMA user_version` (now **5**).

| Table | One row per | Used for |
| --- | --- | --- |
| `game_results` | finished level | Results list (name, WIN / LOSS, date, final °C, waves cleared) |
| `level_progress` | player + stage + level | Unlocking levels; attempts; best (lowest) winning °C; best stars |
| `saved_session` | player | Continue: level, wave, turn, Oxygen, °C and peak °C, boss turn, units with HP and status effects (JSON), squad |
| `quiz_results` | player + stage + quiz type | First pre / post quiz answers and score (study data) |
| `facts_seen` | player + fact | Climate facts already shown |
| `wallet` | player | Carbon Credits balance and total earned |
| `owned_units` | player + unit | Allies bought in the store (the 5 starters are owned by everyone and not stored) |

Reset my progress clears `level_progress`, `saved_session`, `wallet` and `owned_units`; results, quizzes and seen facts stay.
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
