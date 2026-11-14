using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// The battle scene is shared by every stage. At the start of a battle this swaps in the stage's look:
    /// the background and its blurred side fills, the drifting overlay (Baguio fog, coastal sea spray),
    /// the slot markers with their deploy and boss-warning rings, and the camera's clear colour. Anything the stage leaves empty keeps the scene's default.
    /// </summary>
    public class StageEnvironment : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer[] _sideFills = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] _overlayTiles = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] _slotMarkers = new SpriteRenderer[0];
        [SerializeField] private Camera _camera;

        /// <summary>Overlay strength of the current stage (1 = Baguio fog); the fog density is scaled by it.</summary>
        public float OverlayStrength { get; private set; } = 1f;

        private static void SetChildSprite(Transform parent, string child, Sprite sprite)
        {
            if (sprite == null) return;
            Transform t = parent.Find(child);
            if (t != null && t.TryGetComponent(out SpriteRenderer r)) r.sprite = sprite;
        }

        public void Apply(StageData stage)
        {
            if (stage == null) return;
            if (stage.battleBackground != null && _background != null)
            {
                _background.sprite = stage.battleBackground;
                _background.gameObject.name = $"Background_{stage.ShortName.Replace(" ", "")}";
            }
            if (stage.sideFill != null)
                foreach (SpriteRenderer sr in _sideFills) if (sr != null) sr.sprite = stage.sideFill;
            if (stage.overlayTile != null)
                foreach (SpriteRenderer sr in _overlayTiles) if (sr != null) sr.sprite = stage.overlayTile;
            foreach (SpriteRenderer sr in _slotMarkers)
            {
                if (sr == null) continue;
                if (stage.slotMarker != null) sr.sprite = stage.slotMarker;
                // Children of each marker: the deploy ring and the boss warning ring.
                SetChildSprite(sr.transform, "DeployHighlight", stage.deployHighlight);
                SetChildSprite(sr.transform, "BossWarning", stage.dangerHighlight);
            }
            BoardSlot.NormalTint = stage.deployTint;
            BoardSlot.NormalAlpha = stage.deployAlpha;
            if (_camera != null) _camera.backgroundColor = stage.cameraBackground;
            OverlayStrength = stage.overlayStrength;
        }
    }
}
