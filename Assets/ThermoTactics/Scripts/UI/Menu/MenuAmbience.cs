using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Makes the title screen feel alive: the Baguio background drifts and breathes (slow pan and zoom),
    /// two fog layers roll across at different speeds, pine-pollen motes float upward, and the title bobs.
    /// Purely visual; uses unscaled time so it also runs while the game is paused.
    /// </summary>
    public class MenuAmbience : MonoBehaviour
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RawImage[] _fog = new RawImage[0];
        [SerializeField] private float[] _fogSpeed = { 0.012f, -0.02f };
        [SerializeField] private RectTransform _title;
        [SerializeField] private RectTransform _motesRoot;
        [SerializeField] private Image _moteTemplate;
        [SerializeField, Range(0, 40)] private int _moteCount = 22;

        private Vector2 _titleHome;
        private Mote[] _motes = new Mote[0];
        private float _time;

        private struct Mote
        {
            public RectTransform Rt;
            public Image Img;
            public float Speed, Sway, Phase, Life, Age;
        }

        private void Awake()
        {
            if (_title != null) _titleHome = _title.anchoredPosition;
            if (_moteTemplate == null || _motesRoot == null) return;
            _moteTemplate.gameObject.SetActive(false);
            _motes = new Mote[_moteCount];
            for (int i = 0; i < _moteCount; i++)
            {
                var img = Instantiate(_moteTemplate, _motesRoot);
                img.gameObject.SetActive(true);
                img.raycastTarget = false;
                _motes[i] = Spawn(new Mote { Rt = img.rectTransform, Img = img }, Random.value);
            }
        }

        private Mote Spawn(Mote m, float startAge)
        {
            Rect r = _motesRoot.rect;
            m.Speed = Random.Range(10f, 28f);
            m.Sway = Random.Range(6f, 22f);
            m.Phase = Random.Range(0f, 6.28f);
            m.Life = Random.Range(7f, 14f);
            m.Age = startAge * m.Life;
            float size = Random.Range(3f, 7f);
            m.Rt.sizeDelta = new Vector2(size, size);
            m.Rt.anchorMin = m.Rt.anchorMax = new Vector2(0f, 0f);
            m.Rt.anchoredPosition = new Vector2(Random.Range(0f, Mathf.Max(1f, r.width)), Random.Range(-20f, r.height * 0.6f));
            return m;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            _time += dt;

            // Slow "breathing" pan and zoom over the Baguio road.
            if (_background != null)
            {
                float zoom = 1.06f + 0.035f * Mathf.Sin(_time * 0.11f);
                _background.localScale = new Vector3(zoom, zoom, 1f);
                _background.anchoredPosition = new Vector2(Mathf.Sin(_time * 0.05f) * 28f, Mathf.Sin(_time * 0.08f + 1f) * 10f);
            }

            // Rolling fog: scroll the texture; the layers slowly fade in and out.
            for (int i = 0; i < _fog.Length; i++)
            {
                if (_fog[i] == null) continue;
                Rect uv = _fog[i].uvRect;
                uv.x = Mathf.Repeat(uv.x + (i < _fogSpeed.Length ? _fogSpeed[i] : 0.01f) * dt, 1f);
                _fog[i].uvRect = uv;
                Color c = _fog[i].color;
                c.a = 0.22f + 0.1f * Mathf.Sin(_time * 0.3f + i * 2.1f);
                _fog[i].color = c;
            }

            // The title bobs gently.
            if (_title != null) _title.anchoredPosition = _titleHome + new Vector2(0f, Mathf.Sin(_time * 1.3f) * 3f);

            // Pollen motes drift up and sway, fading in and out.
            for (int i = 0; i < _motes.Length; i++)
            {
                Mote m = _motes[i];
                m.Age += dt;
                if (m.Age >= m.Life) { _motes[i] = Spawn(m, 0f); continue; }
                Vector2 p = m.Rt.anchoredPosition;
                p.y += m.Speed * dt;
                p.x += Mathf.Cos(_time * 0.9f + m.Phase) * m.Sway * dt;
                m.Rt.anchoredPosition = p;
                float k = m.Age / m.Life;
                Color c = m.Img.color;
                c.a = Mathf.Sin(k * Mathf.PI) * 0.55f;
                m.Img.color = c;
                _motes[i] = m;
            }
        }
    }
}
