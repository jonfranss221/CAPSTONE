using System.Collections;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Moves the battle camera during End Turn: it zooms in on each lane while that lane resolves,
    /// widens for the boss's cleave, and returns to the full board for the player's turn.
    /// Uses scaled time, so the Battle speed setting also speeds up the camera.
    /// Settings ▸ "Zoom in on each lane" turns the movement off (lanes still resolve one by one).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BattleCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("Orthographic size when looking at one lane.")]
        private float _laneSize = 3.0f;
        [SerializeField, Tooltip("Orthographic size for a lane while the boss fills the board.")]
        private float _bossLaneSize = 3.6f;
        [SerializeField, Tooltip("Orthographic size for the boss's 3-lane cleave.")]
        private float _cleaveSize = 4.1f;
        [SerializeField, Tooltip("Seconds to move between two lanes at 1x speed.")]
        private float _moveTime = 0.35f;
        [SerializeField, Tooltip("How far above the middle of the lane to aim, so both units' heads and HP bars fit.")]
        private float _aimHeight = 1.15f;

        private Camera _cam;
        private Vector3 _homePos;
        private float _homeSize;

        public bool IsHome { get; private set; } = true;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _homePos = transform.position;
            _homeSize = _cam.orthographicSize;
        }

        /// <summary>True when the player wants the lane zoom (Settings).</summary>
        public static bool Enabled => SettingsStore.LaneCamera;

        /// <summary>Centre of a lane between its ally and enemy slot, raised so both units fit.</summary>
        public Vector3 LaneAim(int lane)
        {
            Vector3 a = IsoBoardLayout.SlotPosition(lane, false);
            Vector3 e = IsoBoardLayout.SlotPosition(lane, true);
            Vector3 mid = (a + e) * 0.5f + Vector3.up * _aimHeight;
            return new Vector3(mid.x, mid.y, _homePos.z);
        }

        public IEnumerator FocusLane(int lane, bool boss)
        {
            if (!Enabled) yield break;
            Vector3 aim = LaneAim(lane);
            if (boss) aim += Vector3.up * 0.8f;
            yield return MoveTo(aim, boss ? _bossLaneSize : _laneSize, _moveTime);
        }

        /// <summary>Frames several lanes (the boss's cleave): aims at the middle one, wider.</summary>
        public IEnumerator FocusLanes(int firstLane, int lastLane)
        {
            if (!Enabled) yield break;
            Vector3 aim = (LaneAim(firstLane) + LaneAim(lastLane)) * 0.5f + Vector3.up * 0.6f;
            yield return MoveTo(aim, _cleaveSize, _moveTime);
        }

        public IEnumerator Home()
        {
            if (IsHome) yield break;
            yield return MoveTo(_homePos, _homeSize, _moveTime);
            IsHome = true;
        }

        /// <summary>
        /// Level start: begins slightly zoomed in and up-left of the board, then eases out to the full board.
        /// Unscaled time, so it does not depend on the Battle speed. With the lane camera turned off it does nothing.
        /// </summary>
        public IEnumerator IntroGlide(float seconds)
        {
            if (!Enabled || seconds <= 0f) { SnapHome(); yield break; }
            IsHome = false;
            Vector3 fromPos = _homePos + new Vector3(-1.2f, 0.6f, 0f);
            float fromSize = _homeSize * 0.82f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                transform.position = Vector3.Lerp(fromPos, _homePos, k);
                _cam.orthographicSize = Mathf.Lerp(fromSize, _homeSize, k);
                yield return null;
            }
            SnapHome();
        }

        /// <summary>Jumps straight back to the full board (level end, pause menu, scene changes).</summary>
        public void SnapHome()
        {
            transform.position = _homePos;
            _cam.orthographicSize = _homeSize;
            IsHome = true;
        }

        private IEnumerator MoveTo(Vector3 pos, float size, float time)
        {
            IsHome = false;
            Vector3 fromPos = transform.position;
            float fromSize = _cam.orthographicSize;
            pos.z = _homePos.z;
            // Short hops between neighbouring lanes take less time than a long sweep.
            float dist = Vector2.Distance(fromPos, pos) + Mathf.Abs(fromSize - size);
            float duration = Mathf.Clamp(time * dist / 2.5f, time * 0.5f, time * 1.4f);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                transform.position = Vector3.Lerp(fromPos, pos, k);
                _cam.orthographicSize = Mathf.Lerp(fromSize, size, k);
                yield return null;
            }
            transform.position = pos;
            _cam.orthographicSize = size;
        }
    }
}
