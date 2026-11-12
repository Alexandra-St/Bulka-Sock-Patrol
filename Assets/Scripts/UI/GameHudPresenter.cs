using BulkoGame.GameFlow;

namespace BulkoGame.UI
{
    // Formats session data for the existing scene view; gameplay never reads the HUD.
    public sealed class GameHudPresenter
    {
        private readonly GameHUD view;
        private readonly GameSessionState session;
        private readonly GameBalanceConfig balance;
        private readonly int sockTotal;

        public GameHudPresenter(GameHUD view, GameSessionState session, GameBalanceConfig balance, int sockTotal)
        { this.view = view; this.session = session; this.balance = balance; this.sockTotal = sockTotal; }

        public void Collect(float remaining) => view.Refresh(session.SocksCollected, sockTotal, remaining, session.IsCarryingSock);
        public void DefenseIntro(bool allCollected) => view.ShowDefense(allCollected);
        public void Defense() { view.BeginDefense(session.SocksSafe); Traps(); }
        public void SocksSafe() => view.RefreshDefenseSafe(session.SocksSafe);
        public void DefenseCarry() => view.ShowDefenseCarry(session.IsCarryingDefenseSock);
        public void Traps() => view.RefreshTrapCount(session.TrapsPlaced, balance.maxTraps);
        public void WaveStart() => view.BeginWave(balance.waveSize);
        public void Cats() => view.RefreshCatCount(session.CatsFinished, balance.waveSize);
        public void Result() => view.ShowWaveResult(session.Result == GameResult.Won, session.SocksSafe);
    }
}
