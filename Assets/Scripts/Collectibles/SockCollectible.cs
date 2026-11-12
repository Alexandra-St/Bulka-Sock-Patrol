using UnityEngine;
using BulkoGame.Audio;

namespace BulkoGame.Collectibles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class SockCollectible : MonoBehaviour
    {
        private BulkoGame.GameFlow.GameFlow flow;
        private GameAudio audio;
        public void Initialize(BulkoGame.GameFlow.GameFlow gameFlow, GameAudio gameAudio) { flow = gameFlow; audio = gameAudio; }
        private void Awake() => GetComponent<Collider2D>().isTrigger = true;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<BulkoGame.Player.PlayerController>() == null) return;
            if (flow != null && flow.TryPickUpSock())
            {
                if (audio != null) audio.Play(GameSound.SockPickup);
                Destroy(gameObject);
            }
        }
    }
}
