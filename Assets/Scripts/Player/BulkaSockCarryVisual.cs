using UnityEngine;

namespace BulkoGame.Player
{
    /// <summary>Visual-only display of the sock represented by GameSessionState.</summary>
    public sealed class BulkaSockCarryVisual : MonoBehaviour
    {
        private BulkoGame.GameFlow.GameFlow flow;
        private SpriteRenderer sockRenderer;

        public bool IsVisible => sockRenderer != null && sockRenderer.enabled;

        public void Initialize(BulkoGame.GameFlow.GameFlow gameFlow, Sprite sockSprite)
        {
            flow = gameFlow;
            var visual = new GameObject("Carried Sock Visual");
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(.52f, 1.38f, -.03f);
            sockRenderer = visual.AddComponent<SpriteRenderer>();
            sockRenderer.sprite = sockSprite;
            sockRenderer.enabled = false;
            if (sockSprite != null)
            {
                const float targetWidth = .42f;
                const float targetHeight = .56f;
                float fit = Mathf.Min(targetWidth / sockSprite.bounds.size.x, targetHeight / sockSprite.bounds.size.y);
                visual.transform.localScale = Vector3.one * fit;
            }
            flow.CarryingSockChanged += SetVisible;
            SetVisible(flow.Session != null && flow.Session.IsCarryingSock);
        }

        private void LateUpdate()
        {
            if (sockRenderer != null)
                sockRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 20f) + 2;
        }

        private void SetVisible(bool visible)
        {
            if (sockRenderer != null) sockRenderer.enabled = visible;
        }

        private void OnDestroy()
        {
            if (flow != null) flow.CarryingSockChanged -= SetVisible;
        }
    }
}
