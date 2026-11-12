using UnityEngine;

namespace BulkoGame.Defense
{
    /// <summary>A non-blocking, scene-authored placement point shown only during Defense Phase.</summary>
    public sealed class TrapPlacementZone : MonoBehaviour
    {
        private SpriteRenderer marker;
        private bool defenseVisible;
        private bool occupied;
        private bool nearby;

        public bool IsOccupied => occupied;

        public void Initialize(Sprite markerSprite)
        {
            marker = GetComponent<SpriteRenderer>();
            if (marker == null) marker = gameObject.AddComponent<SpriteRenderer>();
            marker.sprite = markerSprite;
            transform.localScale = new Vector3(.42f, .42f, 1f);
            marker.sortingOrder = -Mathf.RoundToInt(transform.position.y * 20f) - 2;
            marker.enabled = false;
            UpdateTint();
        }

        public void SetDefenseVisible(bool visible)
        {
            defenseVisible = visible;
            if (marker != null) marker.enabled = defenseVisible;
        }

        public void SetNearby(bool value)
        {
            nearby = value;
            UpdateTint();
        }

        public void MarkOccupied()
        {
            occupied = true;
            UpdateTint();
        }

        private void UpdateTint()
        {
            if (marker == null) return;
            marker.color = occupied
                ? new Color32(145, 128, 106, 150)
                : nearby ? new Color32(255, 239, 174, 255) : Color.white;
        }
    }
}
