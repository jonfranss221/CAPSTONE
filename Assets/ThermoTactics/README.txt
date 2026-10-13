THERMO-TACTICS · Stage 1 (Baguio) · version 0.2.0

Open:   Assets/ThermoTactics/Scenes/MainMenu.unity  → press Play.
        (Baguio_Battle.unity can be played directly; it starts the level set on its GameManager.)
Rebuild after changing art/stats:  menu  Thermo-Tactics ▸ Build Everything
        (single steps: Thermo-Tactics ▸ Steps ▸ 1. Import Art / 2. Build Animations / 3. Build Main Menu)
Rebuilding replaces the scenes, so make manual scene edits after rebuilding.

Folders
  Art/Units/<Unit>/              idle_00-11, attack_00-11, hurt_00-07 frames + deploy card
  Art/Bosses/ClearcutBehemoth/   idle, enraged, cleave, slam, hurt frames + card
  Art/Environment/               Baguio background, fog, slot markers, shadow
  Art/UI/                        icons, woven border, stage thumbnails, level-path background
  Animations/<Unit>/             Idle / Attack / Hurt clips + Animator controller (boss: + Enraged, Cleave, Slam)
  Fonts/                         Pixelify Sans, Atkinson Hyperlegible (SIL Open Font Licence)
  GameData/                      Stage1_Baguio, Levels/ (Baguio_Level1-10), Waves/ (35), Units/ (20) — tune numbers here
  Scripts/Core|Board|Units|Data|UI|UI/Menu|Persistence|Presentation
  Editor/                        ThermoTacticsBuilder.cs, MenuSceneBuilder.cs, UiKit.cs

Rules
  Start 1.00°C. Lose when the temperature reaches 1.50°C.
  Enemy attacks an undefended lane: +0.10°C.  Ally attacks an empty enemy slot: −0.05°C (once per ally phase).
  Oxygen each turn = wave allowance (5-11) minus penalty (turn 2: −1, 3: −2, 4: −4, 5+: −8), minimum 2.
  The boss wave has no penalty. Unspent Oxygen does not carry over.
  Win by clearing all of the level's waves below 1.50°C. Clearing a level unlocks the next.
  Each level offers at most 5 unit types in the deploy bar.

Boss (Level 10, wave 5): Clearcut Behemoth, 300 HP, fills all 6 enemy slots.
  Cleave 8 to 3 lanes; Landslide Slam 7 to all lanes every 3rd turn; enrage at 40% HP (cleave 10, slam every 2nd turn).

Database (SQLite, schema version 2)
  Application.persistentDataPath/thermo_tactics.db
  game_results   (id, player_name, result, created_at, final_temperature, waves_cleared)
  level_progress (player_name, stage_id, level_number, is_cleared, attempts, best_final_temp)
  saved_session  (one saved game per player for Continue: level, wave, turn, Oxygen, °C, units with HP)
  Windows editor: %USERPROFILE%\AppData\LocalLow\DefaultCompany\Thermo-Tactics\thermo_tactics.db
  Package: com.gilzoide.sqlite-net 1.3.2 (Packages/manifest.json)
Settings: PlayerPrefs keys "tt.*" (Scripts/Core/SettingsStore.cs)
