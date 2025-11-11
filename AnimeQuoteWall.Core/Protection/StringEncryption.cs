using System;
using System.Security.Cryptography;
using System.Text;

namespace AnimeQuoteWall.Core.Protection;

/// <summary>
/// String encryption utility for protecting sensitive strings in compiled code.
/// Used to obfuscate critical strings and prevent easy reverse engineering.
/// </summary>
internal static class StringEncryption
{
    // Encryption key (XOR-based simple encryption for obfuscation)
    // In production, consider using more sophisticated encryption
    private static readonly byte[] _key = Encoding.UTF8.GetBytes("AQW2024Protect");

    /// <summary>
    /// Decrypts an encrypted string at runtime.
    /// </summary>
    /// <param name="encrypted">Encrypted string data</param>
    /// <returns>Decrypted string</returns>
    public static string Decrypt(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
            return string.Empty;

        try
        {
            var data = Convert.FromBase64String(encrypted);
            var decrypted = new byte[data.Length];
            
            for (int i = 0; i < data.Length; i++)
            {
                decrypted[i] = (byte)(data[i] ^ _key[i % _key.Length]);
            }
            
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            // Return empty string on decryption failure (prevents crashes)
            return string.Empty;
        }
    }

    /// <summary>
    /// Encrypts a string for embedding in code.
    /// Use this tool to generate encrypted strings for critical values.
    /// </summary>
    /// <param name="plainText">Plain text to encrypt</param>
    /// <returns>Encrypted string (Base64)</returns>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var data = Encoding.UTF8.GetBytes(plainText);
        var encrypted = new byte[data.Length];
        
        for (int i = 0; i < data.Length; i++)
        {
            encrypted[i] = (byte)(data[i] ^ _key[i % _key.Length]);
        }
        
        return Convert.ToBase64String(encrypted);
    }
}

/// <summary>
/// Obfuscation helper for protecting method names and logic.
/// </summary>
internal static class ObfuscationHelper
{
    /// <summary>
    /// Validates integrity of critical code sections.
    /// </summary>
    public static bool ValidateIntegrity()
    {
        try
        {
            // Simple integrity check - verify critical methods exist
            var type = Type.GetType("AnimeQuoteWall.Core.Services.WallpaperService");
            if (type == null) return false;
            
            var method = type.GetMethod("CreateWallpaperImage", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            return method != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Anti-tampering check for critical assemblies.
    /// </summary>
    public static bool CheckAntiTampering()
    {
        try
        {
            // Check if assembly has been modified
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var location = assembly.Location;
            
            if (string.IsNullOrEmpty(location))
                return true; // Allow in-memory assemblies (for testing)
            
            var fileInfo = new System.IO.FileInfo(location);
            var lastWrite = fileInfo.LastWriteTime;
            var now = DateTime.Now;
            
            // Basic check: file shouldn't be modified after compilation
            // (This is a simple check - for production, use code signing)
            return true; // Always pass for now - implement stronger checks for Steam release
        }
        catch
        {
            return true; // Fail open to prevent false positives
        }
    }
}

