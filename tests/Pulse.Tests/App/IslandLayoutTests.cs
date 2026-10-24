using Pulse.App.Island;
using Pulse.Core.Platform;

namespace Pulse.Tests.App;

public class IslandLayoutTests
{
    private static readonly IslandLayout Capsule = IslandLayout.Create(null);
    private static readonly IslandLayout Notched = IslandLayout.Create(new NotchInfo(185, 32));

    public static TheoryData<bool> BothStyles => new(false, true);

    private static IslandLayout Layout(bool notched) => notched ? Notched : Capsule;

    [Fact]
    public void Without_a_notch_the_pill_floats_below_the_top_and_widens_for_an_activity()
    {
        Assert.False(Capsule.NotchStyle);
        Assert.True(Capsule.TopOffset > 0);
        Assert.True(Capsule.Active.Width > Capsule.Idle.Width);
        Assert.Equal(Capsule.Idle, Capsule.Target(IslandContent.Idle, expanded: false));
        Assert.Equal(Capsule.Active, Capsule.Target(new IslandContent(true, 3, false), expanded: false));
    }

    [Fact]
    public void With_a_notch_the_pill_hugs_it_and_grows_ears_for_an_activity()
    {
        Assert.True(Notched.NotchStyle);
        Assert.Equal(0, Notched.TopOffset);
        Assert.Equal(32, Notched.Idle.Height);
        Assert.Equal(Math.Max(200, 185 + 24), Notched.Idle.Width);
        Assert.Equal(185 + 2 * IslandLayout.EarWidth, Notched.Active.Width);
        Assert.True(Notched.ContentTop > 32, "the list starts below the notch");
    }

    [Theory]
    [MemberData(nameof(BothStyles))]
    public void Expanded_height_follows_the_rows_up_to_the_maximum(bool notched)
    {
        var layout = Layout(notched);
        double Height(int rows, bool more = false) => layout.Target(new IslandContent(true, rows, more), expanded: true).Height;

        Assert.Equal(Height(1), Height(0)); // the empty state takes one row
        Assert.True(Height(2) > Height(1));
        Assert.Equal(Height(1) + IslandLayout.RowHeight + IslandLayout.RowGap, Height(2));
        Assert.Equal(Height(IslandLayout.MaxRows), Height(IslandLayout.MaxRows + 3));
        Assert.Equal(Height(3) + IslandLayout.MoreHeight, Height(3, more: true));
        Assert.Equal(IslandLayout.ExpandedWidth, layout.Target(IslandContent.Idle, expanded: true).Width);
    }

    [Theory]
    [MemberData(nameof(BothStyles))]
    public void Every_target_fits_the_window_with_room_to_overshoot(bool notched)
    {
        var layout = Layout(notched);
        var tallest = Math.Max(
            layout.Target(new IslandContent(true, IslandLayout.MaxRows, false), expanded: true).Height,
            layout.Target(new IslandContent(true, IslandLayout.MaxRows - 1, true), expanded: true).Height);

        Assert.True(layout.WindowSize.Width >= IslandLayout.ExpandedWidth * 1.08);
        Assert.True(layout.WindowSize.Height >= layout.TopOffset + tallest + 16);
        Assert.Equal(0, layout.WindowSize.Width % 2); // so the pill centers on whole DIPs
    }

    [Theory]
    [MemberData(nameof(BothStyles))]
    public void Frames_are_clamped_into_the_window(bool notched)
    {
        var layout = Layout(notched);

        var frame = layout.FrameAt(10_000, 10_000, 3);
        var rect = layout.PillRect(frame);

        Assert.True(rect.X >= 0 && rect.Right <= layout.WindowSize.Width);
        Assert.True(rect.Bottom <= layout.WindowSize.Height);
        Assert.Equal(1, layout.FrameAt(-50, -50, -1).Width);
    }

    [Fact]
    public void The_pill_is_centered_at_its_top_offset()
    {
        var rect = Capsule.PillRect(Capsule.FrameAt(300, 36, 0));

        Assert.Equal((Capsule.WindowSize.Width - 300) / 2, rect.X);
        Assert.Equal(Capsule.TopOffset, rect.Y);
    }

    [Fact]
    public void A_capsule_is_round_when_collapsed_and_softer_when_expanded()
    {
        Assert.Equal(18, Capsule.FrameAt(300, 36, 0).CornerRadius.TopLeft);
        Assert.Equal(28, Capsule.FrameAt(420, 200, 1).CornerRadius.TopLeft);
    }

    [Fact]
    public void A_notched_pill_keeps_a_flat_top()
    {
        var corners = Notched.FrameAt(400, 200, 1).CornerRadius;

        Assert.Equal((0, 0), (corners.TopLeft, corners.TopRight));
        Assert.True(corners.BottomLeft > 0);
    }

    [Fact]
    public void Compact_and_expanded_content_never_show_together()
    {
        for (var e = 0.0; e <= 1.0; e += 0.05)
        {
            var frame = Capsule.FrameAt(300, 100, e);
            Assert.True(frame.CompactOpacity == 0 || frame.ExpandedOpacity == 0, $"both visible at {e:0.00}");
        }

        Assert.Equal((1, 0), (Capsule.FrameAt(300, 36, 0).CompactOpacity, Capsule.FrameAt(300, 36, 0).ExpandedOpacity));
        Assert.Equal((0, 1), (Capsule.FrameAt(420, 200, 1).CompactOpacity, Capsule.FrameAt(420, 200, 1).ExpandedOpacity));
    }
}
