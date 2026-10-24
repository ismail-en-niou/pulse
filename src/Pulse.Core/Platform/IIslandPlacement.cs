namespace Pulse.Core.Platform;

/// <summary>A point in island-window coordinates (DIPs; points on macOS).</summary>
public readonly record struct IslandPoint(double X, double Y);

/// <summary>A rectangle in island-window coordinates (DIPs; points on macOS).</summary>
public readonly record struct IslandRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;

    public bool Contains(IslandPoint point) =>
        point.X >= X && point.X <= Right && point.Y >= Y && point.Y <= Bottom;

    public IslandRect Inflate(double amount) =>
        new(X - amount, Y - amount, Width + 2 * amount, Height + 2 * amount);
}

/// <summary>Size of the primary screen's camera notch, in points.</summary>
public readonly record struct NotchInfo(double Width, double Height);

/// <summary>
/// How the island window sits on screen: window level and flags, notch detection, and letting clicks around
/// the pill through to the windows behind. Implementations get the window through their constructor.
/// </summary>
public interface IIslandPlacement : IDisposable
{
    /// <summary>How clicks around the pill reach the windows behind it, for diagnostics.</summary>
    string ClickThroughMode { get; }

    /// <summary>Cursor position in window coordinates, or null when the cursor has left the window.</summary>
    event Action<IslandPoint?>? PointerMoved;

    /// <summary>The primary screen's notch, or null when it has none.</summary>
    NotchInfo? DetectNotch();

    /// <summary>Called once the window is on screen.</summary>
    void OnOpened();

    void SetPointerInside(bool inside);

    void OnAnimationStarted();

    /// <summary>The pill stopped moving; <paramref name="pill"/> is where it now is.</summary>
    void OnSettled(IslandRect pill, double cornerRadius);
}
