using UnityEngine;

namespace BulkoGame.Collectibles
{
    public enum SockSpawnRoom { Bedroom, Hallway, LivingRoom, Kitchen, Bathroom }

    /// <summary>A hand-authored, room-tagged sock location in Level_01.</summary>
    public sealed class SockSpawnPoint : MonoBehaviour
    {
        [SerializeField] private SockSpawnRoom room;
        public SockSpawnRoom Room => room;
    }
}
