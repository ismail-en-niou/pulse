namespace Pulse.Tests.Platform;

internal static class TestEnvironment
{
    /// <summary>GitHub Actions sets CI. Tests that use the real keychain or Credential Manager skip there.</summary>
    public static bool IsCI => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

    public static string TempDirectory() => Path.Combine(Path.GetTempPath(), "pulse-tests-" + Guid.NewGuid().ToString("N"));
}
