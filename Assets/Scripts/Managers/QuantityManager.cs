using Callback;
using UnityEngine;

namespace Score
{
    public class QuantityManager : MonoBehaviour
    {
        private int m_QuantityForPlayer1;
        private int m_QuantityForPlayer2;

        private static QuantityManager s_Instance;

        public static QuantityManager Singleton =>
            s_Instance;

        private void Awake()
        {
            if (s_Instance != null &&
                s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
        }

        private void OnEnable()
        {
            CallbackTrigger.OnScoreChanged += OnScoreChanged;
        }
        
        private void OnDisable()
        {
            CallbackTrigger.OnScoreChanged -= OnScoreChanged;
        }

        private void OnScoreChanged(int playerId)
        {
            switch (playerId)
            {
                case 1:

                    m_QuantityForPlayer1++;

                    CallbackTrigger.ScoreAdded(
                        m_QuantityForPlayer1,
                        1
                    );

                    break;

                case 2:

                    m_QuantityForPlayer2++;

                    CallbackTrigger.ScoreAdded(
                        m_QuantityForPlayer2,
                        2
                    );

                    break;
            }
        }

        public int GetQuantityForPlayer1() =>
            m_QuantityForPlayer1;

        public int GetQuantityForPlayer2() =>
            m_QuantityForPlayer2;
    }
}
