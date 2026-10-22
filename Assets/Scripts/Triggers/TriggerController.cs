using Ball;
using Network;
using UnityEngine;

public class TriggerController : MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private PongUdpClient m_NetworkClient;

    private bool m_GameOver;

    private void OnEnable()
    {
        Callback.CallbackTrigger.OnGameOvered +=
            OnGameOver;
    }

    private void OnDisable()
    {
        Callback.CallbackTrigger.OnGameOvered -=
            OnGameOver;
    }

    private void OnGameOver(int teamId)
    {
        m_GameOver = true;
    }

    private async Awaitable OnTriggerEnter2D(
        Collider2D other)
    {
        if (m_GameOver)
            return;

        if (!other.CompareTag("Ball"))
            return;

        // Apenas Player 1 possui a bola real.
        if (m_NetworkClient == null ||
            m_NetworkClient.MyPlayerId != 1)
        {
            return;
        }

        BallController ball =
            other.GetComponent<BallController>();

        if (ball == null)
            return;

        int lastFlipId =
            ball.LastFlipId;

        int scoringTeam =
            GetScoringTeam(lastFlipId);

        if (scoringTeam == -1)
        {
            Debug.LogWarning(
                $"[TRIGGER] " +
                $"Não foi possível determinar " +
                $"quem marcou. LastFlipId = {lastFlipId}"
            );

            Vector2 directionPast =
                ball.CurrentMoveDirection;
            other.GetComponent<BallController>().Resume(-1*directionPast);
            
            return;
        }

        Debug.Log(
            $"[TRIGGER] " +
            $"Último flip = Player {lastFlipId} | " +
            $"Equipe pontuando = {scoringTeam}"
        );

        ball.StopAt(
            Vector3.zero
        );

        other.gameObject.SetActive(
            false
        );

        Debug.Log(
            $"[TRIGGER] " +
            $"Enviando SCORE:{scoringTeam}"
        );

        m_NetworkClient.SendScore(
            scoringTeam
        );

        Vector2 spawnDirection =
            scoringTeam == 1
                ? Vector2.right
                : Vector2.left;

        await SpawnBall(
            ball,
            spawnDirection
        );
    }

    private int GetScoringTeam(
        int flipId)
    {
        return flipId switch
        {
            1 or 2 => 1,
            3 or 4 => 2,
            _ => -1
        };
    }

    private async Awaitable SpawnBall(
        BallController ball,
        Vector2 direction)
    {
        await Awaitable.WaitForSecondsAsync(
            .3f
        );

        if (m_GameOver)
            return;

        ball.gameObject.SetActive(
            true
        );

        ball.Resume(
            direction
        );
    }
}