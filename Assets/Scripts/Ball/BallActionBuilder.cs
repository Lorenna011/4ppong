using System;
using System.Threading;
using UnityEngine;
using static UnityEngine.Random;

namespace Ball
{
    /// <summary>
    /// Defines the contract for the initial stage of the Ball Fluent API,
    /// forcing the developer to select a directional behavior first.
    /// </summary>
    public interface IBallDirectionSelector
    {
        /// <summary>
        /// Sets a specific target direction for the ball.
        /// </summary>
        /// <param name="direction">The geometric vector representing the new direction.</param>
        /// <returns>The next stage of the fluent flow to apply physical modifiers.</returns>
        IBallModifierFlow WithDirection(Vector2 direction);

        /// <summary>
        /// Commands the ball to instantly invert its current movement on the horizontal (X) axis.
        /// </summary>
        /// <returns>The next stage of the fluent flow to apply physical modifiers.</returns>
        IBallModifierFlow RevertItsHorizontal();

        /// <summary>
        /// Bypasses any direction changes, maintaining the ball's current movement trajectory.
        /// </summary>
        /// <returns>The next stage of the fluent flow to apply physical modifiers.</returns>
        IBallModifierFlow KeepCurrentDirection();
    }

    /// <summary>
    /// Defines the contract for the modification and execution stage of the Ball Fluent API.
    /// Allows multiple physical modifiers to be chained consecutively.
    /// </summary>
    public interface IBallModifierFlow
    {
        /// <summary>
        /// Multiplies the current ball speed by a given scale factor.
        /// </summary>
        /// <param name="multiplier">The value to multiply the current speed by.</param>
        /// <returns>The current fluent flow instance for further modifications.</returns>
        IBallModifierFlow MultipliedBy(float multiplier);

        /// <summary>
        /// Multiplies the current ball speed by a random value within a specified range.
        /// </summary>
        /// <param name="min">The minimum value for the random multiplier.</param>
        /// <param name="max">The maximum value for the random multiplier.</param>
        /// <returns>The current fluent flow instance for further modifications.</returns>
        IBallModifierFlow MultipliedByRange(float min, float max);

        /// <summary>
        /// Flags the ball to invert its vertical movement trajectory upon execution.
        /// </summary>
        /// <returns>The current fluent flow instance for further modifications.</returns>
        IBallModifierFlow BounceVertically();

        /// <summary>
        /// Calculates and schedules a specialized angled reflection based on a flipper hit impact.
        /// </summary>
        /// <param name="normalizedImpact">The hit point on the flipper, clamped between -1.0f and 1.0f.</param>
        /// <param name="flipperPositionX">The horizontal global position of the flipper transform.</param>
        /// <returns>The current fluent flow instance for further modifications.</returns>
        IBallModifierFlow BounceOffFlipper(float normalizedImpact, float flipperPositionX);

        /// <summary>
        /// Synchronously evaluates all scheduled configurations and applies them to the target ball.
        /// </summary>
        void Execute();

        /// <summary>
        /// Asynchronously evaluates and applies all scheduled configurations to the target ball after a specified delay.
        /// </summary>
        /// <param name="delayInSeconds">The amount of time to wait in seconds before applying the effects.</param>
        /// <returns>An asynchronous Unity Awaitable handle.</returns>
        Awaitable ExecuteWithDelay(float delayInSeconds);
    }

    /// <summary>
    /// A highly expressive Fluent API Builder designed to schedule, modify, and execute physics actions 
    /// sequentially on a specific <see cref="BallController"/>.
    /// </summary>
    public class BallActionBuilder : IBallDirectionSelector, IBallModifierFlow
    {
        /// <summary>The reference to the controller being manipulated by this builder.</summary>
        private readonly BallController m_Target;

        /// <summary>Stores the new direction vector to be applied to the ball.</summary>
        private Vector2 m_PendingDirection;

        /// <summary>The cumulative speed multiplier coefficient. Defaults to 1.0f.</summary>
        private float m_SpeedMultiplier;

        /// <summary>Flag stating whether the vertical axis should be inverted during execution.</summary>
        private bool m_ShouldReflectVertical;

        /// <summary>Flag stating whether the horizontal axis should be inverted during execution.</summary>
        private bool m_ShouldRevertX;

        /// <summary>The cached clamped impact factor (-1 to 1) for angled flipper bounces. Null if not scheduled.</summary>
        private float? m_NormalizedImpact;

        /// <summary>The cached horizontal coordinate of the hitting flipper.</summary>
        private float m_FlipperPositionX;

        /// <summary>
        /// Entry point for the Fluent API chain. Initializes a builder instance restricted to the Direction selection stage.
        /// </summary>
        /// <param name="ball">The target <see cref="BallController"/> instance to act upon.</param>
        /// <returns>An <see cref="IBallDirectionSelector"/> interface wrapping the builder instance.</returns>
        public static IBallDirectionSelector For(BallController ball)
        {
            return new BallActionBuilder(ball);
        }

        /// <summary>
        /// Private constructor protecting instantiation, forcing the usage of the <see cref="For"/> factory method.
        /// </summary>
        private BallActionBuilder(BallController ball)
        {
            m_Target = ball;
            m_PendingDirection = Vector2.zero;
            m_SpeedMultiplier = 1f;
            m_ShouldReflectVertical = false;
            m_ShouldRevertX = false;
            m_NormalizedImpact = null;
            m_FlipperPositionX = 0f;
        }

        /// <inheritdoc />
        public IBallModifierFlow WithDirection(Vector2 direction)
        {
            m_PendingDirection = direction;
            return this;
        }

        /// <inheritdoc />
        public IBallModifierFlow RevertItsHorizontal()
        {
            m_ShouldRevertX = true;
            return this;
        }

        /// <inheritdoc />
        public IBallModifierFlow KeepCurrentDirection()
        {
            return this;
        }

        /// <inheritdoc />
        public IBallModifierFlow MultipliedBy(float multiplier)
        {
            m_SpeedMultiplier *= multiplier;
            return this;
        }

        /// <inheritdoc />
        public IBallModifierFlow MultipliedByRange(float min, float max)
        {
            m_SpeedMultiplier *= Range(min, max);
            return this;
        }

        /// <inheritdoc />
        public IBallModifierFlow BounceVertically()
        {
            m_ShouldReflectVertical = true;
            return this;
        }

        /// <inheritdoc />
        public IBallModifierFlow BounceOffFlipper(float normalizedImpact, float flipperPositionX)
        {
            m_NormalizedImpact = Mathf.Clamp(normalizedImpact, -1f, 1f);
            m_FlipperPositionX = flipperPositionX;
            return this;
        }

        /// <inheritdoc />
        public void Execute()
        {
            if (m_Target == null) return;

            if (m_PendingDirection != Vector2.zero)
                m_Target.SetDirection(m_PendingDirection);

            if (m_ShouldRevertX)
                m_Target.RevertXAxis();

            if (m_ShouldReflectVertical)
                m_Target.ReflectVertical();

            if (!Mathf.Approximately(m_SpeedMultiplier, 1f))
                m_Target.ModifySpeed(m_SpeedMultiplier);

            if (m_NormalizedImpact.HasValue)
                ApplyFlipReflection();
        }

        /// <inheritdoc />
        public async Awaitable ExecuteWithDelay(float delayInSeconds)
        {
            using var cts = new CancellationTokenSource();
            try
            {
                await Awaitable.WaitForSecondsAsync(delayInSeconds, cts.Token);
                Execute();
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Computes advanced trigonometry to output a precise rebound vector 
        /// relative to the ball's side position against the flipper and the calculated impact angle.
        /// </summary>
        private void ApplyFlipReflection()
        {
            var directionX = Mathf.Sign(m_Target.transform.position.x - m_FlipperPositionX);

            try
            {
                var angleInRadians = m_NormalizedImpact.Value * m_Target.MaxBounceAngle * Mathf.Deg2Rad;

                var outX = directionX * Mathf.Cos(angleInRadians);
                var outY = Mathf.Sin(angleInRadians);

                var finalDirection = new Vector2(outX, outY).normalized;
                m_Target.SetDirection(finalDirection);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogException(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error calculating flip reflection: {ex.Message}");
            }
        }
    }
}
