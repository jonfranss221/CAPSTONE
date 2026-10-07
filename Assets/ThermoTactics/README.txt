THERMO-TACTICS · Baguio Level 1 prototype

Open:   Assets/ThermoTactics/Scenes/Baguio_Level1.unity  → press Play.
Rebuild after changing art/stats:  menu  Thermo-Tactics ▸ Build Baguio Level

Folders
  Art/Units/<Unit>/        idle_00-11, attack_00-11, hurt_00-07 frames + deploy card
  Art/Environment/         Baguio background, fog, slot markers, shadow
  Animations/<Unit>/       Idle / Attack / Hurt clips + Animator controller (triggers: Attack, Hurt)
  GameData/                UnitData (10), WaveData (3), LevelData (Baguio_Level1) — tune numbers here
  Scripts/Core|Board|Units|Data|UI|Persistence|Presentation
  Editor/ThermoTacticsBuilder.cs

Rules in this level
  Start 1.00°C. Lose when the temperature reaches 1.50°C.
  Enemy attacks an undefended lane: +0.10°C.  Ally attacks an empty lane: −0.05°C.
  Oxygen each turn = wave allowance (5 / 7 / 10) minus punishment (turn 2: −1, 3: −2, 4: −4, 5+: −8), minimum 1.
  Unspent Oxygen does not carry over. Win by clearing all 3 waves below 1.50°C.

Results database
  Application.persistentDataPath/results_db.json  (id, player_name, result, created_at, final_temperature, waves_cleared)
  Windows editor: %USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\results_db.json
