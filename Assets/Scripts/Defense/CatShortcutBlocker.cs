using UnityEngine;

namespace BulkoGame.Defense
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CatShortcutBlocker : MonoBehaviour
    {
        public Collider2D BlockerCollider { get; private set; }

        private void Awake()
        {
            BlockerCollider = GetComponent<BoxCollider2D>();
            if (BlockerCollider != null) BlockerCollider.isTrigger = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var blocker = GetComponent<BoxCollider2D>();
            if (blocker != null) blocker.isTrigger = false;
        }
#endif
    }
}
