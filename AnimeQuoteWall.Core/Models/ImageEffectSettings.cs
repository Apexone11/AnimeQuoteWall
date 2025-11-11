namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// Settings for image effects (filters).
/// </summary>
public class ImageEffectSettings
{
    /// <summary>
    /// Type of filter to apply (Blur, Glow, Sepia, Grayscale, Vintage, Brightness, Contrast, None).
    /// </summary>
    public string FilterType { get; set; } = "None";

    /// <summary>
    /// Intensity of the filter (0.1 to 3.0).
    /// </summary>
    public float FilterIntensity { get; set; } = 1.0f;

    /// <summary>
    /// Whether the filter is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;
}

