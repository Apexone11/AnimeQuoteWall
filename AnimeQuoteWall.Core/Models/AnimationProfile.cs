namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// The format to export the animation as
/// </summary>
public enum ExportFormat
{
    Gif,
    Mp4,
}

public class AnimationProfile
{

    // The number of frames per second to use for the animation
    public int FramesPerSecond { get; set; } = 24;

    // The duration of the animation in seconds
    public int DurationSeconds { get; set; } = 6;

    // The motion type to use for the animation
    public string MotionType { get; set; } = "fade";

    // The easing type to use for the animation
    public string EasingType { get; set; } = "linear";

    // Whether to loop the animation
    public bool Loop { get; set; } = true;
}
