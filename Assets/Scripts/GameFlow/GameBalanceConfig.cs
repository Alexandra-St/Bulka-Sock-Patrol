using UnityEngine;

namespace BulkoGame.GameFlow
{
    [CreateAssetMenu(menuName = "BulkoGame/Game Balance")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        public float collectDuration = 60f;
        public float bulkaSpeed = 4.4f;
        public float barkRadius = 2.5f;
        public float barkCooldown = 4f;
        public float fearDuration = 1.25f;
        public int barkHitsToFlee = 2;
        public int trappedBarkHitsToFlee = 1;
        public float catSpeed = 2.4f;
        public int waveSize = 5;
        public float spawnInterval = 2.5f;
        public int maxSimultaneousCats = 3;
        public int maxTraps = 2;
        public float slowTrapMultiplier = .5f;
        public float slowTrapDuration = 2f;
        public float defenseSockStunDuration = 2f;
    }
}
