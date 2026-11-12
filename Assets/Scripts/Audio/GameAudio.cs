using UnityEngine;

namespace BulkoGame.Audio
{
    /// <summary>Single persistent audio facade. Empty configuration or clips are safe no-ops.</summary>
    [DisallowMultipleComponent]
    public sealed class GameAudio : MonoBehaviour
    {
        private static GameAudio instance;

        private AudioConfig config;
        private AudioSource sfxSource;
        private AudioSource musicSource;
        private AudioSource ambienceSource;

        public static GameAudio EnsureInstance(AudioConfig audioConfig)
        {
            if (instance == null) instance = FindAnyObjectByType<GameAudio>();
            if (instance == null)
            {
                var serviceObject = new GameObject("Game Audio");
                instance = serviceObject.AddComponent<GameAudio>();
            }

            instance.Initialize(audioConfig);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(AudioConfig audioConfig)
        {
            config = audioConfig;
            if (sfxSource == null) sfxSource = CreateSource("SFX", false);
            if (musicSource == null) musicSource = CreateSource("Music", true);
            if (ambienceSource == null) ambienceSource = CreateSource("Ambience", true);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public void Play(GameSound sound)
        {
            if (config == null || sfxSource == null) return;
            AudioClip clip = sound switch
            {
                GameSound.BulkaBark => config.bulkaBark,
                GameSound.SockPickup => config.sockPickup,
                GameSound.SockDeposit => config.sockDeposit,
                GameSound.Victory => config.victory,
                GameSound.Lose => config.lose,
                GameSound.CatEntrance => config.catEntrance,
                GameSound.CatFear => config.catFear,
                GameSound.SockStolen => config.sockStolen,
                GameSound.CatExit => config.catExit,
                GameSound.TrapPlaced => config.trapPlaced,
                GameSound.TrapTriggered => config.trapTriggered,
                GameSound.ButtonHover => config.buttonHover,
                GameSound.ButtonClick => config.buttonClick,
                GameSound.StartGame => config.startGame,
                GameSound.ResultPanel => config.resultPanel,
                GameSound.CollectToDefense => config.collectToDefense,
                _ => null
            };
            if (clip == null) return;
            sfxSource.volume = Mathf.Clamp01(config.masterVolume) * Mathf.Clamp01(config.sfxVolume);
            float gain = sound switch
            {
                GameSound.CatFear => config.catFearGain,
                GameSound.SockPickup => config.sockPickupGain,
                GameSound.SockStolen => config.sockStolenGain,
                GameSound.TrapPlaced => config.trapPlaceGain,
                GameSound.ButtonHover => config.uiHoverGain,
                GameSound.CollectToDefense => config.defenseStartGain,
                _ => 1f
            };
            if (sfxSource.volume > 0f && gain > 0f) sfxSource.PlayOneShot(clip, gain);
        }

        public void PlayMainMenuMusic()
        {
            if (ambienceSource != null) ambienceSource.Stop();
            PlayLoop(musicSource, config != null ? config.mainMenuMusic : null, MusicVolume);
        }

        public void PlayGameplayMusic()
        {
            PlayLoop(musicSource, config != null ? config.gameplayMusic : null, MusicVolume);
            PlayLoop(ambienceSource, config != null ? config.gameplayAmbience : null, MusicVolume);
        }


        public void PlayDefenseMusicVariation()
        {
            if (config == null || config.defenseMusicVariation == null) return;
            PlayLoop(musicSource, config.defenseMusicVariation, MusicVolume);
        }

        private float MusicVolume => config == null ? 0f : Mathf.Clamp01(config.masterVolume) * Mathf.Clamp01(config.musicVolume);

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName + " Audio Source");
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private static void PlayLoop(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null || volume <= 0f) return;
            if (source.clip == clip && source.isPlaying) return;
            source.Stop();
            source.clip = clip;
            source.volume = volume;
            source.loop = true;
            source.Play();
        }
    }
}
