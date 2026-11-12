using UnityEngine;
using GameFlowController = BulkoGame.GameFlow.GameFlow;

namespace BulkoGame.Collectibles
{
    /// <summary>Displays the basket fill state from the session's current safe sock count.</summary>
    public sealed class BasketFillVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] fillStates = new Sprite[4];

        private GameFlowController flow;
        private int displayedSockCount = int.MinValue;

        public void Initialize(GameFlowController gameFlow)
        {
            flow = gameFlow;
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            Refresh(true);
        }

        private void Update() => Refresh(false);

        private void Refresh(bool force)
        {
            if (flow == null || flow.Session == null || targetRenderer == null) return;
            int safeSockCount = flow.Session.SocksSafe;
            if (!force && safeSockCount == displayedSockCount) return;

            int stateIndex = safeSockCount <= 0 ? 0
                : safeSockCount <= 3 ? 1
                : safeSockCount <= 7 ? 2 : 3;
            if (fillStates != null && stateIndex < fillStates.Length && fillStates[stateIndex] != null)
                targetRenderer.sprite = fillStates[stateIndex];
            displayedSockCount = safeSockCount;
        }
    }
}
