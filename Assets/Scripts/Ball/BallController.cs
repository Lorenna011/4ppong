using UnityEngine;

namespace Ball
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BallController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float m_MoveSpeed = 5f;
        [SerializeField] private float m_MaxMoveSpeed = 15f;

        [Header("Bounce")]
        [SerializeField] private float m_MaxBounceAngle = 30f;

        private Vector2 m_Move = Vector2.right;

        private Rigidbody2D m_Body;

        private int m_LastFlipId = -1;

        private bool m_MovementEnabled = true;
        private bool m_NetworkAuthority;

        public int LastFlipId => m_LastFlipId;
        public float MaxBounceAngle => m_MaxBounceAngle;
        public Vector2 CurrentMoveDirection => m_Move;

        private void Awake()
        {
            m_Body = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (!m_NetworkAuthority || !m_MovementEnabled)
            {
                m_Body.linearVelocity =
                    Vector2.zero;

                return;
            }

            m_MoveSpeed =
                Mathf.Clamp(
                    m_MoveSpeed,
                    0f,
                    m_MaxMoveSpeed
                );

            m_Body.linearVelocity =
                m_Move * m_MoveSpeed;
        }

        public void SetNetworkAuthority(bool hasAuthority)
        {
            m_NetworkAuthority =
                hasAuthority;

            m_Body.simulated =
                hasAuthority;

            if (hasAuthority) return;

            m_Body.linearVelocity =
                Vector2.zero;

            m_Body.angularVelocity = 0f;
        }

        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            m_Move = direction.normalized;
        }

        public void ModifySpeed(float multiplier)
        {
            m_MoveSpeed *= multiplier;
        }

        public void ReflectVertical()
        {
            m_Move.y = -m_Move.y;
            SetDirection(m_Move);
        }

        public void RevertXAxis()
        {
            m_Move.x = -m_Move.x;
            SetDirection(m_Move);
        }

        public void RegisterFlipHit(int flipId)
        {
            m_LastFlipId = flipId;
        }

        public void StopAt(Vector3 position)
        {
            m_MovementEnabled = false;

            m_Body.linearVelocity =
                Vector2.zero;

            m_Body.angularVelocity = 0f;

            transform.position = position;
        }

        public void Resume(Vector2 direction)
        {
            transform.position =
                Vector3.zero;

            m_LastFlipId = -1;

            m_MovementEnabled = true;

            SetDirection(direction);

            m_Body.linearVelocity =
                m_Move * m_MoveSpeed;
        }
    }
}