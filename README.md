# Thermo-Tactics

A Philippine climate-themed 2D turn-based strategy game for Android, built in Unity.
Players defend Philippine ecosystems from fossil-fuel threats while keeping the global
temperature below the **1.5 °C** limit. Capstone project.

> **Status:** Stage 1 (Baguio — Kennon Road) playable prototype · Level 1 · 3 waves

---

## Requirements

| Item | Version |
| --- | --- |
| Unity | **6 (6000.0.84f1)** — Universal Render Pipeline 2D |
| Target | Android 8.0+ (API 26), landscape |
| Input | Unity Input System (touch and mouse) |
| Packages | 2D feature set, uGUI, Input System, SQLite-net (installed through `Packages/manifest.json`; Git must be installed) |

## Getting started

1. Clone the repository:
   ```bash
   git clone https://github.com/jonfranss221/CAPSTONE.git
   ```
2. Open the folder in **Unity Hub → Add → Add project from disk** (Unity 6000.0.84f1).
   The first open rebuilds the `Library/` folder and takes a few minutes.
3. Open `Assets/ThermoTactics/Scenes/Baguio_Level1.unity` and press **Play**.

## How to play

1. Enter a **username** and press **Start**.
2. Tap a unit card in the deploy bar, then tap a glowing ally slot on the road.
3. Tap **End Turn**: allies attack, then enemies attack, then the temperature is checked.
4. Clear all **3 waves** while the temperature stays **below 1.5 °C** to win.
5. Press **Results** (login or end screen) to see every saved result.

| Rule | Value |
| --- | --- |
| Starting temperature | 1.00 °C |
| Enemy hits an undefended lane | +0.10 °C |
| Ally attacks an empty lane | −0.05 °C |
| Loss | temperature reaches 1.50 °C |
| Oxygen per turn | Wave 1: 5 · Wave 2: 7 · Wave 3: 10 (unspent OP does not carry over) |
| Oxygen punishment | turn 2: −1 · turn 3: −2 · turn 4: −4 · turn 5+: −8 (minimum 1) |

## Implemented features

- Username input with validation (2–20 characters)
- Baguio environment with drifting fog and 12 deployment slots (6 lanes × ally/enemy)
- 5 ally units and 5 enemy units, each with **idle**, **attack** and **hurt** animations
- Oxygen Point system with punishment for long waves
- Wave logic (3 waves) and turn order
- Win / loss at the 1.5 °C threshold
- Local **SQLite** results database (name, WIN/LOSS, date created) shown in the **Results** panel

## Project structure

```
Assets/ThermoTactics/
├── Art/            Units/<Unit>/ (idle, attack, hurt frames + card) · Environment/ (Baguio)
├── Animations/     Idle / Attack / Hurt clips + Animator controller per unit
├── GameData/       UnitData (10) · WaveData (3) · LevelData (Baguio_Level1) — tune numbers here
├── Scenes/         Baguio_Level1.unity
├── Scripts/
│   ├── Core/          GameManager, TurnManager, WaveManager, OxygenPointSystem, TemperatureSystem
│   ├── Board/         GridManager, BoardSlot, IsoBoardLayout, LaneController, DeploySystem, UnitFactory
│   ├── Units/         UnitBase, AllyUnit, EnemyUnit
│   ├── Data/          UnitData, WaveData, LevelData (ScriptableObjects)
│   ├── UI/            UIManager, DeployCard, ResultsPanel
│   ├── Persistence/   ResultsDatabase
│   └── Presentation/  FogScroller
└── Editor/         ThermoTacticsBuilder (menu: Thermo-Tactics ▸ Build Baguio Level)
```

To rebuild the scene after changing art or stats, use **Thermo-Tactics ▸ Build Baguio Level**.
Rebuilding replaces the scene, so make manual scene edits after rebuilding.

## Results database (SQLite)

Results are stored offline in a **SQLite** database, `thermo_tactics.db`, in `Application.persistentDataPath`.
On Windows, the editor puts it in `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\thermo_tactics.db`; on Android it lives in the app's private storage.
The database is accessed with [SQLite-net for Unity](https://github.com/gilzoide/unity-sqlite-net) (`com.gilzoide.sqlite-net` 1.3.2, MIT). Unity installs it from `Packages/manifest.json`, which needs Git to be installed.
Script: `Assets/ThermoTactics/Scripts/Persistence/ResultsDatabase.cs`.

```sql
CREATE TABLE game_results (
  id                INTEGER PRIMARY KEY AUTOINCREMENT,
  player_name       TEXT    NOT NULL CHECK (length(player_name) BETWEEN 1 AND 20),
  result            TEXT    NOT NULL CHECK (result IN ('WIN','LOSS')),
  created_at        TEXT    NOT NULL,          -- yyyy-MM-dd HH:mm:ss
  final_temperature REAL    NOT NULL,          -- °C when the level ended
  waves_cleared     INTEGER NOT NULL DEFAULT 0 CHECK (waves_cleared >= 0)
);
CREATE INDEX idx_game_results_created_at ON game_results(created_at);
```

| Operation | SQL |
| --- | --- |
| Save a result (end of level) | `INSERT INTO game_results (player_name, result, created_at, final_temperature, waves_cleared) VALUES (?, ?, ?, ?, ?)` |
| Results panel | `SELECT ... FROM game_results ORDER BY id DESC` |

Values are bound as parameters (`?`), never concatenated into the SQL. Results saved by the older JSON version (`results_db.json`) are imported automatically the first time the game runs.
The `.db` file can be opened with [DB Browser for SQLite](https://sqlitebrowser.org/) for checking.

## Version control

| Item | Status |
| --- | --- |
| `.gitignore` | Present — Unity template; excludes `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `UserSettings/`, IDE files and builds |
| `.gitattributes` | Present — text/LF rules for Unity YAML; Git LFS rules for audio and layered art |
| Git LFS | Configured for `*.wav *.mp3 *.ogg *.psd *.aseprite *.mp4` (no such files yet) |
| Branches | `main` = stable · `develop` = integration · `feature/<name>` per system |

Commit `.meta` files together with their assets. Unity settings: Version Control mode
**Visible Meta Files**, Asset Serialization **Force Text**.

## Team

| Member | Role |
| --- | --- |
| [ name ] | [ role ] |
| [ name ] | [ role ] |
| [ name ] | [ role ] |
