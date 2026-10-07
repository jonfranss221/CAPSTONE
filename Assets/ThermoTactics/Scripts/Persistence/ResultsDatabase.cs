using System;
using System.Collections.Generic;
using System.IO;
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

    [Serializable]
    internal class ResultsTable
    {
        public int schema_version = 1;
        public int next_id = 1;
        public List<GameResult> records = new List<GameResult>();
    }

    /// <summary>
    /// Local results database stored as JSON in Application.persistentDataPath/results_db.json.
    /// Works offline on Android with no plugins. Writes are atomic (temp file + backup).
    /// </summary>
    public class ResultsDatabase : MonoBehaviour
    {
        public const int MaxNameLength = 20;
        private const string FileName = "results_db.json";

        private ResultsTable _table = new ResultsTable();

        public string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private void Awake() => Load();

        /// <summary>Inserts a result and saves the file. Returns the stored record.</summary>
        public GameResult Insert(string playerName, bool won, float finalTemperature, int wavesCleared)
        {
            var record = new GameResult
            {
                id = _table.next_id++,
                player_name = Sanitize(playerName),
                result = won ? "WIN" : "LOSS",
                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                final_temperature = Mathf.Round(finalTemperature * 100f) / 100f,
                waves_cleared = Mathf.Max(0, wavesCleared),
            };
            _table.records.Add(record);
            Save();
            return record;
        }

        /// <summary>All results, newest first.</summary>
        public List<GameResult> GetAll()
        {
            var list = new List<GameResult>(_table.records);
            list.Sort((a, b) => b.id.CompareTo(a.id));
            return list;
        }

        public static string Sanitize(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
            return name;
        }

        private void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    _table = JsonUtility.FromJson<ResultsTable>(File.ReadAllText(FilePath)) ?? new ResultsTable();
                    if (_table.records == null) _table.records = new List<GameResult>();
                    foreach (var r in _table.records) _table.next_id = Mathf.Max(_table.next_id, r.id + 1);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResultsDatabase] Could not read {FilePath}: {e.Message}. Trying backup.");
                TryLoadBackup();
            }
        }

        private void TryLoadBackup()
        {
            string bak = FilePath + ".bak";
            try
            {
                _table = File.Exists(bak) ? JsonUtility.FromJson<ResultsTable>(File.ReadAllText(bak)) : new ResultsTable();
                if (_table == null) _table = new ResultsTable();
            }
            catch { _table = new ResultsTable(); }
        }

        private void Save()
        {
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(_table, true));
                if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".bak", true);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResultsDatabase] Save failed: {e.Message}");
            }
        }
    }
}
