using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BulkoGame.UI;
using BulkoGame.Audio;

namespace BulkoGame.GameFlow
{
    public sealed class GameFlow : MonoBehaviour
    {
        public const string HighestUnlockedLevelKey = "BulkoGame.HighestUnlockedLevel";
        [Obsolete("Use HighestUnlockedLevelKey; retained only for one-time migration.")]
        public const string Level2UnlockedKey = "BulkoGame.Level2Unlocked";
        public const int CampaignLevelCount = 10;

        public GameSessionState Session { get; private set; }
        public bool ControlsLocked => controlsLocked || isPaused;
        public int SocksSafe => Session.SocksSafe;
        public event Action<int> DefenseStarted;
        public event Action<bool> ResultShown;
        public event Action<bool> CarryingSockChanged;
        private GameBalanceConfig balance;
        private GameHudPresenter presenter;
        private GameAudio audio;
        private float remaining;
        private int total;
        private bool controlsLocked;
        private bool isPaused;

        public static int GetHighestUnlockedLevel()
        {
            if (!PlayerPrefs.HasKey(HighestUnlockedLevelKey))
            {
#pragma warning disable CS0618
                int migrated = PlayerPrefs.GetInt(Level2UnlockedKey, 0) == 1 ? 2 : 1;
#pragma warning restore CS0618
                if (migrated > 1)
                {
                    PlayerPrefs.SetInt(HighestUnlockedLevelKey, migrated);
                    PlayerPrefs.Save();
                }
                return migrated;
            }

            int stored = PlayerPrefs.GetInt(HighestUnlockedLevelKey, 1);
            int clamped = Mathf.Clamp(stored, 1, CampaignLevelCount);
            if (stored != clamped)
            {
                PlayerPrefs.SetInt(HighestUnlockedLevelKey, clamped);
                PlayerPrefs.Save();
            }
            return clamped;
        }

        public static bool IsCampaignLevelUnlocked(int level)
        {
            return level >= 1 && level <= CampaignLevelCount && level <= GetHighestUnlockedLevel();
        }

        public static bool RegisterCampaignWin(string sceneName)
        {
            if (!TryGetCampaignLevel(sceneName, out int currentLevel) || currentLevel >= CampaignLevelCount)
                return false;

            int nextLevel = currentLevel + 1;
            if (nextLevel <= GetHighestUnlockedLevel()) return false;

            PlayerPrefs.SetInt(HighestUnlockedLevelKey, nextLevel);
            PlayerPrefs.Save();
            return true;
        }

        private static bool TryGetCampaignLevel(string sceneName, out int level)
        {
            level = 0;
            return !string.IsNullOrEmpty(sceneName)
                && sceneName.StartsWith("Level_", StringComparison.Ordinal)
                && sceneName.Length == 8
                && int.TryParse(sceneName.Substring(6), out level)
                && level >= 1 && level <= CampaignLevelCount
                && sceneName == $"Level_{level:00}";
        }

        public void Setup(GameBalanceConfig config, GameHudPresenter ui, GameSessionState session, int sockCount, Action<int> onDefenseStarted, GameAudio gameAudio)
        {
            balance = config;
            presenter = ui;
            audio = gameAudio;
            Session = session;
            DefenseStarted = onDefenseStarted;
            total = sockCount;
            remaining = balance.collectDuration;
            controlsLocked = false;
            isPaused = false;
            Session.BeginCollect();
            presenter.Collect(remaining);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        private void Update()
        {
            if (Session == null || Session.Phase != GamePhase.Collect || ControlsLocked) return;
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            presenter.Collect(remaining);
            if (Session.SocksCollected >= total) FinishCollect(true);
            else if (remaining <= 0) FinishCollect(false);
        }

        public bool TryPickUpSock()
        {
            if (Session == null || ControlsLocked || !Session.TryPickUpSock()) return false;
            presenter.Collect(remaining);
            CarryingSockChanged?.Invoke(true);
            return true;
        }

        public bool TryDepositCarriedSock()
        {
            if (Session == null || ControlsLocked || !Session.TryDepositCarriedSock()) return false;
            presenter.Collect(remaining);
            CarryingSockChanged?.Invoke(false);
            if (Session.SocksCollected >= total) FinishCollect(true);
            return true;
        }

        private void FinishCollect(bool allCollected)
        {
            if (ControlsLocked || Session.Phase != GamePhase.Collect) return;
            if (Session.DiscardCarriedSock())
            {
                presenter.Collect(remaining);
                CarryingSockChanged?.Invoke(false);
            }
            controlsLocked = true;
            StartCoroutine(DefenseTransition(allCollected));
        }

        private IEnumerator DefenseTransition(bool allCollected)
        {
            presenter.DefenseIntro(allCollected);
            if (audio != null) audio.Play(GameSound.CollectToDefense);
            yield return new WaitForSeconds(1.8f);
            Session.BeginDefense();
            presenter.Defense();
            controlsLocked = false;
            DefenseStarted?.Invoke(Session.SocksSafe);
        }

        public bool TryStealSock()
        {
            if (Session == null || ControlsLocked || !Session.TryStealSock()) return false;
            presenter.SocksSafe();
            return true;
        }

        public bool TryReturnStolenSock()
        {
            if (Session == null || ControlsLocked || !Session.TryReturnStolenSock()) return false;
            presenter.SocksSafe();
            return true;
        }
        public void NotifyDefenseSockDropped() { Session.RegisterDroppedDefenseSock(); }
        public void NotifyDefenseSockPickedUp() { CarryingSockChanged?.Invoke(true); presenter.DefenseCarry(); }
        public void NotifyDefenseSockResolved()
        {
            if (!Session.TryResolveCarriedDefenseSock()) return;
            CarryingSockChanged?.Invoke(false); presenter.SocksSafe(); presenter.DefenseCarry();
        }

        public void FinishDefense(bool won)
        {
            if (Session == null || Session.Phase != GamePhase.Defense) return;
            if (won) RegisterCampaignWin(SceneManager.GetActiveScene().name);
            controlsLocked = true;
            Session.Finish(won);
            if (audio != null) audio.Play(won ? GameSound.Victory : GameSound.Lose);
            ResultShown?.Invoke(won);
            presenter.Result();
        }
    }
}
