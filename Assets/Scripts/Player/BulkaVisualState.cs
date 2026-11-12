using UnityEngine;

namespace BulkoGame.Player
{
    // Visual state only. Movement, physics and Bark effects remain in their gameplay components.
    public sealed class BulkaVisualState : MonoBehaviour
    {
        private static readonly string[] Directions = { "Down", "Up", "Left", "Right" };
        private Animator animator;
        private int direction;
        private bool moving;
        private float barkUntil;
        private string current;
        private string result;
        public bool ShowingResult => result != null;

        public void Initialize(Animator visualAnimator)
        {
            animator = visualAnimator;
            Refresh();
        }

        public void SetMovement(int facing, bool isMoving)
        {
            direction = Mathf.Clamp(facing, 0, 3);
            moving = isMoving;
            Refresh();
        }

        public void PlayBark()
        {
            if (result != null) return;
            barkUntil = Time.time + .5f;
            Refresh();
        }

        public void ShowResult(bool won)
        {
            result = won ? "Victory" : "Lose";
            // Present the result pose on the card while leaving the player root and collider in place.
            var feet = new Vector3(4.65f, -1.65f, animator.transform.position.z);
            animator.transform.position = feet + new Vector3(0f, 1.12f, 0f);
            Refresh();
        }

        private void Update()
        {
            if (barkUntil > 0f && Time.time >= barkUntil)
            {
                barkUntil = 0f;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (animator == null) return;
            string mode = result ?? (barkUntil > Time.time ? "Bark" : moving ? "Walk" : "Idle");
            string next = "Bulka_" + mode + "_" + Directions[direction];
            if (next == current) return;
            current = next;
            animator.Play(next, 0, 0f);
        }
    }
}
