using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ImageMagick;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for applying animated wallpapers (GIF/MP4) using Wallpaper Engine or fallback methods.
/// </summary>
public class AnimatedWallpaperService
{
    /// <summary>
    /// Checks if Wallpaper Engine is installed and running.
    /// </summary>
    public bool IsWallpaperEngineAvailable()
    {
        try
        {
            // First check if Wallpaper Engine process is running (most reliable)
            var processes = Process.GetProcessesByName("wallpaper32");
            if (processes.Length > 0)
                return true;

            processes = Process.GetProcessesByName("wallpaper64");
            if (processes.Length > 0)
                return true;

            // Check if Wallpaper Engine Web API is accessible (indicates it's running)
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(2);
                var response = httpClient.GetAsync("http://localhost:7070/api/status").Result;
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                // API not available
            }

            // Check if Wallpaper Engine is installed in common locations
            var steamPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? Environment.GetEnvironmentVariable("ProgramFiles");
            var wallpaperEnginePath = Path.Combine(steamPath ?? "", "Steam", "steamapps", "common", "wallpaper_engine");
            
            if (Directory.Exists(wallpaperEnginePath))
                return true;

            // Also check alternative Steam installation paths
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var altSteamPath = Path.Combine(userProfile, "Steam", "steamapps", "common", "wallpaper_engine");
            if (Directory.Exists(altSteamPath))
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a detailed status message about Wallpaper Engine availability.
    /// </summary>
    public string GetWallpaperEngineStatus()
    {
        try
        {
            // Check if process is running
            var processes32 = Process.GetProcessesByName("wallpaper32");
            var processes64 = Process.GetProcessesByName("wallpaper64");
            
            if (processes32.Length > 0 || processes64.Length > 0)
            {
                // Check if API is accessible
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(2);
                    var response = httpClient.GetAsync("http://localhost:7070/api/status").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return "Wallpaper Engine is running and API is accessible.";
                    }
                }
                catch
                {
                    return "Wallpaper Engine process is running but API is not accessible. Try restarting Wallpaper Engine.";
                }
            }

            // Check if installed but not running
            var steamPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? Environment.GetEnvironmentVariable("ProgramFiles");
            var wallpaperEnginePath = Path.Combine(steamPath ?? "", "Steam", "steamapps", "common", "wallpaper_engine");
            
            if (Directory.Exists(wallpaperEnginePath))
            {
                return "Wallpaper Engine is installed but not running. Please start Wallpaper Engine from Steam.";
            }

            return "Wallpaper Engine is not installed. Please install it from Steam to use animated wallpapers.";
        }
        catch
        {
            return "Unable to check Wallpaper Engine status.";
        }
    }

    /// <summary>
    /// Sets an animated wallpaper using Wallpaper Engine API or fallback method.
    /// </summary>
    /// <param name="videoPath">Path to the animated wallpaper file (GIF/MP4)</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SetAnimatedWallpaper(string videoPath)
    {
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            return false;

        try
        {
            var extension = Path.GetExtension(videoPath).ToLowerInvariant();
            
            // Try Wallpaper Engine integration first
            if (IsWallpaperEngineAvailable() && (extension == ".mp4" || extension == ".webm" || extension == ".mov"))
            {
                return SetWallpaperEngineWallpaper(videoPath);
            }

            // Fallback: Extract first frame for GIFs or use static frame extraction
            if (extension == ".gif")
            {
                // For GIFs, extract first frame and set as static wallpaper
                return ExtractAndSetGifFrame(videoPath);
            }

            // For videos without Wallpaper Engine, extract first frame
            return ExtractAndSetVideoFrame(videoPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting animated wallpaper: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets wallpaper using Wallpaper Engine API.
    /// Wallpaper Engine provides a web API on localhost:7070.
    /// </summary>
    private bool SetWallpaperEngineWallpaper(string videoPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(videoPath);
            
            // Method 1: Try Wallpaper Engine Web API (localhost:7070)
            if (TryWallpaperEngineWebAPI(fullPath))
            {
                return true;
            }

            // Method 2: Try command-line interface
            if (TryWallpaperEngineCLI(fullPath))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Wallpaper Engine API error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tries to use Wallpaper Engine's web API (localhost:7070).
    /// </summary>
    private bool TryWallpaperEngineWebAPI(string videoPath)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Check if API is available
            var checkResponse = httpClient.GetAsync("http://localhost:7070/api/status").Result;
            if (!checkResponse.IsSuccessStatusCode)
            {
                return false;
            }

            // Set wallpaper using API
            var requestBody = new
            {
                file = videoPath,
                monitor = -1 // -1 means all monitors
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = httpClient.PostAsync("http://localhost:7070/api/setWallpaper", content).Result;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to use Wallpaper Engine's command-line interface.
    /// </summary>
    private bool TryWallpaperEngineCLI(string videoPath)
    {
        try
        {
            var steamPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? Environment.GetEnvironmentVariable("ProgramFiles");
            var wallpaperEnginePath = Path.Combine(steamPath ?? "", "Steam", "steamapps", "common", "wallpaper_engine");
            
            var exePath = Path.Combine(wallpaperEnginePath, "wallpaper64.exe");
            if (!File.Exists(exePath))
            {
                exePath = Path.Combine(wallpaperEnginePath, "wallpaper32.exe");
            }

            if (!File.Exists(exePath))
            {
                return false;
            }

            // Try different command formats that Wallpaper Engine might support
            var commands = new[]
            {
                $"-control openWallpaper -file \"{videoPath}\"",
                $"-control applyWallpaper -file \"{videoPath}\"",
                $"-file \"{videoPath}\""
            };

            foreach (var args in commands)
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                try
                {
                    var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        process.WaitForExit(3000); // Wait up to 3 seconds
                        if (process.ExitCode == 0)
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // Try next command format
                    continue;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts first frame from GIF and sets as static wallpaper.
    /// </summary>
    private bool ExtractAndSetGifFrame(string gifPath)
    {
        try
        {
            // Use ImageMagick to extract first frame
            // This is a fallback when Wallpaper Engine is not available
            using var image = new ImageMagick.MagickImage(gifPath);
            image.Write(gifPath.Replace(".gif", "_frame0.png"));
            
            var framePath = gifPath.Replace(".gif", "_frame0.png");
            if (File.Exists(framePath))
            {
                var result = WallpaperSettingHelper.SetWallpaper(framePath);
                // Clean up temporary frame file after a delay
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    try { File.Delete(framePath); } catch { }
                });
                return result;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts first frame from video and sets as static wallpaper.
    /// </summary>
    private bool ExtractAndSetVideoFrame(string videoPath)
    {
        try
        {
            // Use FFmpeg to extract first frame
            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ffmpeg", "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                // Try to find FFmpeg in PATH
                ffmpegPath = "ffmpeg";
            }

            var framePath = videoPath.Replace(Path.GetExtension(videoPath), "_frame0.png");
            
            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{videoPath}\" -vframes 1 -y \"{framePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = Process.Start(processInfo);
            process?.WaitForExit(10000); // Wait up to 10 seconds

            if (File.Exists(framePath))
            {
                var result = WallpaperSettingHelper.SetWallpaper(framePath);
                // Clean up temporary frame file after a delay
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    try { File.Delete(framePath); } catch { }
                });
                return result;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

