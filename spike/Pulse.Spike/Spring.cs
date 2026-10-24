namespace Pulse.Spike;

/// <summary>A damped spring on one value. Retargeting keeps the current velocity, so interrupted animations stay smooth.</summary>
internal sealed class Spring
{
    private const double SubStep = 1.0 / 240;
    private const double MaxFrameTime = 0.1;

    private double _stiffness;
    private double _damping;

    public double Value { get; private set; }

    public double Velocity { get; private set; }

    public double Target { get; private set; }

    public bool IsSettled => Math.Abs(Value - Target) < 0.001 && Math.Abs(Velocity) < 0.01;

    /// <param name="target">Value to move towards.</param>
    /// <param name="response">Period of the undamped oscillation in seconds; lower is faster.</param>
    /// <param name="dampingRatio">1 is critically damped; lower values overshoot.</param>
    public void Retarget(double target, double response, double dampingRatio)
    {
        var omega = 2 * Math.PI / response;
        Target = target;
        _stiffness = omega * omega;
        _damping = 2 * dampingRatio * omega;
    }

    /// <summary>Integrates with fixed sub-steps (semi-implicit Euler) so the motion doesn't depend on the frame rate.</summary>
    public void Advance(double seconds)
    {
        var remaining = Math.Clamp(seconds, 0, MaxFrameTime);
        while (remaining > 0)
        {
            var dt = Math.Min(SubStep, remaining);
            var acceleration = -_stiffness * (Value - Target) - _damping * Velocity;
            Velocity += acceleration * dt;
            Value += Velocity * dt;
            remaining -= dt;
        }

        if (IsSettled)
        {
            Value = Target;
            Velocity = 0;
        }
    }
}
