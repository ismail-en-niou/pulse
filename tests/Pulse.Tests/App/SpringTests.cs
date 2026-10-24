using Pulse.App.Island;

namespace Pulse.Tests.App;

public class SpringTests
{
    private static int FramesToSettle(Spring spring, int maxFrames = 600)
    {
        for (var frame = 1; frame <= maxFrames; frame++)
        {
            spring.Advance(1.0 / 60);
            if (spring.IsSettled)
                return frame;
        }

        return -1;
    }

    [Fact]
    public void A_new_spring_rests_at_its_value()
    {
        var spring = new Spring(160, precision: 0.05);

        Assert.True(spring.IsSettled);
        Assert.Equal(160, spring.Value);
    }

    [Fact]
    public void Pixel_sized_moves_settle_within_a_second_and_land_exactly()
    {
        var spring = new Spring(160, precision: 0.05);
        spring.Retarget(420, response: 0.42, dampingRatio: 0.68);

        var frames = FramesToSettle(spring);

        Assert.InRange(frames, 1, 60);
        Assert.Equal(420, spring.Value);
        Assert.Equal(0, spring.Velocity);
    }

    [Fact]
    public void An_underdamped_spring_overshoots_a_little()
    {
        var spring = new Spring(0);
        spring.Retarget(1, response: 0.42, dampingRatio: 0.68);

        var peak = 0.0;
        for (var i = 0; i < 120; i++)
        {
            spring.Advance(1.0 / 60);
            peak = Math.Max(peak, spring.Value);
        }

        Assert.InRange(peak, 1.01, 1.1);
    }

    [Fact]
    public void Retargeting_mid_flight_keeps_the_velocity()
    {
        var spring = new Spring(0);
        spring.Retarget(1, response: 0.42, dampingRatio: 0.68);
        for (var i = 0; i < 5; i++)
            spring.Advance(1.0 / 60);
        var velocity = spring.Velocity;

        spring.Retarget(0, response: 0.32, dampingRatio: 0.85);

        Assert.Equal(velocity, spring.Velocity);
        Assert.True(velocity > 0);
    }

    [Fact]
    public void A_long_frame_is_capped_so_the_spring_stays_stable()
    {
        var spring = new Spring(0);
        spring.Retarget(100, response: 0.2, dampingRatio: 0.5);

        spring.Advance(30); // a stalled UI thread

        Assert.InRange(spring.Value, 0, 150);
    }

    [Fact]
    public void Snap_jumps_without_motion()
    {
        var spring = new Spring(0);
        spring.Retarget(1, response: 0.4, dampingRatio: 0.7);
        spring.Advance(1.0 / 60);

        spring.Snap(5);

        Assert.True(spring.IsSettled);
        Assert.Equal((5.0, 5.0, 0.0), (spring.Value, spring.Target, spring.Velocity));
    }
}
