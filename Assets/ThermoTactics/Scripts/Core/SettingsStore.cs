using System;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Player settings kept in PlayerPrefs (Unity's key-value store on the phone).
    /// Keys start with "tt.". Only this class reads or writes them.
    /// </summary>
    public static class SettingsStore
    {
        public static event Action Changed;

        public static readonly float[] BattleSpeeds = { 1f, 1.5f, 2f };

        public static float MusicVolume { get => PlayerPrefs.GetFloat("tt.music_volume", 0.7f); set => SetF("tt.music_volume", Mathf.Clamp01(value)); }
        public static float SfxVolume { get => PlayerPrefs.GetFloat("tt.sfx_volume", 0.85f); set => SetF("tt.sfx_volume", Mathf.Clamp01(value)); }
        public static bool LargeText { get => GetB("tt.large_text", false); set => SetB("tt.large_text", value); }
        public static bool HighContrast { get => GetB("tt.high_contrast", false); set => SetB("tt.high_contrast", value); }
        public static bool PopupsEnabled { get => GetB("tt.popups_enabled", true); set => SetB("tt.popups_enabled", value); }
        public static bool ShowNumbers { get => GetB("tt.show_numbers", true); set => SetB("tt.show_numbers", value); }
        public static bool BossWarning { get => GetB("tt.boss_warning", true); set => SetB("tt.boss_warning", value); }
        /// <summary>Zoom the camera in on each lane while End Turn resolves.</summary>
        public static bool LaneCamera { get => GetB("tt.lane_camera", true); set => SetB("tt.lane_camera", value); }

        /// <summary>Index into BattleSpeeds (0 = 1x, 1 = 1.5x, 2 = 2x).</summary>
        public static int BattleSpeedIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("tt.battle_speed", 0), 0, BattleSpeeds.Length - 1);
            set { PlayerPrefs.SetInt("tt.battle_speed", Mathf.Clamp(value, 0, BattleSpeeds.Length - 1)); Save(); }
        }
        public static float BattleSpeed => BattleSpeeds[BattleSpeedIndex];

        /// <summary>The name typed last time, so the profile form is filled in on the next launch.</summary>
        public static string LastPlayer { get => PlayerPrefs.GetString("tt.last_player", ""); set { PlayerPrefs.SetString("tt.last_player", value ?? ""); PlayerPrefs.Save(); } }
        public static string TesterCode { get => PlayerPrefs.GetString("tt.tester_code", ""); set { PlayerPrefs.SetString("tt.tester_code", value ?? ""); PlayerPrefs.Save(); } }

        /// <summary>True once this player finished or skipped the Level 1 tutorial (names match without case, like the database).</summary>
        public static bool TutorialDone(string player) => PlayerPrefs.GetInt(TutorialKey(player), 0) == 1;

        public static void SetTutorialDone(string player, bool done)
        {
            if (done) PlayerPrefs.SetInt(TutorialKey(player), 1); else PlayerPrefs.DeleteKey(TutorialKey(player));
            PlayerPrefs.Save();
        }

        private static string TutorialKey(string player) => "tt.tutorial_done." + (player ?? "").Trim().ToLowerInvariant();

        /// <summary>Applies settings that act on the whole app (volume until the AudioManager exists).</summary>
        public static void Apply()
        {
            AudioListener.volume = SfxVolume;
        }

        private static bool GetB(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
        private static void SetB(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); Save(); }
        private static void SetF(string key, float value) { PlayerPrefs.SetFloat(key, value); Save(); }

        private static void Save()
        {
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }
    }
}
