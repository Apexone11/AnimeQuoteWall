using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AnimeQuoteWall.Core.Protection;

/// <summary>
/// Code protection utilities for protecting critical algorithms and business logic.
/// Provides anti-tampering, integrity checks, and obfuscation support.
/// </summary>
public static class CodeProtection
{
    private static bool _integrityChecked = false;
    private static bool _isValid = false;

    /// <summary>
    /// Initializes protection checks. Call this early in application startup.
    /// </summary>
    public static void Initialize()
    {
        if (_integrityChecked)
            return;

        try
        {
            // Perform integrity checks
            _isValid = ValidateCodeIntegrity();
            _integrityChecked = true;

            if (!_isValid)
            {
                // Log warning but don't crash (allows debugging)
                Debug.WriteLine("Warning: Code integrity check failed. Application may have been tampered with.");
            }
        }
        catch
        {
            // Fail silently to prevent crashes during development
            _isValid = true;
            _integrityChecked = true;
        }
    }

    /// <summary>
    /// Validates that critical code sections are intact.
    /// </summary>
    private static bool ValidateCodeIntegrity()
    {
        try
        {
            // Check critical types exist
            var criticalTypes = new[]
            {
                "AnimeQuoteWall.Core.Services.WallpaperService",
                "AnimeQuoteWall.Core.Services.PlaylistService",
                "AnimeQuoteWall.Core.Services.ScheduleService",
                "AnimeQuoteWall.Core.Models.Playlist"
            };

            foreach (var typeName in criticalTypes)
            {
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    Debug.WriteLine($"Critical type not found: {typeName}");
                    return false;
                }
            }

            // Check critical methods exist
            var wallpaperServiceType = Type.GetType("AnimeQuoteWall.Core.Services.WallpaperService");
            if (wallpaperServiceType != null)
            {
                var createMethod = wallpaperServiceType.GetMethod("CreateWallpaperImage",
                    BindingFlags.Public | BindingFlags.Instance);
                if (createMethod == null)
                    return false;

                var drawMethod = wallpaperServiceType.GetMethod("DrawAnimatedQuote",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (drawMethod == null)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a protected value (for sensitive configuration).
    /// </summary>
    public static string GetProtectedValue(string key)
    {
        // In production, these would be encrypted
        // For now, return obfuscated values
        return key switch
        {
            "AppName" => StringEncryption.Decrypt("AQW2024Protect"),
            "Version" => "1.2.0",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Validates code integrity for distribution.
    /// </summary>
    public static bool ValidateDistributionIntegrity()
    {
        try
        {
            // Perform integrity checks
            return ValidateCodeIntegrity();
        }
        catch
        {
            return true; // Fail open
        }
    }

    /// <summary>
    /// Obfuscates a method name for protection.
    /// </summary>
    public static string ObfuscateMethodName(string methodName)
    {
        // Simple obfuscation - in production, use proper obfuscation tools
        var bytes = Encoding.UTF8.GetBytes(methodName);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash).Substring(0, 16);
    }
}

