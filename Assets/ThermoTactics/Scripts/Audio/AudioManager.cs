using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThermoTactics
{
    public enum MusicTrack { None, Menu, Battle, Boss }
    public enum Sfx { Click, Confirm, Win, Lose, Deploy, Pickup, AttackAlly, AttackEnemy, UnitDown, Purchase }

    /// <summary>
    /// Plays the game's music and sound effects. Created once at start-up and kept across scenes, so the menu
    /// music carries on between the Title, Stage Select and Level Select screens. Music volume follows
    /// Settings ▸ Music and effects follow Settings ▸ Sound. Every call is safe when no library is present.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const float FadeTime = 0.8f;
        private const int SfxVoices = 8;
        private const float RepeatGap = 0.05f;   // the same effect at most every 50 ms (a lane of hits stays clean)

        private static AudioManager _instance;
        private AudioLibrary _lib;
        private AudioSource _musicA, _musicB;     // two players for a crossfade
        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private int _nextVoice;
        private MusicTrack _current = MusicTrack.None;
        private Coroutine _fade;
        private readonly Dictionary<Sfx, float> _lastPlayed = new Dictionary<Sfx, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var lib = Resources.Load<AudioLibrary>("AudioLibrary");
            if (lib == null) { Debug.LogWarning("[Audio] Resources/AudioLibrary not found: the game runs silent. Run Build Everything."); return; }
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AudioManager>();
            _instance._lib = lib;
            _instance.Init();
        }

        private void Init()
        {
            // The game's cameras are built in code without a listener, so the AudioManager carries the one and only
            // listener for the whole app (it lives across scenes, like the music). Sound is 2D, so its position does not matter.
            if (GetComponent<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            AudioListener.volume = 1f;
            _musicA = NewSource(true);
            _musicB = NewSource(true);
            for (int i = 0; i < SfxVoices; i++) _voices.Add(NewSource(false));
            SettingsStore.Changed += ApplyVolumes;
        }

        private void OnDestroy() { if (_instance == this) SettingsStore.Changed -= ApplyVolumes; }

        private AudioSource NewSource(bool music)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = music;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = music;   // the menu music keeps going under pause screens
            return s;
        }

        private float MusicLevel => SettingsStore.MusicVolume * (_lib != null ? _lib.musicGain : 1f);
        private float SfxLevel => SettingsStore.SfxVolume * (_lib != null ? _lib.sfxGain : 1f);

        private void ApplyVolumes()
        {
            if (_fade != null) return;   // a running fade picks the new level up itself
            AudioSource live = _musicA.isPlaying ? _musicA : _musicB;
            live.volume = MusicLevel;
        }

        // ------------------------------------------------------------------ music

        /// <summary>Crossfades to a track. Asking for the track that is already playing does nothing.</summary>
        public static void PlayMusic(MusicTrack track)
        {
            if (_instance == null) return;
            _instance.SwitchTo(track);
        }

        /// <summary>Fades the music out (before a win or lose sound, or when leaving a screen).</summary>
        public static void StopMusic() => PlayMusic(MusicTrack.None);

        private AudioClip ClipFor(MusicTrack t) =>
            t == MusicTrack.Menu ? _lib.menuMusic : t == MusicTrack.Battle ? _lib.battleMusic : t == MusicTrack.Boss ? _lib.bossMusic : null;

        private void SwitchTo(MusicTrack track)
        {
            if (track == _current) return;
            _current = track;
            AudioSource from = _musicA.isPlaying ? _musicA : _musicB;
            AudioSource to = from == _musicA ? _musicB : _musicA;
            AudioClip clip = ClipFor(track);
            if (clip != null)
            {
                to.clip = clip;
                to.volume = 0f;
                to.time = 0f;
                to.Play();
            }
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(from, clip != null ? to : null));
        }

        private IEnumerator Crossfade(AudioSource from, AudioSource to)
        {
            float start = from.isPlaying ? from.volume : 0f;
            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                float k = t / FadeTime;
                if (from.isPlaying) from.volume = start * (1f - k);
                if (to != null) to.volume = MusicLevel * k;
                yield return null;
            }
            from.Stop();
            if (to != null) to.volume = MusicLevel;
            _fade = null;
        }

        // ------------------------------------------------------------------ effects

        public static void Play(Sfx sfx, float volume = 1f)
        {
            if (_instance == null) return;
            _instance.PlayEffect(sfx, volume);
        }

        private AudioClip ClipFor(Sfx s)
        {
            switch (s)
            {
                case Sfx.Click: return _lib.click;
                case Sfx.Confirm: return _lib.confirm;
                case Sfx.Win: return _lib.win;
                case Sfx.Lose: return _lib.lose;
                case Sfx.Deploy: return _lib.deploy;
                case Sfx.Pickup: return _lib.pickup;
                case Sfx.AttackAlly: return _lib.attackAlly;
                case Sfx.AttackEnemy: return _lib.attackEnemy;
                case Sfx.UnitDown: return _lib.unitDown;
                case Sfx.Purchase: return _lib.purchase;
            }
            return null;
        }

        private void PlayEffect(Sfx sfx, float volume)
        {
            AudioClip clip = ClipFor(sfx);
            if (clip == null || SfxLevel <= 0f) return;
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(sfx, out float last) && now - last < RepeatGap) return;
            _lastPlayed[sfx] = now;
            AudioSource v = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Count;
            v.pitch = sfx == Sfx.AttackAlly || sfx == Sfx.AttackEnemy ? Random.Range(0.94f, 1.06f) : 1f;   // repeated hits sound less mechanical
            v.PlayOneShot(clip, Mathf.Clamp01(volume) * SfxLevel);
        }
    }
}
