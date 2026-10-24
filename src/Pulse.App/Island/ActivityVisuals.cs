using Avalonia.Media;
using Pulse.Core.Activities;

namespace Pulse.App.Island;

/// <summary>How an activity looks in the island: colors, icons and the short texts. Pure, so it is unit-tested.</summary>
internal static class ActivityVisuals
{
    /// <summary>Apple's system colors, which read well on the black pill.</summary>
    public static Color Glow(ActivityState state) => state switch
    {
        ActivityState.Running => Color.FromRgb(0x0A, 0x84, 0xFF),
        ActivityState.NeedsInput => Color.FromRgb(0xFF, 0x9F, 0x0A),
        ActivityState.Success => Color.FromRgb(0x30, 0xD1, 0x58),
        ActivityState.Failure => Color.FromRgb(0xFF, 0x45, 0x3A),
        _ => Color.FromRgb(0x98, 0x98, 0x9D),
    };

    /// <summary>
    /// Stroke path in a 24×24 box. Every icon is static: a spinning one would keep a render loop running for as
    /// long as a job runs (CLAUDE.md rule 7).
    /// </summary>
    public static string IconPath(ActivityState state) => state switch
    {
        ActivityState.Running => "M12,3 A9,9 0 1 1 3,12",
        ActivityState.NeedsInput => "M12,5 L12,13.5 M12,18.5 L12,18.6",
        ActivityState.Success => "M5,12.5 L10,17.5 L19,7",
        ActivityState.Failure => "M6.5,6.5 L17.5,17.5 M17.5,6.5 L6.5,17.5",
        _ => "M12,11 L12,18 M12,6.5 L12,6.6",
    };

    /// <summary>Progress of an unfinished activity, e.g. "40%"; null when there is nothing to measure.</summary>
    public static string? ProgressText(Activity activity) =>
        activity is { IsFinished: false, Progress: { } progress } ? $"{progress}%" : null;

    /// <summary>Right of the notch there is room for a few characters: the progress, or else the start of the title.</summary>
    public static string NotchTrailing(Activity activity) => ProgressText(activity) ?? activity.Title;

    /// <summary>
    /// The rows the expanded island lists, and how many are left out. Past <see cref="IslandLayout.MaxRows"/>, the last
    /// row's place goes to a "+N more" line.
    /// </summary>
    public static (IReadOnlyList<Activity> Rows, int Hidden) SplitRows(IReadOnlyList<Activity> activities)
    {
        if (activities.Count <= IslandLayout.MaxRows)
            return (activities, 0);

        const int shown = IslandLayout.MaxRows - 1;
        return (activities.Take(shown).ToList(), activities.Count - shown);
    }
}
