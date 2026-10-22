using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Ball;
using Callback;
using UnityEngine;

namespace Network
{
    public class PongUdpClient : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField]
        private string m_ServerAddress =
            "127.0.0.1";

        [SerializeField]
        private int m_ServerPort = 5001;

        [Header("Game References")]
        [Tooltip(
            "Índice 0 = Player 1, " +
            "Índice 1 = Player 2, " +
            "Índice 2 = Player 3, " +
            "Índice 3 = Player 4."
        )]
        [SerializeField]
        private FlipController[] m_Flips =
            new FlipController[4];

        [SerializeField]
        private BallController m_Ball;

        [SerializeField]
        private UI.UIManager m_UIManager;

        private UdpClient m_Client;

        private IPEndPoint m_ServerEndpoint;

        private Thread m_ReceiveThread;

        private volatile bool m_Running;

        private int m_MyPlayerId = -1;

        private readonly Vector3[]
            m_RemoteFlipPositions =
                new Vector3[4];

        private readonly bool[]
            m_HasRemoteFlip =
                new bool[4];

        private Vector3 m_RemoteBallPosition;

        private bool m_HasRemoteBall;

        private bool m_GameOver;

        private readonly ConcurrentQueue<Action>
            m_MainThreadActions = new();

        private void Start()
        {
            if (m_Flips == null ||
                m_Flips.Length != 4)
            {
                Debug.LogError(
                    "[CLIENT] " +
                    "m_Flips precisa conter exatamente 4 flips."
                );

                return;
            }

            try
            {
                m_Client =
                    new UdpClient(
                        AddressFamily.InterNetwork
                    );

                m_ServerEndpoint =
                    new IPEndPoint(
                        IPAddress.Parse(
                            m_ServerAddress
                        ),
                        m_ServerPort
                    );

                m_Client.Connect(
                    m_ServerEndpoint
                );

                m_Running = true;

                m_ReceiveThread =
                    new Thread(ReceiveData)
                    {
                        IsBackground = true
                    };

                m_ReceiveThread.Start();

                Send("HELLO");

                Debug.Log(
                    $"[CLIENT] " +
                    $"Conectando a {m_ServerEndpoint}..."
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[CLIENT] Falha ao iniciar: {exception}"
                );
            }
        }

        private void Update()
        {
            while (
                m_MainThreadActions.TryDequeue(
                    out var action))
            {
                action?.Invoke();
            }

            if (m_MyPlayerId <= 0)
                return;

            if (!m_GameOver)
            {
                SendLocalFlipPosition();

                // Player 1 é a autoridade da bola.
                if (m_MyPlayerId == 1)
                {
                    SendBallPosition();
                }
            }

            UpdateRemoteFlips();
            UpdateRemoteBall();
        }

        public bool IsLocalPlayer(
            int playerId)
        {
            return m_MyPlayerId == playerId;
        }

        private void SendLocalFlipPosition()
        {
            int index =
                m_MyPlayerId - 1;

            if (index < 0 ||
                index >= m_Flips.Length)
            {
                return;
            }

            FlipController flip =
                m_Flips[index];

            if (flip == null)
                return;

            string message =
                FormattableString.Invariant($"FLIP:{m_MyPlayerId};{flip.transform.position.x:F3};{flip.transform.position.y:F3}");

            Send(message);
        }

        private void SendBallPosition()
        {
            if (m_Ball == null)
                return;

            string message =
                FormattableString.Invariant($"BALL:{m_Ball.transform.position.x:F3};{m_Ball.transform.position.y:F3}");

            Send(message);
        }

        private void ReceiveData()
        {
            IPEndPoint remoteEndpoint =
                new(IPAddress.Any, 0);

            while (m_Running)
            {
                try
                {
                    byte[] data =
                        m_Client.Receive(
                            ref remoteEndpoint
                        );

                    string message =
                        Encoding.UTF8.GetString(data);

                    HandleMessage(message);
                }
                catch (SocketException)
                {
                    if (!m_Running)
                        break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"[CLIENT] {exception}"
                    );
                }
            }
        }

        private void HandleMessage(
            string message)
        {
            Debug.Log(
                $"[CLIENT] Recebi: {message}"
            );

            if (message.StartsWith(
                    "ASSIGN:",
                    StringComparison.Ordinal))
            {
                ParseAssign(message);
                return;
            }

            if (message.StartsWith(
                    "FLIP:",
                    StringComparison.Ordinal))
            {
                ParseFlip(message);
                return;
            }

            if (message.StartsWith(
                    "BALL:",
                    StringComparison.Ordinal))
            {
                ParseBall(message);
                return;
            }

            if (message.StartsWith(
                    "STATE:SCORE;",
                    StringComparison.Ordinal))
            {
                ParseScore(message);
                return;
            }

            if (message.StartsWith(
                    "GAMEOVER:",
                    StringComparison.Ordinal))
            {
                ParseGameOver(message);
                return;
            }

            if (message == "FULL")
            {
                m_MainThreadActions.Enqueue(() =>
                {
                    Debug.LogWarning(
                        "[CLIENT] Servidor cheio."
                    );
                });
            }
        }

        private void ParseAssign(
            string message)
        {
            if (!int.TryParse(
                    message.Substring(7),
                    out int id))
            {
                return;
            }

            m_MainThreadActions.Enqueue(() =>
            {
                m_MyPlayerId = id;

                Debug.Log(
                    $"[CLIENT] Meu ID = {id}"
                );

                // Apenas Player 1 controla a física da bola.
                if (m_Ball != null)
                {
                    m_Ball.SetNetworkAuthority(
                        id == 1
                    );
                }
            });
        }

        private void ParseFlip(
            string message)
        {
            string payload =
                message.Substring(5);

            string[] parts =
                payload.Split(';');

            if (parts.Length != 3)
                return;

            if (!int.TryParse(
                    parts[0],
                    out int playerId))
            {
                return;
            }

            if (playerId == m_MyPlayerId)
                return;

            if (playerId < 1 ||
                playerId > 4)
            {
                return;
            }

            if (!float.TryParse(
                    parts[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float x))
            {
                return;
            }

            if (!float.TryParse(
                    parts[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float y))
            {
                return;
            }

            int index =
                playerId - 1;

            m_RemoteFlipPositions[index] =
                new Vector3(x, y, 0f);

            m_HasRemoteFlip[index] = true;
        }

        private void ParseBall(
            string message)
        {
            if (m_MyPlayerId == 1)
                return;

            string payload =
                message.Substring(5);

            string[] parts =
                payload.Split(';');

            if (parts.Length != 2)
                return;

            if (!float.TryParse(
                    parts[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float x))
            {
                return;
            }

            if (!float.TryParse(
                    parts[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float y))
            {
                return;
            }

            m_RemoteBallPosition =
                new Vector3(x, y, 0f);

            m_HasRemoteBall = true;
        }

        private void ParseScore(
            string message)
        {
            string payload =
                message.Substring(
                    "STATE:SCORE;".Length
                );

            string[] parts =
                payload.Split(';');

            if (parts.Length != 2)
                return;

            if (!int.TryParse(
                    parts[0],
                    out int team1Score))
            {
                return;
            }

            if (!int.TryParse(
                    parts[1],
                    out int team2Score))
            {
                return;
            }

            Debug.Log(
                $"[CLIENT] " +
                $"Placar recebido: " +
                $"Equipe 1 = {team1Score} | " +
                $"Equipe 2 = {team2Score}"
            );

            m_MainThreadActions.Enqueue(() =>
            {
                if (m_UIManager == null)
                {
                    Debug.LogError(
                        "[CLIENT] UIManager não foi atribuído."
                    );

                    return;
                }

                m_UIManager.SetScore(
                    team1Score,
                    team2Score
                );
            });
        }

        private void ParseGameOver(
            string message)
        {
            if (!int.TryParse(
                    message.Substring(9),
                    out int winningTeam))
            {
                return;
            }

            Debug.Log(
                $"[CLIENT] GAMEOVER recebido: equipe {winningTeam}"
            );

            m_MainThreadActions.Enqueue(() =>
            {
                m_GameOver = true;

                CallbackTrigger.GameOver(
                    winningTeam
                );

                if (m_Ball == null)
                    return;

                m_Ball.StopAt(
                    Vector3.zero
                );
            });
        }

        private void UpdateRemoteFlips()
        {
            for (int i = 0;
                 i < m_Flips.Length;
                 i++)
            {
                int playerId =
                    i + 1;

                if (playerId == m_MyPlayerId)
                    continue;

                if (!m_HasRemoteFlip[i])
                    continue;

                FlipController flip =
                    m_Flips[i];

                if (flip == null)
                    continue;

                flip.transform.position =
                    Vector3.Lerp(
                        flip.transform.position,
                        m_RemoteFlipPositions[i],
                        Time.deltaTime * 15f
                    );
            }
        }

        private void UpdateRemoteBall()
        {
            if (m_MyPlayerId == 1)
                return;

            if (!m_HasRemoteBall)
                return;

            if (m_Ball == null)
                return;

            m_Ball.transform.position =
                Vector3.Lerp(
                    m_Ball.transform.position,
                    m_RemoteBallPosition,
                    Time.deltaTime * 20f
                );
        }

        private void Send(
            string message)
        {
            if (m_Client == null)
                return;

            try
            {
                byte[] data =
                    Encoding.UTF8.GetBytes(
                        message
                    );

                Debug.Log(
                    $"[CLIENT] Enviando: {message}"
                );

                m_Client.Send(
                    data,
                    data.Length
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[CLIENT] Erro ao enviar: {exception}"
                );
            }
        }

        public void SendScore(int teamId)
        {
            if (teamId is < 1 or > 2)
            {
                return;
            }

            Send($"SCORE:{teamId}");
        }

        public int MyPlayerId =>
            m_MyPlayerId;

        private void OnApplicationQuit()
        {
            StopClient();
        }

        private void OnDestroy()
        {
            StopClient();
        }

        private void StopClient()
        {
            if (!m_Running)
                return;

            m_Running = false;

            try
            {
                m_Client?.Close();
            }
            catch
            {
            }

            if (m_ReceiveThread != null &&
                m_ReceiveThread.IsAlive)
            {
                m_ReceiveThread.Join(100);
            }

            m_Client = null;
        }
    }
}
