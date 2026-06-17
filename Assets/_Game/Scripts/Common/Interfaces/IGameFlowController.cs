namespace RhythmGame.Common.Interfaces
{
    public interface IGameFlowController
    {
        /// <summary>
        /// Restarts the current gameplay session.
        /// </summary>
        void RetryGame();

        /// <summary>
        /// Quits the current gameplay session and returns to the lobby.
        /// </summary>
        void QuitToLobby();
    }
}
