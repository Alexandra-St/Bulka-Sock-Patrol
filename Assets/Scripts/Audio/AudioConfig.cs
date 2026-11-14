using UnityEngine;

namespace BulkoGame.Audio
{
    [CreateAssetMenu(menuName = "Bulka: Sock Patrol/Audio Config")]
    public sealed class AudioConfig : ScriptableObject
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = .22f;
        [Range(0f, 1f)] public float sfxVolume = .85f;

        [Header("SFX mix multipliers")]
        [Range(0f, 4f)] public float catFearGain = 4f;
        [Range(0f, 4f)] public float sockPickupGain = 4f;
        [Range(0f, 4f)] public float sockStolenGain = 2f;
        [Range(0f, 4f)] public float trapPlaceGain = 2f;
        [Range(0f, 4f)] public float uiHoverGain = 3f;
        [Range(0f, 1f)] public float defenseStartGain = .4f;

        [Header("Bulka")]
        public AudioClip bulkaBark;
        public AudioClip sockPickup;
        public AudioClip sockDeposit;
        public AudioClip victory;
        public AudioClip lose;

        [Header("Cats")]
        public AudioClip catEntrance;
        public AudioClip catFear;
        public AudioClip sockStolen;
        public AudioClip catExit;

        [Header("Traps")]
        public AudioClip trapPlaced;
        public AudioClip trapTriggered;

        [Header("UI and flow")]
        public AudioClip buttonHover;
        public AudioClip buttonClick;
        public AudioClip startGame;
        public AudioClip resultPanel;
        public AudioClip collectToDefense;

        [Header("Music and ambience")]
        public AudioClip mainMenuMusic;
        public AudioClip gameplayMusic;
        public AudioClip gameplayAmbience;
        public AudioClip defenseMusicVariation;
    }

    public enum GameSound
    {
        BulkaBark,
        SockPickup,
        SockDeposit,
        Victory,
        Lose,
        CatEntrance,
        CatFear,
        SockStolen,
        CatExit,
        TrapPlaced,
        TrapTriggered,
        ButtonHover,
        ButtonClick,
        StartGame,
        ResultPanel,
        CollectToDefense
    }
}
