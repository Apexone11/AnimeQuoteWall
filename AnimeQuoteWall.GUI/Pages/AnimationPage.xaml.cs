using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;
using AnimeQuoteWall.GUI.Services;
using Microsoft.Win32;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// Page for generating and exporting animated wallpapers (GIF/MP4).
/// 
/// Features:
/// - Generate animation frames with customizable settings
/// - Export animations to GIF format (using ImageMagick)
/// - Export animations to MP4 format (using FFmpeg)
/// - Non-blocking generation (allows navigation while generating)
/// - Progress tracking and cancellation support
/// </summary>
public partial class AnimationPage : Page
{
    /// <summary>
    /// Service for managing quotes.
    /// </summary>
    private readonly IQuoteService _quoteService;
    
    /// <summary>
    /// Service for managing background images.
    /// </summary>
    private readonly IBackgroundService _backgroundService;
    
    /// <summary>
    /// Service for generating animation frames and exporting animations.
    /// </summary>
    private readonly AnimationService _animationService;
    
    /// <summary>
    /// Background task manager for non-blocking animation generation.
    /// </summary>
    private readonly BackgroundTaskManager _taskManager;
    
    /// <summary>
    /// List of loaded quotes.
    /// </summary>
    private List<Quote> _quotes = new();
    
    /// <summary>
    /// List of generated animation frame file paths.
    /// </summary>
    private IReadOnlyList<string>? _generatedFrames;
    
    /// <summary>
    /// Cancellation token source for canceling animation generation.
    /// </summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the AnimationPage.
    /// </summary>
    public AnimationPage()
    {
        try
        {
            InitializeComponent();
            _quoteService = new QuoteService();
            _backgroundService = new BackgroundService();
            _animationService = new AnimationService();
            _taskManager = BackgroundTaskManager.Instance;
            
            Loaded += async (s, e) => await InitializeAsync();
            Unloaded += (s, e) => OnPageUnloaded();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to initialize AnimationPage: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Initializes the animation page by loading quotes and setting up UI.
    /// Subscribes to background task status updates.
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            // Ensure quotes file exists and load quotes
            await _quoteService.EnsureQuotesFileAsync(AppConfiguration.QuotesFilePath);
            _quotes = await _quoteService.LoadQuotesAsync(AppConfiguration.QuotesFilePath);
            
            // Load default animation settings from config
            if (FpsTextBox != null)
                FpsTextBox.Text = AppConfiguration.AnimationFps.ToString();
            if (DurationTextBox != null)
                DurationTextBox.Text = AppConfiguration.AnimationDurationSec.ToString();
            
            // Subscribe to task status updates for background generation
            if (_taskManager != null)
            {
                _taskManager.TaskStatusChanged += TaskManager_TaskStatusChanged;
                
                // Check if there's an ongoing task (e.g., user navigated away and back)
                CheckBackgroundTaskStatus();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            System.Diagnostics.Debug.WriteLine($"AnimationPage InitializeAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles page unloaded event.
    /// Unsubscribes from task status updates to prevent memory leaks.
    /// </summary>
    private void OnPageUnloaded()
    {
        try
        {
            // Unsubscribe when page is unloaded
            if (_taskManager != null)
            {
                _taskManager.TaskStatusChanged -= TaskManager_TaskStatusChanged;
            }
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Checks the status of any ongoing background animation generation task.
    /// Restores UI state if a task is running or completed.
    /// This is called when the page is loaded to handle cases where the user
    /// navigated away and back while generation was in progress.
    /// </summary>
    private void CheckBackgroundTaskStatus()
    {
        try
        {
            if (_taskManager == null) return;
            
            var task = _taskManager.CurrentAnimationTask;
            if (task != null && task.IsRunning)
            {
                // Restore UI state for ongoing generation
                if (GenerateAnimationButton != null)
                    GenerateAnimationButton.IsEnabled = false;
                if (ExportButton != null)
                    ExportButton.IsEnabled = false;
                if (ProgressBorder != null)
                    ProgressBorder.Visibility = Visibility.Visible;
                if (ProgressBar != null)
                    ProgressBar.Value = task.Progress * 100;
                if (ProgressText != null)
                    ProgressText.Text = task.StatusMessage;
                if (CancelButton != null)
                {
                    CancelButton.IsEnabled = true;
                    CancelButton.Visibility = Visibility.Visible;
                }
                
                // Restore cancellation token source reference
                _cancellationTokenSource = task.CancellationTokenSource;
            }
            else if (task != null && !task.IsRunning && task.Result != null)
            {
                // Generation completed while user was away
                _generatedFrames = task.Result;
                if (ProgressBorder != null)
                    ProgressBorder.Visibility = Visibility.Visible;
                if (ProgressBar != null)
                    ProgressBar.Value = 100;
                if (ProgressText != null)
                    ProgressText.Text = "Animation frames generated!";
                if (ExportButton != null)
                    ExportButton.IsEnabled = true;
                if (GenerateAnimationButton != null)
                    GenerateAnimationButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CheckBackgroundTaskStatus error: {ex.Message}");
        }
    }

    private void TaskManager_TaskStatusChanged(object? sender, BackgroundTaskStatus status)
    {
        try
        {
            // Update UI on the UI thread
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (status.IsRunning)
                    {
                        if (ProgressBar != null)
                            ProgressBar.Value = status.Progress * 100;
                        if (ProgressText != null)
                            ProgressText.Text = status.StatusMessage;
                        if (ProgressBorder != null)
                            ProgressBorder.Visibility = Visibility.Visible;
                        if (GenerateAnimationButton != null)
                            GenerateAnimationButton.IsEnabled = false;
                        if (ExportButton != null)
                            ExportButton.IsEnabled = false;
                        if (CancelButton != null)
                        {
                            CancelButton.IsEnabled = true;
                            CancelButton.Visibility = Visibility.Visible;
                        }
                    }
                    else if (status.Result != null)
                    {
                        // Generation completed
                        _generatedFrames = status.Result;
                        if (ProgressBar != null)
                            ProgressBar.Value = 100;
                        if (ProgressText != null)
                            ProgressText.Text = "Animation frames generated!";
                        if (ExportButton != null)
                            ExportButton.IsEnabled = true;
                        if (GenerateAnimationButton != null)
                            GenerateAnimationButton.IsEnabled = true;
                        if (CancelButton != null)
                        {
                            CancelButton.IsEnabled = false;
                            CancelButton.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        // Cancelled or error
                        if (GenerateAnimationButton != null)
                            GenerateAnimationButton.IsEnabled = true;
                        if (CancelButton != null)
                        {
                            CancelButton.IsEnabled = false;
                            CancelButton.Visibility = Visibility.Collapsed;
                        }
                        if (ProgressText != null && status.StatusMessage.Contains("cancelled", StringComparison.OrdinalIgnoreCase))
                        {
                            ProgressText.Text = "Generation cancelled.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"TaskManager_TaskStatusChanged UI update error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TaskManager_TaskStatusChanged error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the Generate Animation button click event.
    /// Starts non-blocking animation frame generation in the background.
    /// 
    /// Process:
    /// 1. Validate quotes and backgrounds are available
    /// 2. Validate FPS and duration inputs
    /// 3. Create wallpaper settings and animation profile
    /// 4. Start frame generation task (non-blocking)
    /// 5. Register task with BackgroundTaskManager for navigation support
    /// 
    /// </summary>
    private async void GenerateAnimationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate quotes are available
            if (_quotes.Count == 0)
            {
                System.Windows.MessageBox.Show("No quotes available. Please add quotes first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate backgrounds are available
            var backgrounds = _backgroundService.GetAllBackgroundImages(AppConfiguration.BackgroundsDirectory);
            if (backgrounds.Count == 0)
            {
                System.Windows.MessageBox.Show("No background images available. Please add backgrounds first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate FPS input (1-60)
            if (!int.TryParse(FpsTextBox.Text, out int fps) || fps < 1 || fps > 60)
            {
                System.Windows.MessageBox.Show("FPS must be between 1 and 60.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate duration input (1-30 seconds)
            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration < 1 || duration > 30)
            {
                System.Windows.MessageBox.Show("Duration must be between 1 and 30 seconds.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if there's already a task running
            if (_taskManager.IsAnimationGenerating)
            {
                System.Windows.MessageBox.Show("Animation generation is already in progress. You can navigate to other pages and return here to check progress.", "Already Generating", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Update UI to show generation in progress
            GenerateAnimationButton.IsEnabled = false;
            ExportButton.IsEnabled = false;
            ProgressBorder.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;

            // Create cancellation token for canceling generation
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Select random quote and background
            var randomQuote = _quoteService.GetRandomQuote(_quotes);
            var randomBackground = _backgroundService.GetRandomBackgroundImage(AppConfiguration.BackgroundsDirectory);

            // Get screen dimensions
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            // Create wallpaper settings
            var settings = new WallpaperSettings
            {
                Width = screenWidth,
                Height = screenHeight,
                BackgroundColor = "#141414",
                FontFamily = "Segoe UI",
                TextColor = "#FFFFFF",
                OutlineColor = "#000000",
                PanelColor = "#1A1A1A",
                PanelOpacity = 0.85f,
                MaxPanelWidthPercent = 0.7f,
                FontSizeFactor = 25f,
                MinFontSize = 32
            };

            // Create animation profile from UI settings
            var profile = new AnimationProfile
            {
                FramesPerSecond = fps,
                DurationSeconds = duration,
                MotionType = (MotionTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "fade",
                EasingType = GetEasingType(),
                Loop = LoopCheckBox.IsChecked ?? true
            };

            // Save settings to config for next time
            AppConfiguration.AnimationFps = fps;
            AppConfiguration.AnimationDurationSec = duration;

            // Create temporary directory for frames
            var tempDir = Path.Combine(Path.GetTempPath(), "AnimeQuoteAnimation_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            // Create progress callback that updates both UI and task manager
            var progress = new Progress<double>(p =>
            {
                var percent = Math.Round(p * 100);
                ProgressBar.Value = percent;
                ProgressText.Text = $"Generating frames... {percent}%";
                _taskManager.UpdateProgress(p, $"Generating frames... {percent}%");
            });

            // Start the generation task (don't await - let it run in background)
            var generationTask = _animationService.GenerateFramesAsync(
                randomBackground,
                randomQuote,
                settings,
                profile,
                tempDir,
                progress,
                token);

            // Register with task manager so it can continue even if user navigates away
            _taskManager.StartAnimationGeneration(generationTask, _cancellationTokenSource);

            // Continue in background - don't block navigation
            _ = ContinueGenerationAsync(generationTask);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            GenerateAnimationButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Continues animation generation after the task starts.
    /// Handles completion, cancellation, and errors.
    /// Updates UI when generation completes.
    /// </summary>
    private async Task ContinueGenerationAsync(Task<IReadOnlyList<string>> generationTask)
    {
        try
        {
            // Wait for generation to complete
            _generatedFrames = await generationTask;

            // Update UI if page is still loaded
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = "Animation frames generated!";
                ExportButton.IsEnabled = true;
                GenerateAnimationButton.IsEnabled = true;
            });

            // Show notification (non-blocking)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Windows.MessageBox.Show($"Generated {_generatedFrames.Count} frames successfully! You can now export your animation.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = "Generation cancelled.";
                GenerateAnimationButton.IsEnabled = true;
            });
        }
        catch (Exception ex)
        {
            // Handle errors
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Windows.MessageBox.Show($"Failed to generate animation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                GenerateAnimationButton.IsEnabled = true;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// Handles the Export button click event.
    /// Exports generated animation frames to GIF or MP4 format.
    /// 
    /// Process:
    /// 1. Validate frames are available
    /// 2. Show save file dialog (GIF or MP4 based on selection)
    /// 3. Export frames using AnimationService (GIF via ImageMagick, MP4 via FFmpeg)
    /// 4. Show success/error message
    /// 
    /// </summary>
    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate frames are available
        if (_generatedFrames == null || _generatedFrames.Count == 0)
        {
            System.Windows.MessageBox.Show("No frames to export. Generate animation first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            // Determine export format (GIF or MP4)
            var isGif = GifRadioButton.IsChecked ?? true;
            var filter = isGif ? "GIF Files (*.gif)|*.gif" : "MP4 Files (*.mp4)|*.mp4";
            var defaultExt = isGif ? ".gif" : ".mp4";
            var defaultName = isGif ? "animation.gif" : "animation.mp4";

            // Show save file dialog
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Animation",
                Filter = filter,
                FileName = defaultName,
                DefaultExt = defaultExt
            };

            if (dialog.ShowDialog() != true)
                return;

            // Update UI to show export in progress
            ExportButton.IsEnabled = false;
            ProgressBorder.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;

            // Create cancellation token
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Create animation profile from UI settings
            var profile = new AnimationProfile
            {
                FramesPerSecond = int.TryParse(FpsTextBox.Text, out int fps) ? fps : 24,
                DurationSeconds = int.TryParse(DurationTextBox.Text, out int dur) ? dur : 6,
                MotionType = (MotionTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "fade",
                EasingType = GetEasingType(),
                Loop = LoopCheckBox.IsChecked ?? true
            };

            // Create progress callback
            var progress = new Progress<double>(p =>
            {
                ProgressBar.Value = p * 100;
                ProgressText.Text = isGif ? $"Exporting GIF... {Math.Round(p * 100)}%" : $"Exporting MP4... {Math.Round(p * 100)}%";
            });

            try
            {
                if (isGif)
                {
                    // Export to GIF using ImageMagick
                    await _animationService.ExportGifAsync(_generatedFrames, dialog.FileName, profile, progress, token);
                }
                else
                {
                    // Export to MP4 using FFmpeg
                    var ffmpegPath = AppConfiguration.FfmpegPath;
                    if (string.IsNullOrWhiteSpace(ffmpegPath))
                    {
                        // Try default location
                        var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ffmpeg", "ffmpeg.exe");
                        if (File.Exists(defaultPath))
                        {
                            ffmpegPath = defaultPath;
                            AppConfiguration.FfmpegPath = ffmpegPath;
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("FFmpeg not found. Please ensure ffmpeg.exe is in Resources/ffmpeg/ folder.", "FFmpeg Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    await _animationService.ExportMp4Async(_generatedFrames, dialog.FileName, profile, ffmpegPath, progress, token);
                }

                // Show success message
                ProgressText.Text = "Export complete!";
                AppConfiguration.LastExportDirectory = Path.GetDirectoryName(dialog.FileName);
                System.Windows.MessageBox.Show($"Animation exported successfully to:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                ProgressText.Text = "Export cancelled.";
                System.Windows.MessageBox.Show("Export was cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Handle errors
                System.Windows.MessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExportButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Cancel button click event.
    /// Cancels the ongoing animation generation task.
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _taskManager.CancelAnimationGeneration();
        CancelButton.IsEnabled = false;
        CancelButton.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Gets the easing type from the UI combo box.
    /// Converts display names to internal easing type strings.
    /// </summary>
    /// <returns>Easing type string ("easeIn", "easeOut", or "linear")</returns>
    private string GetEasingType()
    {
        var selected = (EasingTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Linear";
        return selected switch
        {
            "Ease In" => "easeIn",
            "Ease Out" => "easeOut",
            _ => "linear"
        };
    }
}

