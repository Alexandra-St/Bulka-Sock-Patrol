using UnityEngine;

namespace BulkoGame.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        private float moveSpeed = 4.4f;
        private GameFlow.GameFlow flow;
        public void Initialize(GameFlow.GameFlow gameFlow, GameFlow.GameBalanceConfig balance)
        { flow = gameFlow; moveSpeed = balance.bulkaSpeed; }
        private Rigidbody2D body;
        private Vector2 input;
        private SpriteRenderer sprite;
        private BulkaVisualState visualState;
        private int moveDirection;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            visualState = GetComponent<BulkaVisualState>();
            moveDirection = 0;
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            if (flow == null || flow.ControlsLocked)
            {
                input = Vector2.zero;
                if (visualState != null) visualState.SetMovement(moveDirection, false);
                return;
            }

            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            if (input.sqrMagnitude > 0.001f)
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                {
                    moveDirection = input.x < 0 ? 2 : 3;
                }
                else
                {
                    moveDirection = input.y > 0 ? 1 : 0;
                }
            }
            if (visualState != null) visualState.SetMovement(moveDirection, input.sqrMagnitude > 0.001f);
        }

        private void FixedUpdate() => body.linearVelocity = input * moveSpeed;

        private void LateUpdate() => sprite.sortingOrder = visualState != null && visualState.ShowingResult
            ? 710 : -Mathf.RoundToInt(transform.position.y * 20f);
    }
}
