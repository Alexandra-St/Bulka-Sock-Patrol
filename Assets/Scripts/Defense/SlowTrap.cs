using System.Collections;
using UnityEngine;
using BulkoGame.Audio;

namespace BulkoGame.Defense
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class SlowTrap : MonoBehaviour
    {
        private bool triggered;
        private CircleCollider2D trigger;
        private SpriteRenderer trapRenderer;
        private Sprite triggeredSprite;
        private Sprite usedSprite;
        private float slowMultiplier;
        private float slowDuration;
        private GameAudio audio;

        public void Initialize(Sprite trapSprite, Sprite triggeredTrapSprite, Sprite usedTrapSprite,
            float triggerRadius, BulkoGame.GameFlow.GameBalanceConfig balance, GameAudio gameAudio)
        {
            slowMultiplier = balance.slowTrapMultiplier;
            slowDuration = balance.slowTrapDuration;
            audio = gameAudio;
            triggeredSprite = triggeredTrapSprite;
            usedSprite = usedTrapSprite;
            GetComponent<SpriteRenderer>().enabled = false;
            var visual = transform.Find("Production trap visual");
            if (visual == null)
            {
                var visualObject = new GameObject("Production trap visual");
                visualObject.transform.SetParent(transform, false);
                visual = visualObject.transform;
            }
            trapRenderer = visual.GetComponent<SpriteRenderer>();
            if (trapRenderer == null) trapRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            SetVisual(trapSprite);
            trapRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 20f);

            trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = triggerRadius;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            var cat = other.GetComponent<CatController>();
            if (cat == null) return;

            triggered = true;
            trigger.enabled = false;
            if (audio != null) audio.Play(GameSound.TrapTriggered);
            SetVisual(triggeredSprite);
            gameObject.name = "Spent Slow Trap";
            cat.ApplySlow(slowMultiplier, slowDuration);
            cat.RegisterTrapTrigger();
            StartCoroutine(RemoveAfterUse());
        }

        private IEnumerator RemoveAfterUse()
        {
            yield return new WaitForSeconds(Mathf.Min(.35f, slowDuration));
            SetVisual(usedSprite);
            yield return new WaitForSeconds(Mathf.Max(0f, slowDuration - .35f));
            Destroy(gameObject);
        }

        private void SetVisual(Sprite sprite)
        {
            if (sprite == null) return;
            trapRenderer.sprite = sprite;
            const float targetWidth = 1.35f;
            const float targetHeight = 1.05f;
            float fit = Mathf.Min(targetWidth / sprite.bounds.size.x, targetHeight / sprite.bounds.size.y);
            Vector3 rootScale = transform.lossyScale;
            trapRenderer.transform.localScale = new Vector3(fit / rootScale.x, fit / rootScale.y, 1f);
        }
    }
}
