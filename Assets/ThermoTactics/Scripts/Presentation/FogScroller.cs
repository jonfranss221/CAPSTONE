using UnityEngine;

/// <summary>
/// Drifts a horizontally-tileable fog sprite sideways forever.
/// Put this on a parent object that holds TWO copies of the same fog sprite
/// placed side by side (the second at x = +21 world units).
/// </summary>
namespace ThermoTactics
{
public class FogScroller : MonoBehaviour
{
    [Tooltip("World units per second. Negative = drift left.")]
    [SerializeField] private float _speed = -0.15f;

    [Tooltip("Width of ONE fog sprite in world units (672 px / 32 PPU = 21).")]
    [SerializeField] private float _tileWidth = 21f;

    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.position;
    }

    private void Update()
    {
        // Mathf.Repeat wraps the offset back to 0 after one full sprite width,
        // so the two copies loop seamlessly.
        float offset = Mathf.Repeat(Time.time * _speed, _tileWidth);
        transform.position = _startPosition + Vector3.right * (offset - _tileWidth);
    }

    /// <summary>
    /// Optional hook for TemperatureSystem: thicker fog when cold, thinner when hot.
    /// Pass 1.0 at 1.0°C and lower values as the temperature climbs.
    /// </summary>
    public void SetDensity(float density01)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(density01);
            sr.color = c;
        }
    }
}
}
