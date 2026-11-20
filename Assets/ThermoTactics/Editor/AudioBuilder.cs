using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Part of Build Everything: sets the import settings of the game's audio and fills Resources/AudioLibrary.
    /// Music streams from storage (Vorbis); short effects are decompressed on load so they play instantly.
    /// Sources and licences are listed in Assets/ThermoTactics/Audio/CREDITS.txt.
    /// </summary>
    public static class AudioBuilder
    {
        public const string AudioDir = "Assets/ThermoTactics/Audio";
        public const string LibraryPath = "Assets/ThermoTactics/Resources/AudioLibrary.asset";

        public static void Build()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                bool music = path.Contains("/Music/");
                var s = importer.defaultSampleSettings;
                var load = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                float quality = music ? 0.55f : 0.7f;
                bool same = s.loadType == load && s.compressionFormat == AudioCompressionFormat.Vorbis &&
                            Mathf.Approximately(s.quality, quality) && s.preloadAudioData == !music &&
                            importer.forceToMono == !music && importer.loadInBackground == music;
                if (same) continue;   // already set: no re-import (keeps Build Everything quick and quiet)
                s.loadType = load;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = quality;
                s.preloadAudioData = !music;
                importer.defaultSampleSettings = s;
                importer.forceToMono = !music;
                importer.loadInBackground = music;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<AudioLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
            lib.menuMusic = Clip("Music/menu_superepic");
            lib.battleMusic = Clip("Music/battle_warfare");
            lib.bossMusic = Clip("Music/boss_thunder_unison");
            lib.click = Clip("Sfx/ui_click");
            lib.confirm = Clip("Sfx/ui_confirm");
            lib.win = Clip("Sfx/win");
            lib.lose = Clip("Sfx/lose");
            lib.deploy = Clip("Sfx/deploy");
            lib.pickup = Clip("Sfx/pickup");
            lib.attackAlly = Clip("Sfx/attack_ally");
            lib.attackEnemy = Clip("Sfx/attack_enemy");
            lib.unitDown = Clip("Sfx/unit_down");
            lib.purchase = Clip("Sfx/purchase");
            lib.musicGain = 0.45f;   // existing libraries keep their saved value otherwise
            lib.sfxGain = 1f;
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log("[Thermo-Tactics] Audio library built: 3 music tracks, 11 sound effects.");
        }

        private static AudioClip Clip(string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.ogg");
            if (c == null) Debug.LogError($"[Thermo-Tactics] Missing audio file {AudioDir}/{name}.ogg");
            return c;
        }
    }
}
