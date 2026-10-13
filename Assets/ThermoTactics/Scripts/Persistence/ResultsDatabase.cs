using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>One finished level, as stored in the results database.</summary>
    [Serializable]
    public class GameResult
    {
        public int id;
        public string player_name;
        public string result;          // "WIN" or "LOSS"
        public string created_at;      // "yyyy-MM-dd HH:mm:ss" (device local time)
        public float final_temperature;
        public int waves_cleared;
    }

    /// <summary>
    /// Row shape used by SQLite-net to read the game_results table.
    /// [Preserve] (and SQLite-net's [Column], which derives from it) keeps the setters from being stripped in IL2CPP (Android) builds.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    internal class GameResultRow
    {
        [Column("id")]                public int    Id               { get; set; }
        [Column("player_name")]       public string PlayerName       { get; set; }
        [Column("result")]            public string Result           { get; set; }
        [Column("created_at")]        public string CreatedAt        { get; set; }
        [Column("final_temperature")] public double FinalTemperature { get; set; }
        [Column("waves_cleared")]     public int    WavesCleared     { get; set; }

        public GameResult ToResult() => new GameResult
        {
            id = Id,
            player_name = PlayerName,
            result = Result,
            created_at = CreatedAt,
            final_temperature = (float)FinalTemperature,
            waves_cleared = WavesCleared,
        };
    }

    /// <summary>Row shape for the level_progress table.</summary>
    [UnityEngine.Scripting.Preserve]
    public class LevelProgress
    {
        [Column("stage_id")]        public int     StageId       { get; set; }
        [Column("level_number")]    public int     LevelNumber   { get; set; }
        [Column("is_cleared")]      public int     IsCleared     { get; set; }
        [Column("attempts")]        public int     Attempts      { get; set; }
        [Column("best_final_temp")] public double? BestFinalTemp { get; set; }
        [Column("best_stars")]      public int     BestStars     { get; set; }
    }

    /// <summary>Row shape for the saved_session table.</summary>
    [UnityEngine.Scripting.Preserve]
    internal class SavedSessionRow
    {
        [Column("player_name")]  public string PlayerName  { get; set; }
        [Column("stage_id")]     public int    StageId     { get; set; }
        [Column("level_number")] public int    LevelNumber { get; set; }
        [Column("wave_number")]  public int    WaveNumber  { get; set; }
        [Column("turn_number")]  public int    TurnNumber  { get; set; }
        [Column("oxygen_left")]  public int    OxygenLeft  { get; set; }
        [Column("oxygen_allowance")] public int OxygenAllowance { get; set; }
        [Column("oxygen_penalty")]   public int OxygenPenalty   { get; set; }
        [Column("temperature")]  public double Temperature { get; set; }
        [Column("boss_turn")]    public int    BossTurn    { get; set; }
        [Column("peak_temperature")] public double PeakTemperature { get; set; }
        [Column("allies")]       public string Allies      { get; set; }
        [Column("enemies")]      public string Enemies     { get; set; }
        [Column("saved_at")]     public string SavedAt     { get; set; }
        [Column("app_version")]  public string AppVersion  { get; set; }
    }

    /// <summary>
    /// Local database: SQLite file at Application.persistentDataPath/thermo_tactics.db
    /// (package com.gilzoide.sqlite-net, native SQLite for Android and Windows).
    ///
    /// Tables (schema version 3; version 3 added best_stars and peak_temperature):
    ///   game_results(id, player_name, result, created_at, final_temperature, waves_cleared) — one row per finished level
    ///   level_progress(player_name, stage_id, level_number, is_cleared, attempts, best_final_temp) — drives unlocking
    ///   saved_session(player_name, stage_id, level_number, wave_number, turn_number, …) — the Continue save, one per player
    /// Results recorded earlier by the JSON version (results_db.json) are imported once.
    /// </summary>
    public class ResultsDatabase : MonoBehaviour
    {
        public const int MaxNameLength = 20;
        public const int SchemaVersion = 3;
        private const string FileName = "thermo_tactics.db";
        private const string LegacyJsonName = "results_db.json";

        private const string CreateTableSql =
            "CREATE TABLE IF NOT EXISTS game_results (" +
            "  id                INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  player_name       TEXT    NOT NULL CHECK (length(player_name) BETWEEN 1 AND 20)," +
            "  result            TEXT    NOT NULL CHECK (result IN ('WIN','LOSS'))," +
            "  created_at        TEXT    NOT NULL," +
            "  final_temperature REAL    NOT NULL," +
            "  waves_cleared     INTEGER NOT NULL DEFAULT 0 CHECK (waves_cleared >= 0)" +
            ")";

        private const string CreateIndexSql =
            "CREATE INDEX IF NOT EXISTS idx_game_results_created_at ON game_results(created_at)";

        private const string InsertSql =
            "INSERT INTO game_results (player_name, result, created_at, final_temperature, waves_cleared) " +
            "VALUES (?, ?, ?, ?, ?)";

        private const string SelectAllSql =
            "SELECT id, player_name, result, created_at, final_temperature, waves_cleared " +
            "FROM game_results ORDER BY id DESC";

        // Version 2: progress and the Continue save. Player names match without case ("Jon" = "jon").
        private const string CreateProgressSql =
            "CREATE TABLE IF NOT EXISTS level_progress (" +
            "  player_name     TEXT    NOT NULL COLLATE NOCASE," +
            "  stage_id        INTEGER NOT NULL CHECK (stage_id BETWEEN 1 AND 5)," +
            "  level_number    INTEGER NOT NULL CHECK (level_number BETWEEN 1 AND 10)," +
            "  is_cleared      INTEGER NOT NULL DEFAULT 0 CHECK (is_cleared IN (0,1))," +
            "  attempts        INTEGER NOT NULL DEFAULT 0 CHECK (attempts >= 0)," +
            "  best_final_temp REAL," +
            "  best_stars      INTEGER NOT NULL DEFAULT 0 CHECK (best_stars BETWEEN 0 AND 3)," +
            "  PRIMARY KEY (player_name, stage_id, level_number))";

        private const string CreateSessionSql =
            "CREATE TABLE IF NOT EXISTS saved_session (" +
            "  player_name      TEXT    NOT NULL PRIMARY KEY COLLATE NOCASE," +
            "  stage_id         INTEGER NOT NULL," +
            "  level_number     INTEGER NOT NULL," +
            "  wave_number      INTEGER NOT NULL CHECK (wave_number >= 1)," +
            "  turn_number      INTEGER NOT NULL CHECK (turn_number >= 1)," +
            "  oxygen_left      INTEGER NOT NULL CHECK (oxygen_left >= 0)," +
            "  oxygen_allowance INTEGER NOT NULL," +
            "  oxygen_penalty   INTEGER NOT NULL DEFAULT 0," +
            "  temperature      REAL    NOT NULL," +
            "  boss_turn        INTEGER NOT NULL DEFAULT 0," +
            "  peak_temperature REAL    NOT NULL DEFAULT 0," +
            "  allies           TEXT    NOT NULL," +
            "  enemies          TEXT    NOT NULL," +
            "  saved_at         TEXT    NOT NULL," +
            "  app_version      TEXT    NOT NULL)";

        // A win sets is_cleared (never back to 0) and keeps the lowest winning temperature.
        private const string RecordAttemptSql =
            "INSERT INTO level_progress (player_name, stage_id, level_number, is_cleared, attempts, best_final_temp, best_stars) " +
            "VALUES (?, ?, ?, ?, 1, ?, ?) " +
            "ON CONFLICT (player_name, stage_id, level_number) DO UPDATE SET " +
            "  attempts = attempts + 1," +
            "  is_cleared = MAX(is_cleared, excluded.is_cleared)," +
            "  best_final_temp = CASE WHEN excluded.best_final_temp IS NOT NULL AND " +
            "    (best_final_temp IS NULL OR excluded.best_final_temp < best_final_temp) " +
            "    THEN excluded.best_final_temp ELSE best_final_temp END," +
            "  best_stars = MAX(best_stars, excluded.best_stars)";

        private const string SaveSessionSql =
            "INSERT OR REPLACE INTO saved_session (player_name, stage_id, level_number, wave_number, turn_number, " +
            "oxygen_left, oxygen_allowance, oxygen_penalty, temperature, boss_turn, peak_temperature, allies, enemies, saved_at, app_version) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

        private SQLiteConnection _db;

        public string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private void Awake() => Open();

        private void OnDestroy() => Close();

        private void OnApplicationQuit() => Close();

        /// <summary>Inserts a result. Returns the stored record (with its new id), or null on failure.</summary>
        public GameResult Insert(string playerName, bool won, float finalTemperature, int wavesCleared)
        {
            var record = new GameResult
            {
                player_name = Sanitize(playerName),
                result = won ? "WIN" : "LOSS",
                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                final_temperature = Mathf.Round(finalTemperature * 100f) / 100f,
                waves_cleared = Mathf.Max(0, wavesCleared),
            };
            if (string.IsNullOrEmpty(record.player_name)) record.player_name = "Player";

            if (!EnsureOpen()) return record;
            try
            {
                _db.Execute(InsertSql, record.player_name, record.result, record.created_at,
                            (double)record.final_temperature, record.waves_cleared);
                record.id = (int)_db.ExecuteScalar<long>("SELECT last_insert_rowid()");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResultsDatabase] Insert failed: {e.Message}");
            }
            return record;
        }

        /// <summary>All results, newest first.</summary>
        public List<GameResult> GetAll()
        {
            var list = new List<GameResult>();
            if (!EnsureOpen()) return list;
            try
            {
                foreach (GameResultRow row in _db.Query<GameResultRow>(SelectAllSql))
                    list.Add(row.ToResult());
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResultsDatabase] Query failed: {e.Message}");
            }
            return list;
        }

        /// <summary>Deletes one result by id. Returns true if a row was removed.</summary>
        public bool Delete(int id)
        {
            if (!EnsureOpen()) return false;
            try { return _db.Execute("DELETE FROM game_results WHERE id = ?", id) > 0; }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Delete failed: {e.Message}"); return false; }
        }

        // ---------------------------------------------------------------- level_progress

        /// <summary>Records one finished attempt. A win marks the level cleared and may improve the best temperature.</summary>
        public void RecordAttempt(string playerName, int stageId, int levelNumber, bool won, float finalTemperature, int stars = 0)
        {
            if (!EnsureOpen()) return;
            try
            {
                object best = won ? (object)(double)(Mathf.Round(finalTemperature * 100f) / 100f) : null;
                _db.Execute(RecordAttemptSql, Sanitize(playerName), stageId, levelNumber, won ? 1 : 0, best, won ? Mathf.Clamp(stars, 1, 3) : 0);
            }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Progress update failed: {e.Message}"); }
        }

        /// <summary>Every level row of a stage for one player (levels never played have no row).</summary>
        public List<LevelProgress> GetProgress(string playerName, int stageId)
        {
            var list = new List<LevelProgress>();
            if (!EnsureOpen()) return list;
            try
            {
                list.AddRange(_db.Query<LevelProgress>(
                    "SELECT stage_id, level_number, is_cleared, attempts, best_final_temp, best_stars FROM level_progress " +
                    "WHERE player_name = ? AND stage_id = ? ORDER BY level_number", Sanitize(playerName), stageId));
            }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Progress query failed: {e.Message}"); }
            return list;
        }

        /// <summary>How many levels this player has cleared in a stage (0 = only Level 1 is open).</summary>
        public int CountCleared(string playerName, int stageId)
        {
            if (!EnsureOpen()) return 0;
            try
            {
                return _db.ExecuteScalar<int>("SELECT COUNT(*) FROM level_progress WHERE player_name = ? AND stage_id = ? AND is_cleared = 1",
                    Sanitize(playerName), stageId);
            }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Progress count failed: {e.Message}"); return 0; }
        }

        /// <summary>Levels cleared in all stages.</summary>
        public int CountClearedAll(string playerName)
        {
            if (!EnsureOpen()) return 0;
            try { return _db.ExecuteScalar<int>("SELECT COUNT(*) FROM level_progress WHERE player_name = ? AND is_cleared = 1", Sanitize(playerName)); }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Progress count failed: {e.Message}"); return 0; }
        }

        // ---------------------------------------------------------------- saved_session

        /// <summary>Writes (or replaces) the player's single saved game. Returns false if it could not be stored.</summary>
        public bool SaveSession(SessionSnapshot s)
        {
            if (s == null || !EnsureOpen()) return false;
            try
            {
                s.playerName = Sanitize(s.playerName);
                s.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                s.appVersion = Application.version;
                _db.Execute(SaveSessionSql, s.playerName, s.stageId, s.levelNumber, Mathf.Max(1, s.waveNumber), Mathf.Max(1, s.turnNumber),
                    Mathf.Max(0, s.oxygenLeft), s.oxygenAllowance, s.oxygenPenalty, (double)s.temperature, s.bossTurn, (double)s.peakTemperature,
                    SessionSnapshot.ToJson(s.allies), SessionSnapshot.ToJson(s.enemies), s.savedAt, s.appVersion);
                return true;
            }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Save game failed: {e.Message}"); return false; }
        }

        /// <summary>The player's saved game, or null. A save from another app version is discarded.</summary>
        public SessionSnapshot LoadSession(string playerName)
        {
            string name = Sanitize(playerName);
            if (name.Length == 0 || !EnsureOpen()) return null;
            try
            {
                var rows = _db.Query<SavedSessionRow>("SELECT * FROM saved_session WHERE player_name = ?", name);
                if (rows.Count == 0) return null;
                SavedSessionRow r = rows[0];
                if (r.AppVersion != Application.version)
                {
                    Debug.Log($"[ResultsDatabase] Saved game from version {r.AppVersion} discarded (now {Application.version}).");
                    DeleteSession(name);
                    return null;
                }
                return new SessionSnapshot
                {
                    playerName = r.PlayerName, stageId = r.StageId, levelNumber = r.LevelNumber,
                    waveNumber = r.WaveNumber, turnNumber = r.TurnNumber,
                    oxygenLeft = r.OxygenLeft, oxygenAllowance = r.OxygenAllowance, oxygenPenalty = r.OxygenPenalty,
                    temperature = (float)r.Temperature, bossTurn = r.BossTurn, peakTemperature = (float)r.PeakTemperature,
                    allies = SessionSnapshot.FromJson(r.Allies), enemies = SessionSnapshot.FromJson(r.Enemies),
                    savedAt = r.SavedAt, appVersion = r.AppVersion,
                };
            }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Load game failed: {e.Message}"); return null; }
        }

        public void DeleteSession(string playerName)
        {
            if (!EnsureOpen()) return;
            try { _db.Execute("DELETE FROM saved_session WHERE player_name = ?", Sanitize(playerName)); }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Delete saved game failed: {e.Message}"); }
        }

        /// <summary>Settings ▸ Reset my progress: clears the saved game and unlocked levels. Results stay.</summary>
        public void ResetProgress(string playerName)
        {
            if (!EnsureOpen()) return;
            try
            {
                string name = Sanitize(playerName);
                _db.RunInTransaction(() =>
                {
                    _db.Execute("DELETE FROM level_progress WHERE player_name = ?", name);
                    _db.Execute("DELETE FROM saved_session WHERE player_name = ?", name);
                });
            }
            catch (Exception e) { Debug.LogError($"[ResultsDatabase] Reset progress failed: {e.Message}"); }
        }

        public int Count()
        {
            if (!EnsureOpen()) return 0;
            return _db.ExecuteScalar<int>("SELECT COUNT(*) FROM game_results");
        }

        public static string Sanitize(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
            return name;
        }

        // ---------------------------------------------------------------- internals

        private bool EnsureOpen()
        {
            if (_db == null) Open();
            return _db != null;
        }

        private void Open()
        {
            if (_db != null) return;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                _db = new SQLiteConnection(FilePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                _db.Execute(CreateTableSql);
                _db.Execute(CreateIndexSql);
                Upgrade(_db.ExecuteScalar<int>("PRAGMA user_version"));
                ImportLegacyJson();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResultsDatabase] Could not open {FilePath}: {e.Message}");
                _db = null;
            }
        }

        /// <summary>Adds the tables of newer schema versions without touching existing results.</summary>
        private void Upgrade(int from)
        {
            if (from < 2)
            {
                _db.RunInTransaction(() =>
                {
                    _db.Execute(CreateProgressSql);
                    _db.Execute(CreateSessionSql);
                });
                }
            else if (from < 3)
            {
                // Version 3: stars (rated by the highest temperature reached) and the peak kept in the saved game.
                _db.RunInTransaction(() =>
                {
                    _db.Execute("ALTER TABLE level_progress ADD COLUMN best_stars INTEGER NOT NULL DEFAULT 0 CHECK (best_stars BETWEEN 0 AND 3)");
                    _db.Execute("ALTER TABLE saved_session ADD COLUMN peak_temperature REAL NOT NULL DEFAULT 0");
                });
            }
            if (from > 0 && from < SchemaVersion) Debug.Log($"[ResultsDatabase] Upgraded database from version {from} to {SchemaVersion}.");
            _db.Execute($"PRAGMA user_version = {SchemaVersion}");
        }

        private void Close()
        {
            if (_db == null) return;
            try { _db.Close(); } catch { /* already closed */ }
            _db = null;
        }

        [Serializable]
        private class LegacyTable
        {
            public List<GameResult> records = new List<GameResult>();
        }

        /// <summary>One-time import of results saved by the older JSON database, then renames the file.</summary>
        private void ImportLegacyJson()
        {
            string legacy = Path.Combine(Application.persistentDataPath, LegacyJsonName);
            if (!File.Exists(legacy)) return;
            try
            {
                var table = JsonUtility.FromJson<LegacyTable>(File.ReadAllText(legacy));
                if (table?.records != null && table.records.Count > 0)
                {
                    table.records.Sort((a, b) => a.id.CompareTo(b.id));
                    _db.RunInTransaction(() =>
                    {
                        foreach (GameResult r in table.records)
                        {
                            string name = Sanitize(r.player_name);
                            if (string.IsNullOrEmpty(name)) name = "Player";
                            string res = r.result == "WIN" ? "WIN" : "LOSS";
                            _db.Execute(InsertSql, name, res, r.created_at ?? "", (double)r.final_temperature, Mathf.Max(0, r.waves_cleared));
                        }
                    });
                    Debug.Log($"[ResultsDatabase] Imported {table.records.Count} result(s) from {LegacyJsonName}.");
                }
                File.Move(legacy, legacy + ".migrated");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResultsDatabase] Legacy JSON import skipped: {e.Message}");
            }
        }
    }
}
