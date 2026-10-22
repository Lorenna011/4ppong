using System;

namespace Callback
{
    public static class CallbackTrigger
    {
        public static Action<int> OnScoreChanged;

        public static Action<int, int> OnScoreAdded;

        public static Action<int> OnGameOvered;

        public static void ScoreChanged(int playerId)
        {
            OnScoreChanged?.Invoke(playerId);
        }

        public static void ScoreAdded(int score, int playerId)
        {
            OnScoreAdded?.Invoke(score, playerId);
        }

        public static void GameOver(int teamId)
        {
            OnGameOvered?.Invoke(teamId);
        }
    }
}
