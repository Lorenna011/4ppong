using Ball;
using UnityEngine;

namespace Border
{
    public class BorderController : MonoBehaviour
    {
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.gameObject.CompareTag("Ball"))
                return;

            var ball =
                other.gameObject.GetComponent<BallController>();

            if (ball == null)
                return;

            ball.ReflectVertical();
        }
    }
}
