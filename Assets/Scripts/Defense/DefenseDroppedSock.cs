using UnityEngine;
using BulkoGame.GameFlow;
using BulkoGame.Player;

namespace BulkoGame.Defense
{
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class DefenseDroppedSock : MonoBehaviour
    {
        private CatController owner;
        private BulkoGame.GameFlow.GameFlow flow;
        private bool resolved;
        public CatController Owner => owner;
        public bool IsResolved => resolved;

        public void Initialize(CatController cat, BulkoGame.GameFlow.GameFlow gameFlow, Sprite sprite)
        {
            owner = cat; flow = gameFlow;
            GetComponent<SpriteRenderer>().enabled = false;
            var visual = new GameObject("Dropped sock visual");
            visual.transform.SetParent(transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 20f) + 1;
            if (sprite != null && sprite.bounds.size.x > 0 && sprite.bounds.size.y > 0)
                visual.transform.localScale = Vector3.one * Mathf.Min(.42f / sprite.bounds.size.x, .56f / sprite.bounds.size.y);
            var trigger = GetComponent<CircleCollider2D>(); trigger.isTrigger = true; trigger.radius = .7f;
        }

        public bool TryReclaim(CatController cat)
        {
            if (resolved || cat != owner || !cat.AcceptDroppedSock(this)) return false;
            resolved = true; flow.Session.ResolveDroppedDefenseSock(); Destroy(gameObject); return true;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryPickUp(other);
        private void OnTriggerStay2D(Collider2D other) => TryPickUp(other);
        private void TryPickUp(Collider2D other)
        {
            if (resolved || flow == null || flow.ControlsLocked || other.GetComponent<PlayerController>() == null || !flow.Session.TryPickUpDefenseSock()) return;
            resolved = true; flow.NotifyDefenseSockPickedUp();
            owner.OnDroppedSockRecoveredByBulka(); Destroy(gameObject);
        }
    }
}
