using BulkoGame.Art;
using BulkoGame.Audio;
using BulkoGame.GameFlow;
using UnityEngine;

namespace BulkoGame.Levels
{
    [CreateAssetMenu(menuName = "BulkoGame/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public string levelId;
        public string displayName;
        public GameBalanceConfig balance;
        public AudioConfig audio;
        public ApartmentArtSet art;
        public EnvironmentArtSet environmentArt;
        public int sockCount;
        public int minimumSocksPerZone;
        public int maximumSocksPerZone;
        public string[] catRouteSequence;
        public string nextLevelSceneName;
    }
}
