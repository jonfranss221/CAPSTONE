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

    /// <summary>
    /// Local results database: SQLite file at Application.persistentDataPath/thermo_tactics.db
    /// (package com.gilzoide.sqlite-net, native SQLite for Android and Windows).
    ///
    /// Table: game_results(id, player_name, result, created_at, final_temperature, waves_cleared)
    /// Results recorded earlier by the JSON version (results_db.json) are imported once.
    /// </summary>
    public class ResultsDatabase : MonoBehaviour
    {
        public const int MaxNameLength = 20;
        public const int SchemaVersion = 1;
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
                _db.Execute($"PRAGMA user_version = {SchemaVersion}");
                ImportLegacyJson();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResultsDatabase] Could not open {FilePath}: {e.Message}");
                _db = null;
            }
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
