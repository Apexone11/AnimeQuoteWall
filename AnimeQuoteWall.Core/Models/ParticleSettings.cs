using System.Drawing;

namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// Settings for particle effects.
/// </summary>
public class ParticleSettings
{
    /// <summary>
    /// Type of particles to spawn (Snow, Stars, Sparkles, Rain, None).
    /// </summary>
    public string ParticleType { get; set; } = "None";

    /// <summary>
    /// Number of particles to maintain.
    /// </summary>
    public int ParticleCount { get; set; } = 100;

    /// <summary>
    /// Speed of particles (10-200).
    /// </summary>
    public float ParticleSpeed { get; set; } = 50f;

    /// <summary>
    /// Color of particles.
    /// </summary>
    public Color ParticleColor { get; set; } = Color.White;

    /// <summary>
    /// Whether particles are enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;
}

