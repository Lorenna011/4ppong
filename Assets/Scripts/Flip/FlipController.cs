using Ball;
using Input;
using Network;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlipController : MonoBehaviour
{
    [Header("Identifier Settings")]
    [SerializeField] private int m_Id;

    [Header("Movement Settings")]
    [SerializeField] private float m_MoveSpeed;

    [Header("Network Settings")]
    [SerializeField] private PongUdpClient m_NetworkClient;

    private ActionAsset m_Asset;

    private Vector2 m_MoveInput;

    private Rigidbody2D m_Body;

    private Collider2D m_Collider;

    public int ID => m_Id;

    private void Awake()
    {
        m_Asset = new ActionAsset();
    }

    private void OnEnable()
    {
        m_Asset.Enable();
        m_Asset.asset.Enable();

        m_Asset.asset["Move"].performed += OnMove;
        m_Asset.asset["Move"].canceled += OnMoveCancel;

    }

    private void OnDisable()
    {
        m_Asset.asset["Move"].performed -= OnMove;
        m_Asset.asset["Move"].canceled -= OnMoveCancel;

        m_Asset.asset.Disable();
        m_Asset.Disable();
    }

    private void Start()
    {
        m_Body = GetComponent<Rigidbody2D>();
        m_Collider = GetComponent<Collider2D>();
    }
    
    private bool IsLocal()
    {
        return m_NetworkClient &&
               m_NetworkClient.IsLocalPlayer(m_Id);
    }

    private void FixedUpdate()
    {
        if (!IsLocal())
            return;

        var movement =
            new Vector2(
                0f,
                m_MoveInput.y *
                m_MoveSpeed *
                Time.fixedDeltaTime
            );

        m_Body.MovePosition(
            m_Body.position + movement
        );
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (!IsLocal())
            return;

        m_MoveInput =
            ctx.ReadValue<Vector2>();
    }

    private void OnMoveCancel(InputAction.CallbackContext ctx)
    {
        if (!IsLocal())
            return;

        m_MoveInput = Vector2.zero;
    }

    private void OnCollisionEnter2D(
        Collision2D other)
    {
        if (!other.gameObject.CompareTag("Ball"))
            return;

        if (m_NetworkClient == null)
            return;

        // Apenas o cliente que possui a bola
        // processa a colisão física.
        if (m_NetworkClient.MyPlayerId != 1)
            return;

        var ball =
            other.gameObject.GetComponent<BallController>();

        if (ball == null)
            return;

        ball.RegisterFlipHit(m_Id);

        var contact =
            other.GetContact(0);

        var normalizedImpact =
            (
                contact.point.y -
                m_Collider.bounds.center.y
            ) /
            m_Collider.bounds.extents.y;

        normalizedImpact =
            Mathf.Clamp(
                normalizedImpact,
                -1f,
                1f
            );

        BallActionBuilder.For(ball)
            .RevertItsHorizontal()
            .MultipliedByRange(1.1f, 1.3f)
            .BounceOffFlipper(
                normalizedImpact,
                transform.position.x
            )
            .Execute();
    }
}
