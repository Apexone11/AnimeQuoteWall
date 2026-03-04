using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for tracking mouse position and calculating parallax effects.
/// </summary>
public class MouseTrackingService
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// Gets the current mouse position in screen coordinates.
    /// </summary>
    public Point GetMousePosition()
    {
        GetCursorPos(out POINT point);
        return new Point(point.X, point.Y);
    }

    /// <summary>
    /// Calculates parallax offset based on mouse position relative to screen center.
    /// </summary>
    /// <param name="screenWidth">Width of the screen</param>
    /// <param name="screenHeight">Height of the screen</param>
    /// <param name="intensity">Parallax intensity (0.0 to 1.0)</param>
    /// <returns>Parallax offset as a PointF</returns>
    public PointF CalculateParallaxOffset(int screenWidth, int screenHeight, float intensity = 0.5f)
    {
        // Validate screen dimensions to prevent division by zero
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return new PointF(0, 0);
        }
        
        var mousePos = GetMousePosition();
        var centerX = screenWidth / 2f;
        var centerY = screenHeight / 2f;

        // Calculate offset from center (-1.0 to 1.0)
        var offsetX = (mousePos.X - centerX) / centerX;
        var offsetY = (mousePos.Y - centerY) / centerY;

        // Apply intensity and scale
        var maxOffset = 50f * intensity; // Maximum pixel offset
        return new PointF(offsetX * maxOffset, offsetY * maxOffset);
    }

    /// <summary>
    /// Gets normalized mouse position (0.0 to 1.0) relative to screen.
    /// </summary>
    public PointF GetNormalizedMousePosition(int screenWidth, int screenHeight)
    {
        var mousePos = GetMousePosition();
        return new PointF(
            Math.Clamp(mousePos.X / (float)screenWidth, 0f, 1f),
            Math.Clamp(mousePos.Y / (float)screenHeight, 0f, 1f)
        );
    }
}

