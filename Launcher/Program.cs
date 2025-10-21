using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AnimeQuoteWallLauncher
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                // Get the launcher directory
                string launcherDir = AppDomain.CurrentDomain.BaseDirectory;
                string guiPath = Path.Combine(launcherDir, "AnimeQuoteWall.GUI");
                string exePath = Path.Combine(guiPath, "bin", "Debug", "net8.0-windows", "AnimeQuoteWall.dll");

                // Check if the GUI project exists
                if (!Directory.Exists(guiPath))
                {
                    MessageBox.Show(
                        "AnimeQuoteWall.GUI folder not found!\n\nPlease make sure the launcher is in the project root folder.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Start the application silently
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run",
                    WorkingDirectory = guiPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,  // No command window!
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to launch Anime Quote Wallpaper Manager:\n\n{ex.Message}",
                    "Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
