namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// Settings for interactive effects (mouse and clock).
/// </summary>
public class InteractiveSettings
{
    /// <summary>
    /// Whether mouse tracking (parallax) is enabled.
    /// </summary>
    public bool MouseTrackingEnabled { get; set; } = false;

    /// <summary>
    /// Parallax intensity (0.0 to 1.0).
    /// </summary>
    public float ParallaxIntensity { get; set; } = 0.5f;

    /// <summary>
    /// Whether clock effects are enabled.
    /// </summary>
    public bool ClockEffectsEnabled { get; set; } = false;

    /// <summary>
    /// Whether time-based color shift is enabled.
    /// </summary>
    public bool TimeColorShiftEnabled { get; set; } = true;

    /// <summary>
    /// Whether time-based opacity is enabled.
    /// </summary>
    public bool TimeOpacityEnabled { get; set; } = false;
}

