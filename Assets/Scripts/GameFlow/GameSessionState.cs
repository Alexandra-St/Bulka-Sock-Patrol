namespace BulkoGame.GameFlow
{
    public enum GamePhase { MainMenu, Collect, Defense, Result }
    public enum GameResult { None, Won, Lost }

    // A new instance belongs to each Level_01 run; no state survives scene reload.
    public sealed class GameSessionState
    {
        public GamePhase Phase { get; private set; } = GamePhase.MainMenu;
        public GameResult Result { get; private set; } = GameResult.None;
        public int SocksCollected { get; private set; }
        public int SocksSafe { get; private set; }
        public int TrapsPlaced { get; private set; }
        public int CatsFinished { get; private set; }
        public bool IsCarryingSock { get; private set; }
        public bool IsCarryingDefenseSock { get; private set; }
        public int UnresolvedDefenseSocks { get; private set; }

        public void BeginCollect()
        {
            Phase = GamePhase.Collect;
            Result = GameResult.None;
            SocksCollected = 0;
            SocksSafe = 0;
            TrapsPlaced = 0;
            CatsFinished = 0;
            IsCarryingSock = false;
            IsCarryingDefenseSock = false;
            UnresolvedDefenseSocks = 0;
        }

        public bool TryPickUpSock()
        {
            if (Phase != GamePhase.Collect || IsCarryingSock) return false;
            IsCarryingSock = true;
            return true;
        }

        public bool TryDepositCarriedSock()
        {
            if (Phase != GamePhase.Collect || !IsCarryingSock) return false;
            IsCarryingSock = false;
            SocksCollected++;
            SocksSafe++;
            return true;
        }

        public bool DiscardCarriedSock()
        {
            if (!IsCarryingSock) return false;
            IsCarryingSock = false;
            return true;
        }

        public void BeginDefense() { IsCarryingSock = false; SocksSafe = SocksCollected; Phase = GamePhase.Defense; }
        public bool TryStealSock()
        {
            if (Phase != GamePhase.Defense || SocksSafe <= 0) return false;
            SocksSafe--;
            return true;
        }
        public bool TryReturnStolenSock()
        {
            if (Phase != GamePhase.Defense || SocksSafe >= SocksCollected) return false;
            SocksSafe++;
            return true;
        }
        public void RegisterDroppedDefenseSock() { UnresolvedDefenseSocks++; }
        public bool TryPickUpDefenseSock()
        {
            if (Phase != GamePhase.Defense || IsCarryingSock || UnresolvedDefenseSocks <= 0) return false;
            IsCarryingSock = true;
            IsCarryingDefenseSock = true;
            return true;
        }
        public bool TryResolveCarriedDefenseSock()
        {
            if (Phase != GamePhase.Defense || !IsCarryingDefenseSock || !IsCarryingSock || UnresolvedDefenseSocks <= 0 || SocksSafe >= SocksCollected) return false;
            IsCarryingSock = false;
            IsCarryingDefenseSock = false;
            UnresolvedDefenseSocks--;
            SocksSafe++;
            return true;
        }
        public bool ResolveDroppedDefenseSock()
        {
            if (UnresolvedDefenseSocks <= 0) return false;
            UnresolvedDefenseSocks--;
            return true;
        }
        public void PlaceTrap() { TrapsPlaced++; }
        public void FinishCat() { CatsFinished++; }
        public void Finish(bool won) { Phase = GamePhase.Result; Result = won ? GameResult.Won : GameResult.Lost; }
    }
}
