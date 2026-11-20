using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Every music track and sound effect in the game, filled in by Thermo-Tactics ▸ Build Everything
    /// (Editor/AudioBuilder.cs) from Assets/ThermoTactics/Audio. Loaded at start-up from Resources/AudioLibrary.
    /// </summary>
    public class AudioLibrary : ScriptableObject
    {
        [Header("Music (looped)")]
        public AudioClip menuMusic;     // Title, Stage Select, Level Select, Store, Settings
        public AudioClip battleMusic;   // battle waves of every stage
        public AudioClip bossMusic;     // the boss wave of every stage

        [Header("Interface")]
        public AudioClip click;         // ordinary buttons
        public AudioClip confirm;       // Play, Start, End Turn, Next Level, Buy ...

        [Header("Results")]
        public AudioClip win;
        public AudioClip lose;

        [Header("Battle")]
        public AudioClip deploy;
        public AudioClip pickup;
        public AudioClip attackAlly;
        public AudioClip attackEnemy;
        public AudioClip unitDown;

        [Header("Store")]
        public AudioClip purchase;

        [Header("Mix")]
        [Range(0f, 1f)] public float musicGain = 0.45f;   // music sits under the effects (buttons and hits must cut through)
        [Range(0f, 1f)] public float sfxGain = 1f;
    }
}
