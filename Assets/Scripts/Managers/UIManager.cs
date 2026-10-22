using Callback;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Team Scores")]
        [SerializeField]
        private TMP_Text m_Team1Score;

        [SerializeField]
        private TMP_Text m_Team2Score;

        [Header("Victory")]
        [SerializeField]
        private TMP_Text m_VictoryText;

        private void Awake()
        {
            if (m_VictoryText != null)
            {
                m_VictoryText.gameObject.SetActive(
                    false
                );
            }
        }

        private void OnEnable()
        {
            CallbackTrigger.OnGameOvered +=
                GameOver;
        }

        private void OnDisable()
        {
            CallbackTrigger.OnGameOvered -=
                GameOver;
        }

        public void SetScore(
            int team1Score,
            int team2Score)
        {
            if (m_Team1Score == null ||
                m_Team2Score == null)
            {
                Debug.LogError(
                    "[UI] Textos de pontuação não atribuídos."
                );

                return;
            }

            m_Team1Score.text =
                team1Score.ToString();

            m_Team2Score.text =
                team2Score.ToString();

            Debug.Log(
                $"[UI] " +
                $"Equipe 1 = {team1Score} | " +
                $"Equipe 2 = {team2Score}"
            );
        }

        private void GameOver(int teamId)
        {
            if (m_VictoryText == null)
                return;

            string message =
                teamId switch
                {
                    1 => "A equipe da esquerda ganhou!",
                    2 => "A equipe da direita ganhou!",
                    _ => string.Empty
                };

            m_VictoryText.text =
                message;

            m_VictoryText.gameObject.SetActive(
                true
            );
        }
    }
}
