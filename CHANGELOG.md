# Changelog

Thermo-Tactics uses semantic versioning, **MAJOR.MINOR.PATCH**:

- **PATCH** (0.3.0 → 0.3.1): bug fixes only, no new features, saved games still load.
- **MINOR** (0.3.0 → 0.4.0): a new working feature that keeps existing saves working.
- **MAJOR** (1.x → 2.0.0): a change that breaks saved-data compatibility or redesigns the game.
- Versions below **1.0.0** are pre-release: features may still change between minor versions.

Each release is built with **Thermo-Tactics ▸ Release ▸ Build Android APK** from `main`, tagged `vX.Y.Z` in Git,
and published as `Thermo-Tactics-vX.Y.Z.apk` under the repository's GitHub Releases.

## [0.5.0] — player codes, leaderboard and admin role

Release type: **Testing**. Saved games from 0.4.0 still load: the database only gains a new table.

### Added
- **Participant codes.** Each player name registered on a phone is given a code (A-001, A-002, …) the first time
  it presses Start. The code is shown in the player profile and is used in the quiz CSV, so a player's pre-quiz and
  post-quiz can be matched without writing down extra personal details. One name is one player per phone: capitals
  and extra spaces are ignored, and a code is never given to a second player.
- **"Is this you?" check.** Typing a name that is already registered, when it is not the last player on the phone,
  asks to confirm before continuing, and suggests adding a surname or initial. This keeps two students with the same
  first name from sharing one set of results on a shared phone.
- **Leaderboard**, replacing the Results screen everywhere players see it (Settings, the end-of-level button and the
  Level Complete and Stage Complete screens). It ranks the players on the phone by their total stars across every
  stage, with equal stars sharing a rank, and highlights the current player's row. Quiz scores are not shown.
- **Admin role.** Settings has an ADMIN section: entering the admin code unlocks the admin tools until the app is
  closed, and a LOCK button ends it sooner. An ADMIN tag shows on the title screen while it is unlocked. Admin-only
  tools, greyed out otherwise: the full results table (with codes), the quiz CSV export, and the phone letter used
  for new codes. Resetting your own progress, the credits and the leaderboard stay open to players.
- **Security Checks** test (Thermo-Tactics ▸ Test ▸ Security Checks): 11 negative tests covering access control,
  the admin code, player names, quiz scores, missing records and a damaged save file. It writes
  `Logs/qa/security/report.txt` and runs on a separate test database, so the real save file is untouched.

### Changed
- The quiz CSV now starts with the player's participant code and is named `quiz_results_phone<letter>_<date>.csv`,
  so files collected from several phones can be merged without clashing.
- The optional "tester code" field was removed from the player profile. Codes are now given automatically, and the
  phone letter is set once by an admin instead of typed by each player.
- Player names are limited to letters, numbers, spaces and `. - '`. Other symbols are refused, both on the name
  field and in the database code.
- The admin code is stored only as a salted PBKDF2-SHA256 hash (100,000 rounds) and is compared in constant time,
  so it cannot be read out of the APK and is slow to guess. Three wrong codes in a row start a 30-second wait.
- Admin unlock, lock, wrong codes and refused exports are written to the device log (never the code itself).
- The export and the full results table check the admin state in the code that does the work, not only by greying
  out their buttons.
- A damaged or unreadable save file now logs a short message and leaves the game running with empty results,
  instead of stopping the screen that asked for the data.

### Known issues
- The quiz CSV fills the per-question and per-topic columns for Stage 1 only; Stage 2 rows carry their scores and
  gain but leave those columns blank.
- The APK is still signed with the default debug key. A release key, kept outside the repository, is planned.
- Access control is enforced inside the app. The game is offline with no server, so a rooted phone could still read
  the database file directly. This is accepted for the prototype.

## [0.4.0] — Stage 2 and the matched-pair quizzes

Release type: **Testing**.

### Added
- **Stage 2 · Sierra Madre Coast** (Aurora and Isabela): 10 coastal levels from Sabang Beach in Baler to the
  Palanan Coast, with a difficulty spike at Level 5 (Dinadiawan Beach) and a boss at Level 10. It opens once all
  10 Baguio levels are cleared.
- New enemies in play: Toxic Runoff (poison), Diesel Drone (hovering swarm) and Coal Brute (soot, +0.05 °C per hit),
  with elite versions from Level 5.
- **Typhoon Tyrant** boss (340 HP): its Storm Band hits 3 neighbouring lanes and moves one lane every turn (shown in
  advance); its Storm Surge hits all 6 lanes every 3rd turn, 1 less for each living Forest ally (max −3). Below
  40% HP it becomes a Super Typhoon: a 4-lane band and a surge every 2nd turn.
- New coastal battle background, sea-spray overlay, darker slot markers on the sand, Stage 2 card art and a coastal
  level path in the same layout as Baguio's, recoloured to the coast palette.
- 9 climate-fact pop-ups for Stage 2 (typhoons, mangroves and blue carbon, the Sierra Madre, sea turtles).
- Stage 2 allies (Hydro Guardian, Seagrass Healer, Sea Turtle Guardian, Eagle Striker) on sale once Baguio is cleared.
- Wide phones (wider than 16:9) show a soft, darkened extension of the background at the sides instead of black bars;
  the game view itself stays 16:9.
- **Pre- and post-quizzes for Stage 2**, matching Stage 1's design.
- **Recap cards** before the post-quiz: a short refresher of the climate facts met during the stage, so the
  post-quiz measures what was learned rather than what was remembered from one reading.

### Changed
- **The quizzes were rebuilt as matched pairs.** Each awareness question in the pre-quiz has a partner in the
  post-quiz on the same concept, so the two scores can be compared question by question. Learning is reported as
  the normalized gain, (post − pre) ÷ (highest possible − pre), which shows how much of the room left for
  improvement a player actually gained.
- Questions about the game's own rules are kept, but **not scored**. They are reported separately as game
  comprehension, so the awareness score measures climate knowledge only.
- The answer choices are shuffled for each player, so a remembered position cannot be the answer.
- The quiz result screen reports the gain and the per-concept comparison, and marks the game-rule row as
  "Game check, not scored".
- The menus, Continue, the store and the stage-clear screen now work with several stages; texts that said "Baguio"
  name the current stage.
- The Level 1 tutorial only runs in Stage 1.
- The boss shadow is soft instead of a large hard-edged oval.
- Slot rings are per stage. On the coast sand they have a dark edge so the blue deploy ring, the gold "picked up"
  ring, the red boss warning and the yellow high-contrast ring stand out from the sand.
- The "picked up" ring is now a true gold in Baguio too (it showed olive green before).
- Editor: pressing Play always starts from the Main Menu (Thermo-Tactics ▸ Always Play From Main Menu turns it off),
  and the Main Menu scene opens on the title screen instead of showing the Results panel.

### Fixed
- The level details panel no longer overlaps the squad line and the saved-game note.
- "Level N is now unlocked" is shown only the first time a level is cleared, not on every replay.
- The quiz result screen fits all 7 concepts and all 8 questions without cutting off a row.
- A full post-quiz score (6 of 6) was rejected by the database and the result was lost; the quiz table now accepts
  any score up to the number of scored questions.

### Known issues
- Stages 3–5 are still locked ("coming soon").
- The quiz CSV fills the per-question and per-topic columns for Stage 1 only.

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
