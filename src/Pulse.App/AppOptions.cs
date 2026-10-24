namespace Pulse.App;

/// <summary>Command-line switches for development and manual testing.</summary>
/// <param name="DebugBounds">Outline the whole (transparent) window, to see where clicks must pass through.</param>
/// <param name="MacNonActivating">macOS: probe clicking the pill without activating Pulse (private API).</param>
public sealed record AppOptions(bool DebugBounds, bool MacNonActivating)
{
    public static AppOptions Default { get; } = new(false, false);

    public static AppOptions Parse(string[] args) =>
        new(args.Contains("--debug-bounds"), args.Contains("--mac-nonactivating"));
}
