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
        // Use async version synchronously for backward compatibility
        return IsWallpaperEngineAvailableAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Checks if Wallpaper Engine is installed and running (async version).
    /// </summary>
    public async Task<bool> IsWallpaperEngineAvailableAsync()
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
                using var response = await httpClient.GetAsync("http://localhost:7070/api/status").ConfigureAwait(false);
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
        // Use async version synchronously for backward compatibility
        return GetWallpaperEngineStatusAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets a detailed status message about Wallpaper Engine availability (async version).
    /// </summary>
    public async Task<string> GetWallpaperEngineStatusAsync()
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
                    using var response = await httpClient.GetAsync("http://localhost:7070/api/status").ConfigureAwait(false);
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
    /// <param name="monitorIndex">Optional monitor index to apply wallpaper to. Null uses settings default.</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SetAnimatedWallpaper(string videoPath, int? monitorIndex = null)
    {
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            return false;

        try
        {
            var extension = Path.GetExtension(videoPath).ToLowerInvariant();
            
            // Try Wallpaper Engine integration first
            if (IsWallpaperEngineAvailable() && (extension == ".mp4" || extension == ".webm" || extension == ".mov"))
            {
                return SetWallpaperEngineWallpaper(videoPath, monitorIndex);
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
    /// <param name="videoPath">Path to the video file</param>
    /// <param name="monitorIndex">Optional monitor index. Null uses settings default (-1 for all monitors)</param>
    private bool SetWallpaperEngineWallpaper(string videoPath, int? monitorIndex = null)
    {
        try
        {
            var fullPath = Path.GetFullPath(videoPath);
            
            // Method 1: Try Wallpaper Engine Web API (localhost:7070)
            if (TryWallpaperEngineWebAPI(fullPath, monitorIndex))
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
    /// <param name="videoPath">Path to the video file</param>
    /// <param name="monitorIndex">Optional monitor index. Null uses settings default (-1 for all monitors)</param>
    private bool TryWallpaperEngineWebAPI(string videoPath, int? monitorIndex = null)
    {
        // Use async version synchronously for backward compatibility
        return TryWallpaperEngineWebAPIAsync(videoPath, monitorIndex).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Tries to use Wallpaper Engine's web API (localhost:7070) - async version.
    /// </summary>
    /// <param name="videoPath">Path to the video file</param>
    /// <param name="monitorIndex">Optional monitor index. Null uses settings default (-1 for all monitors)</param>
    private async Task<bool> TryWallpaperEngineWebAPIAsync(string videoPath, int? monitorIndex = null)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Check if API is available
            using var checkResponse = await httpClient.GetAsync("http://localhost:7070/api/status").ConfigureAwait(false);
            if (!checkResponse.IsSuccessStatusCode)
            {
                return false;
            }

            // Determine monitor parameter
            // If monitorIndex is specified, use it; otherwise use settings default (-1 for all monitors)
            var monitorParam = monitorIndex ?? -1; // -1 means all monitors, specific index for single monitor
            
            // Set wallpaper using API
            var requestBody = new
            {
                file = videoPath,
                monitor = monitorParam
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            using var response = await httpClient.PostAsync("http://localhost:7070/api/setWallpaper", content).ConfigureAwait(false);
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
    /// Clears/removes animated wallpaper from Wallpaper Engine.
    /// This allows static wallpapers to be applied afterward.
    /// </summary>
    /// <param name="monitorIndex">Optional monitor index to clear. Null clears all monitors.</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool ClearAnimatedWallpaper(int? monitorIndex = null)
    {
        try
        {
            if (!IsWallpaperEngineAvailable())
            {
                // If Wallpaper Engine is not available, there's nothing to clear
                return true;
            }

            // Method 1: Try Wallpaper Engine Web API
            if (TryClearWallpaperEngineWebAPI(monitorIndex))
            {
                return true;
            }

            // Method 2: Try command-line interface
            if (TryClearWallpaperEngineCLI(monitorIndex))
            {
                return true;
            }

            // If both methods fail, return false
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing animated wallpaper: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tries to clear wallpaper using Wallpaper Engine Web API.
    /// </summary>
    private bool TryClearWallpaperEngineWebAPI(int? monitorIndex)
    {
        // Use async version synchronously for backward compatibility
        return TryClearWallpaperEngineWebAPIAsync(monitorIndex).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Tries to clear wallpaper using Wallpaper Engine Web API - async version.
    /// </summary>
    private async Task<bool> TryClearWallpaperEngineWebAPIAsync(int? monitorIndex)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Check if API is available
            using var checkResponse = await httpClient.GetAsync("http://localhost:7070/api/status").ConfigureAwait(false);
            if (!checkResponse.IsSuccessStatusCode)
            {
                return false;
            }

            // Determine monitor parameter (-1 for all monitors, specific index for single monitor)
            var monitorParam = monitorIndex ?? -1;

            // Try to clear wallpaper using API
            // Wallpaper Engine API might support clearing via setting empty/null wallpaper
            // or via a dedicated clear endpoint
            var requestBody = new
            {
                monitor = monitorParam,
                file = (string?)null // Setting file to null might clear it
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Try clearWallpaper endpoint first
            try
            {
                using var clearResponse = await httpClient.PostAsync("http://localhost:7070/api/clearWallpaper", content).ConfigureAwait(false);
                if (clearResponse.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Clear endpoint might not exist, try setting empty wallpaper
            }

            // Try setting empty/null wallpaper
            try
            {
                using var setResponse = await httpClient.PostAsync("http://localhost:7070/api/setWallpaper", content).ConfigureAwait(false);
                return setResponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to clear wallpaper using Wallpaper Engine command-line interface.
    /// </summary>
    private bool TryClearWallpaperEngineCLI(int? monitorIndex)
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

            // Try commands to clear wallpaper
            var commands = new[]
            {
                "-control closeWallpaper",
                "-control stopWallpaper",
                "-control clearWallpaper"
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
                        process.WaitForExit(3000);
                        if (process.ExitCode == 0)
                        {
                            return true;
                        }
                    }
                }
                catch
                {
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

