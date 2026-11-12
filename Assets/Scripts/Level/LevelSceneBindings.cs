using BulkoGame.Collectibles;
using BulkoGame.Defense;
using UnityEngine;

namespace BulkoGame.Levels
{
    public sealed class LevelSceneBindings : MonoBehaviour
    {
        public LevelDefinition definition;
        public Transform playerSpawn;
        public Transform basketRoot;
        public CatRoute[] routes;
        public TrapPlacementZone[] trapZones;
        public SockSpawnPoint[] sockSpawnPoints;
        public Camera gameplayCamera;
    }
}
