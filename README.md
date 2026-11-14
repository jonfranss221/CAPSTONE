# Thermo-Tactics

A Philippine climate-themed 2D turn-based strategy game for Android, built in Unity.
Players defend Philippine ecosystems from fossil-fuel threats while keeping the global
temperature below the **1.5 °C** limit. Capstone project.

> **Status (v0.4.0):** Stage 1 (Baguio Forest) and Stage 2 (Sierra Madre Coast) playable from the Main Menu: 10 levels
> each with unlocking and 1–3 stars, the full roster with Speed, counters, traits and synergies, Carbon Credits, the Store
> and squad selection, moving and taking back units, a boss per stage (Clearcut Behemoth, Typhoon Tyrant), matched-pair pre- and post-quizzes
> with recap cards for both stages, and save / Continue.
> Download the APK from [Releases](https://github.com/jonfranss221/CAPSTONE/releases); changes are listed in [CHANGELOG.md](CHANGELOG.md).

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
   (`Baguio_Battle.unity` is the battle scene for every stage; played directly it starts Baguio Level 1.)

## How to play

1. On the title screen, type a **player name** (2–20 characters). A tester code is optional.
2. Press **Start**, pick a stage, then a level on the path. Level 1 is open; clearing a level opens the next.
   **Sierra Madre Coast** opens once all 10 Baguio levels are cleared.
   Every level opens with a short intro: the screen fades in, the camera glides onto the road and a title card shows
   the stage, level and place (tap to skip). The first time a player starts Level 1, a **tutorial** walks through the
   screen step by step: you really pick a card, deploy and end a turn. It can be skipped and replayed from
   **Pause ▸ How to Play**.
3. Tap a unit card in the deploy bar, then a glowing ally slot on the road. Each level offers up to 5 unit types.
   Tap a unit already on the road to pick it up (gold ring), then tap an empty slot to move it. A unit placed **this
   turn** moves for free, can be swapped by tapping another card (its OP is refunded first) or removed with
   **TAKE BACK** for a full refund, so a mis-tap costs nothing. Pressing **End Turn** locks it in place. Moving a unit
   placed in an **earlier** turn is allowed only **before any Oxygen is spent** that turn, uses **all of it**, and
   only into an empty lane.
4. Tap **End Turn**. The camera zooms in on lanes 1 to 6 in turn: in each lane your unit attacks, then the enemy
   (if still standing) hits back. A banner shows what happened in each lane. Settings can turn the zoom off.
5. Clear every wave while the temperature stays **below 1.5 °C** to win. You earn **1–3 stars** from the highest
   temperature reached: 3 stars at 1.20 °C or below, 2 at 1.35 °C or below.
6. After a level: **Next Level**, **Retry**, **Level Select** or **Stage Select**. Beating Level 10 shows the
   **Stage Complete** screen with your star total.
7. The game saves itself at the start of every turn, after every deploy, move or take-back and when the app is paused or closed.
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

### Sierra Madre Coast levels (Stage 2)

| Level | Location | Waves | Oxygen per wave | Notes |
| --- | --- | --- | --- | --- |
| 1 | Sabang Beach | 3 | 6 · 7 · 8 | New: Toxic Runoff |
| 2 | Cemento Beach | 3 | 6 · 8 · 9 | New: Diesel Drone |
| 3 | Digisit | 3 | 6 · 8 · 10 | New: Coal Brute |
| 4 | Dicasalarin Cove | 3 | 7 · 9 · 10 | |
| 5 | Dinadiawan Beach | 4 | 7 · 9 · 10 · 11 | Difficulty spike: elite enemies |
| 6 | Casiguran Sound | 3 | 7 · 8 · 10 | |
| 7 | Casapsapan Beach | 3 | 7 · 9 · 10 | |
| 8 | Dilasag Coast | 4 | 7 · 9 · 10 · 11 | |
| 9 | Divilacan Bay | 4 | 7 · 9 · 10 · 11 | |
| 10 | Palanan Coast | 5 | 7 · 9 · 10 · 11 · 8 | Wave 5: Typhoon Tyrant (boss) |

**Typhoon Tyrant:** 340 HP (323 at Level 10's ×0.95), hovering, fills all 6 enemy slots. **Storm Band** hits 3 neighbouring lanes for 5 and moves one
lane every turn (1-2-3, 2-3-4 … and back), so the next band is always known. **Storm Surge** hits all 6 lanes for 8 every
3rd turn, 1 less for each living Forest ally (max −3). Below 40% HP it becomes a **Super Typhoon**: a 4-lane band for 7 and a
surge every 2nd turn. Mangrove Guardian and Sea Turtle Guardian take half damage from it.

**Clearcut Behemoth:** 300 HP and fills all 6 enemy slots. **Cleave** hits the lane of your strongest unit and the lanes beside it for 8.
**Landslide Slam** hits all 6 lanes for 7 every 3rd turn. At 40% HP it **enrages**: cleave 10, slam every 2nd turn.
The boss panel always shows its next move.

## Implemented features

- Main Menu: living background (pan, fog, pollen), player profile, Continue, Start, Settings, Quit (with confirmation),
  Credits, Results, and a progress strip with stars for the current stage; buttons and screens animate
- Stage Select (Baguio open; Sierra Madre Coast opens after Baguio; three more stages locked) and a 10-node level path per
  stage with stars and best temperatures (same layout, recoloured per stage)
- **Stages share one battle scene**: `StageEnvironment` swaps in the stage's background, side fill, weather overlay (Baguio fog,
  coastal sea spray), slot markers and camera colour. Wider-than-16:9 phones get a soft, darkened extension at the sides
- Settings: music and effects volume (used once audio is added), battle speed (1× / 1.5× / 2×), large text, high contrast, climate pop-ups,
  damage numbers, boss warning, lane camera zoom, and resetting a player's progress
- 13 unit types in play in Baguio (5 allies, 8 enemies) and 6 elite variants; Stage 2 adds Coal Brute, Diesel Drone and Toxic Runoff with 3 more elites; each with **idle**, **attack** and **hurt** animations
- Full roster data (roster v3, Phase 1): all 30 characters (15 allies, 15 enemies) plus the Factory Titan and Typhoon Tyrant
  bosses exist as unit assets with art and animations (see *Roster* below)
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
- **Moving and taking back units**: a unit placed this turn can be moved for free, swapped for another unit or taken back
  with a full Oxygen refund until End Turn; a unit from an earlier turn can move to an empty lane only before any Oxygen is
  spent that turn, and the move uses all of it
- Clearcut Behemoth boss (cleave, Landslide Slam, enrage) and Typhoon Tyrant boss (moving Storm Band, Storm Surge with a
  forest shield, Super Typhoon); HUD boss panel and danger rings on target lanes
- Oxygen system with turn penalty, minimum of 2 and no penalty in the boss wave
- Lane-by-lane End Turn with a zooming camera and a lane banner
- Level intro (fade in, camera glide, title card with stage, level, place and waves) and a hands-on Level 1 tutorial
  with a coach box and highlight, shown once per player, skippable, replayable from Pause ▸ How to Play
- Pause menu; Level Complete (stars) / Level Failed screens with Next Level, Retry, Level Select and Stage Select;
  Stage Complete screen after Level 10
- Local **SQLite** database: results, level progress (unlocks, best stars) and the saved game behind Continue

- Education: Stage 2 has 9 climate-fact pop-ups (typhoons, mangroves and blue carbon, the Sierra Madre, sea turtles) and no quiz yet.
  Stage 1: pre-quiz before Baguio, climate-fact pop-ups during Levels 1–9 (one per quiz topic) with a Climate Facts
  review list, post-quiz after Level 10 with a topic-by-topic comparison and knowledge gain, and a quiz CSV export for
  the study (database schema version 4: `quiz_results`, `facts_seen`)

Planned: audio, enemy movement (EnemyAI), a Stage 2 quiz, stages 3–5 with their own bosses.

## Project structure

```
Assets/ThermoTactics/
├── Art/            Units/<Unit>/ (28) · Bosses/ClearcutBehemoth, FactoryTitan, TyphoonTyrant · Environment/ (Baguio, coast) · UI/ (icons, thumbnails, level paths)
├── Animations/     Idle / Attack / Hurt clips + Animator controller per unit and for the boss
├── Fonts/          Pixelify Sans, Atkinson Hyperlegible (SIL Open Font Licence)
├── GameData/       Stage1_Baguio · Stage2_SierraMadreCoast · Levels/ (Baguio_Level1–10, Coast_Level1–10) · Waves/ (71) · Units/ (40: 28 units, 9 elites, 3 bosses)
├── Scenes/         MainMenu.unity · Baguio_Battle.unity
├── Scripts/
│   ├── Core/          GameManager, GameSession, SettingsStore, StarRating, TurnManager, WaveManager, OxygenPointSystem, TemperatureSystem,
│   │                  DamageRules, SynergyRules, CreditRules
│   ├── Board/         GridManager, BoardSlot, IsoBoardLayout, LaneController, DeploySystem, UnitFactory
│   ├── Units/         UnitBase, AllyUnit, EnemyUnit, BossUnit, StatusEffects
│   ├── Data/          StageData, LevelData, WaveData, UnitData, BossData (ScriptableObjects), UnitTraits (enums)
│   ├── UI/            UIManager, DeployCard, ResultsPanel, TextScaler, ButtonFx, PanelFx
│   │   └── Menu/      MainMenuController, MenuAmbience, SettingsPanel, StageSelectPanel, LevelSelectPanel, LevelNode, StageThemes
│   ├── Models/        LevelResult, SavedLevelResult, StageSummary (data passed from the game logic to the services)
│   ├── Services/      ProgressService (saves a finished level and reads stage progress; GameManager.EndLevel goes through it)
│   ├── Persistence/   ResultsDatabase, SessionSnapshot
│   ├── Presentation/  FogScroller, BattleCamera, StageEnvironment
│   └── Dev/           AutoPlaytest, QuickCheck (editor only)
└── Editor/         ThermoTacticsBuilder, MenuSceneBuilder, RosterArtImporter, UiKit, PlaytestMenu
```

**Thermo-Tactics ▸ Build Everything** imports the art, builds the animations, both stages (20 levels) and both scenes.
Single steps are under **Thermo-Tactics ▸ Steps**. **Thermo-Tactics ▸ Test ▸ Play Stage 1 (or 2) Automatically** plays all 10 levels
by itself as the player "Playtest" and saves a report and screenshots to `Logs/qa/auto/` or `Logs/qa/auto_stage2/` (not committed).
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
Landfill Leviathan (Stage 3+). Bosses: Clearcut Behemoth (Stage 1), Typhoon Tyrant (Storm Band and Storm Surge, Stage 2),
Factory Titan (Overdrive, a later stage). Elites keep +50% HP and +1 damage, take their base unit's category and trait, and get Speed +1.

## Database (SQLite)

Player data is stored offline in **`thermo_tactics.db`** in `Application.persistentDataPath`
(Windows editor: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Thermo-Tactics\`; Android: the app's private storage).
It is accessed with [SQLite-net for Unity](https://github.com/gilzoide/unity-sqlite-net) (`com.gilzoide.sqlite-net` 1.3.2, MIT)
by one script, `Scripts/Persistence/ResultsDatabase.cs`. The schema version is kept in `PRAGMA user_version` (now **6**).

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

## Building a release

1. Set `Version` in `Assets/ThermoTactics/Editor/ReleaseBuilder.cs` (MAJOR.MINOR.PATCH, see [CHANGELOG.md](CHANGELOG.md))
   and add the release notes to `CHANGELOG.md`.
2. In Unity, run **Thermo-Tactics ▸ Release ▸ Build Android APK** (needs Android Build Support in Unity Hub).
   It sets the version and Android versionCode, rebuilds the scenes and data, and writes a release (non-development) build to
   `Builds/Thermo-Tactics-vX.Y.Z.apk` with a build record (`Builds/Thermo-Tactics-vX.Y.Z-build.txt`: size, SHA-256, scenes).
   `Builds/` is not tracked by Git.
3. Commit the version change, tag it (`git tag vX.Y.Z`, `git push origin vX.Y.Z`) and attach the APK to a GitHub Release for that tag.

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
