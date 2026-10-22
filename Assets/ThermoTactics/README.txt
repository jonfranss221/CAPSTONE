THERMO-TACTICS · Stage 1 (Baguio) · version 0.2.0

Open:   Assets/ThermoTactics/Scenes/MainMenu.unity  → press Play.
        (Baguio_Battle.unity can be played directly; it starts the level set on its GameManager.)
Rebuild after changing art/stats:  menu  Thermo-Tactics ▸ Build Everything
        (single steps: Thermo-Tactics ▸ Steps ▸ 1. Import Art / 2. Build Animations / 3. Build Main Menu)
Automatic test: Thermo-Tactics ▸ Test ▸ Play Stage 1 Automatically → Logs/qa/auto/report.txt
Quick check:    Thermo-Tactics ▸ Test ▸ Quick Check of New Screens → Logs/qa/check/report.txt
Rebuilding replaces the scenes, so make manual scene edits after rebuilding.

Folders
  Art/Units/<Unit>/              idle_00-11, attack_00-11, hurt_00-07 frames + deploy card
  Art/Bosses/ClearcutBehemoth/   idle, enraged, cleave, slam, hurt frames + card
  Art/Environment/               Baguio background, fog, slot markers, shadow
  Art/UI/                        icons, woven border, stage thumbnails, level-path background
  Animations/<Unit>/             Idle / Attack / Hurt clips + Animator controller (boss: + Enraged, Cleave, Slam)
  Fonts/                         Pixelify Sans, Atkinson Hyperlegible (SIL Open Font Licence)
  GameData/                      Stage1_Baguio, Levels/ (Baguio_Level1-10), Waves/ (35), Units/ (20) — tune numbers here
  Scripts/Core|Board|Units|Data|UI|UI/Menu|Persistence|Presentation|Dev (Dev = editor-only play-test)
  Editor/                        ThermoTacticsBuilder.cs, MenuSceneBuilder.cs, UiKit.cs, PlaytestMenu.cs

Rules
  Each level opens with an intro (fade in, camera glide, title card; tap to skip).
  Level 1 tutorial: shown the first time a player starts Level 1 (PlayerPrefs "tt.tutorial_done.<player>"),
  skippable, replayed from Pause ▸ HOW TO PLAY. Settings ▸ Reset my progress shows it again.
  Start 1.00°C. Lose when the temperature reaches 1.50°C.
  End Turn goes lane by lane (1 to 6): the ally attacks, then the enemy hits back; the camera zooms in on each lane.
  Enemy attacks an undefended lane: +0.10°C.  Ally attacks an empty enemy slot: −0.05°C (once per turn).
  Oxygen each turn = wave allowance (5-11) minus penalty (turn 2: −1, 3: −2, 4: −4, 5+: −8), minimum 2.
  The boss wave has no penalty. Unspent Oxygen does not carry over.
  Win by clearing all of the level's waves below 1.50°C. Clearing a level unlocks the next.
  Each level offers at most 5 unit types in the deploy bar.
  Stars (1–3) from the highest temperature reached: 3 at ≤ 1.20°C, 2 at ≤ 1.35°C.

Boss (Level 10, wave 5): Clearcut Behemoth, 300 HP, fills all 6 enemy slots.
  Cleave 8 to 3 lanes; Landslide Slam 7 to all lanes every 3rd turn; enrage at 40% HP (cleave 10, slam every 2nd turn).

Education (pre-quiz, climate facts, post-quiz)
  Pre-quiz: 5 questions, shown the first time a player opens the Baguio card in Stage Select.
  Climate facts: pop-ups in Levels 1–9 (level start or after a wave), each tied to one quiz topic; shown once per player,
    rereadable in CLIMATE FACTS on the level path. Settings ▸ "Climate fact pop-ups" turns them off.
  Post-quiz: 5 different questions on the same topics (bank Q6–Q10; pre-quiz is Q1–Q5) after Level 10 is won, then a
    topic-by-topic table (questions right before → after: Climate basics, Forests & carbon, Game & real world) and the
    knowledge gain ((post − pre) / pre × 100, N/A when pre = 0 or missing). A retake keeps the first score (attempts + 1).
  Content: ThermoTacticsBuilder.BaguioTopics / BaguioPre / BaguioPost / BaguioFacts → GameData/Quizzes, GameData/Facts
    (Build Everything).
  Export: Settings ▸ Data ▸ Export quiz results → Documents/ThermoTactics/quiz_results_<tester>_<date>.csv
    (Android: /storage/emulated/0/Documents/ThermoTactics; falls back to persistentDataPath/Exports).

Database (SQLite, schema version 4)
  Application.persistentDataPath/thermo_tactics.db
  game_results   (id, player_name, result, created_at, final_temperature, waves_cleared)
  level_progress (player_name, stage_id, level_number, is_cleared, attempts, best_final_temp, best_stars)
  saved_session  (one saved game per player for Continue: level, wave, turn, Oxygen, °C, peak °C, units with HP)
  quiz_results   (player_name, stage_id, quiz_type pre/post, answers "1,1,2,2,1" (indexes into that quiz's questions),
                  score, max_score, taken_at, attempts)
  facts_seen     (player_name, fact_id, seen_at)
  Reset my progress keeps game_results, quiz_results and facts_seen.
  Windows editor: %USERPROFILE%\AppData\LocalLow\DefaultCompany\Thermo-Tactics\thermo_tactics.db
  Package: com.gilzoide.sqlite-net 1.3.2 (Packages/manifest.json)
Settings: PlayerPrefs keys "tt.*" (Scripts/Core/SettingsStore.cs)
