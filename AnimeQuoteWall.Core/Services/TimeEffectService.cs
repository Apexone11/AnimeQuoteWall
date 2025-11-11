using System;
using System.Drawing;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for time-based effects (day/night color shifts, time-based animations).
/// </summary>
public class TimeEffectService
{
    /// <summary>
    /// Gets the current time information.
    /// </summary>
    public TimeInfo GetCurrentTime()
    {
        var now = DateTime.Now;
        return new TimeInfo
        {
            Hour = now.Hour,
            Minute = now.Minute,
            Second = now.Second,
            TotalSeconds = now.Hour * 3600 + now.Minute * 60 + now.Second,
            IsDaytime = now.Hour >= 6 && now.Hour < 20,
            IsNighttime = now.Hour >= 20 || now.Hour < 6
        };
    }

    /// <summary>
    /// Calculates a color shift based on time of day.
    /// Returns a color multiplier for day/night effects.
    /// </summary>
    public Color CalculateTimeBasedColorShift()
    {
        var timeInfo = GetCurrentTime();
        
        // Day: brighter, warmer colors
        // Night: darker, cooler colors
        if (timeInfo.IsDaytime)
        {
            // Gradual transition: 6 AM = 0.8, 12 PM = 1.0, 8 PM = 0.8
            var hourProgress = (timeInfo.Hour - 6) / 14f; // 0 to 1 from 6 AM to 8 PM
            var brightness = 0.8f + 0.2f * (float)Math.Sin(hourProgress * Math.PI);
            return Color.FromArgb(
                (int)(255 * brightness),
                (int)(255 * brightness),
                (int)(255 * (brightness * 0.95f)) // Slightly cooler
            );
        }
        else
        {
            // Night: darker, bluer
            var hourProgress = timeInfo.Hour >= 20 
                ? (timeInfo.Hour - 20) / 10f // 8 PM to 6 AM
                : (timeInfo.Hour + 4) / 10f; // Midnight to 6 AM
            var brightness = 0.4f + 0.2f * (float)Math.Sin(hourProgress * Math.PI);
            return Color.FromArgb(
                (int)(255 * brightness * 0.9f), // Red
                (int)(255 * brightness * 0.95f), // Green
                (int)(255 * brightness) // Blue (more blue at night)
            );
        }
    }

    /// <summary>
    /// Calculates opacity based on time (for time-based fade effects).
    /// </summary>
    public float CalculateTimeBasedOpacity()
    {
        var timeInfo = GetCurrentTime();
        
        // Example: fade in during morning, fade out during evening
        if (timeInfo.Hour >= 6 && timeInfo.Hour < 12)
        {
            // Morning: fade in from 0.5 to 1.0
            var progress = (timeInfo.Hour - 6 + timeInfo.Minute / 60f) / 6f;
            return 0.5f + 0.5f * (float)progress;
        }
        else if (timeInfo.Hour >= 12 && timeInfo.Hour < 20)
        {
            // Day: full opacity
            return 1.0f;
        }
        else
        {
            // Evening/Night: fade out from 1.0 to 0.5
            var progress = timeInfo.Hour >= 20 
                ? (timeInfo.Hour - 20 + timeInfo.Minute / 60f) / 10f
                : (timeInfo.Hour + 4 + timeInfo.Minute / 60f) / 10f;
            return 1.0f - 0.5f * (float)Math.Min(progress, 1.0);
        }
    }

    /// <summary>
    /// Gets a time-based animation progress value (0.0 to 1.0) that cycles based on time.
    /// </summary>
    /// <param name="cycleDurationHours">Duration of one cycle in hours</param>
    public float GetTimeBasedAnimationProgress(float cycleDurationHours = 24f)
    {
        var timeInfo = GetCurrentTime();
        var totalHours = timeInfo.Hour + timeInfo.Minute / 60f + timeInfo.Second / 3600f;
        return (totalHours % cycleDurationHours) / cycleDurationHours;
    }
}

/// <summary>
/// Information about the current time.
/// </summary>
public class TimeInfo
{
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Second { get; set; }
    public int TotalSeconds { get; set; }
    public bool IsDaytime { get; set; }
    public bool IsNighttime { get; set; }
}

