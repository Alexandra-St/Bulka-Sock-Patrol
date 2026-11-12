using UnityEngine;
using BulkoGame.Audio;

namespace BulkoGame.Collectibles
{
    /// <summary>Collect-phase trigger for depositing Bulka's single carried sock.</summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class BasketDepositZone : MonoBehaviour
    {
        private BulkoGame.GameFlow.GameFlow flow;
        private GameAudio audio;

        public void InitializeAudio(GameAudio gameAudio) => audio = gameAudio;

        public void Initialize(BulkoGame.GameFlow.GameFlow gameFlow, float radius)
        {
            flow = gameFlow;
            var basketRoot = transform.parent;
            var basketVisual = basketRoot != null ? basketRoot.GetComponentInChildren<BasketFillVisual>(true) : null;
            if (basketVisual != null) basketVisual.Initialize(gameFlow);
            var trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = radius;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryDeposit(other);
        private void OnTriggerStay2D(Collider2D other) => TryDeposit(other);
        private void TryDeposit(Collider2D other)
        {
            if (other.GetComponent<BulkoGame.Player.PlayerController>() == null) return;
            if (flow != null && flow.ControlsLocked) return;
            if (flow != null && flow.Session != null && flow.Session.IsCarryingDefenseSock)
            {
                int safeBefore = flow.Session.SocksSafe;
                flow.NotifyDefenseSockResolved();
                if (flow.Session.SocksSafe == safeBefore + 1) audio?.Play(GameSound.SockDeposit);
                return;
            }
            if (flow != null && flow.TryDepositCarriedSock()) audio?.Play(GameSound.SockDeposit);
        }
    }
}
