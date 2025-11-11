namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// The format to export the animation as
/// </summary>
public enum ExportFormat
{
    Gif,
    Mp4,
}

/// <summary>
/// Text animation types.
/// </summary>
public enum TextAnimationType
{
    None,
    Fade,
    Slide,
    Typewriter
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

    // Text animation type
    public TextAnimationType TextAnimationType { get; set; } = TextAnimationType.None;

    // Image effects to apply
    public ImageEffectSettings ImageEffects { get; set; } = new ImageEffectSettings();

    // Motion effects (parallax, zoom, pan, rotation)
    public List<string> MotionEffects { get; set; } = new List<string>();

    // Particle system settings
    public ParticleSettings ParticleSettings { get; set; } = new ParticleSettings();

    // Interactive effects settings (mouse/time)
    public InteractiveSettings InteractiveSettings { get; set; } = new InteractiveSettings();
}
