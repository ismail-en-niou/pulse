using Pulse.Core;

namespace Pulse.Tests.Core;

public class PulsePathsTests
{
    [Fact]
    public void Override_directory_wins()
    {
        var custom = Path.Combine(Path.GetTempPath(), "pulse-home");

        Assert.Equal(Path.GetFullPath(custom), PulsePaths.ResolveHome(custom));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Default_directory_is_named_Pulse(string? overrideDirectory)
    {
        var home = PulsePaths.ResolveHome(overrideDirectory);

        Assert.Equal("Pulse", Path.GetFileName(home));
        Assert.True(Path.IsPathRooted(home));
    }
}
