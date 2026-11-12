using UnityEngine;

namespace BulkoGame.Player
{
    /// <summary>Fades an explicitly marked foreground sprite while it covers Bulka.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ForegroundOcclusion : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float fadedAlpha = .35f;
        [SerializeField] private float fadeSpeed = 5f;
        private SpriteRenderer foreground;
        private SpriteRenderer player;
        private float originalAlpha;

        private void Awake()
        {
            foreground = GetComponent<SpriteRenderer>();
            originalAlpha = foreground.color.a;
        }

        public void Initialize(SpriteRenderer playerRenderer) => player = playerRenderer;

        private void LateUpdate()
        {
            if (foreground == null) return;
            bool coversPlayer = player != null && foreground.enabled && player.enabled &&
                foreground.sortingLayerID == player.sortingLayerID &&
                foreground.sortingOrder > player.sortingOrder &&
                foreground.bounds.min.x < player.bounds.max.x && foreground.bounds.max.x > player.bounds.min.x &&
                foreground.bounds.min.y < player.bounds.max.y && foreground.bounds.max.y > player.bounds.min.y;
            Color color = foreground.color;
            color.a = Mathf.MoveTowards(color.a, coversPlayer ? originalAlpha * fadedAlpha : originalAlpha,
                Mathf.Max(0f, fadeSpeed) * Time.deltaTime);
            foreground.color = color;
        }

        private void OnDisable()
        {
            if (foreground == null) return;
            Color color = foreground.color;
            color.a = originalAlpha;
            foreground.color = color;
        }
    }
}
