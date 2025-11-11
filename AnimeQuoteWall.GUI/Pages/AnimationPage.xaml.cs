using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
    /// Service for editing images and videos.
    /// </summary>
    private readonly MediaEditingService _mediaEditingService;
    
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
    /// Timer for animating the preview by cycling through frames.
    /// </summary>
    private DispatcherTimer? _previewAnimationTimer;
    
    /// <summary>
    /// Current frame index for the preview animation.
    /// </summary>
    private int _currentPreviewFrameIndex = 0;

    /// <summary>
    /// Currently loaded media file path (image or video).
    /// </summary>
    private string? _loadedMediaPath;

    /// <summary>
    /// Currently edited bitmap (for preview and further editing).
    /// </summary>
    private System.Drawing.Bitmap? _currentEditedBitmap;

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
            _mediaEditingService = new MediaEditingService();
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
    /// Stops the preview animation timer.
    /// </summary>
    private void OnPageUnloaded()
    {
        try
        {
            // Stop preview animation timer
            StopPreviewAnimation();
            
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
                // Show preview
                UpdateAnimationPreview();
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
                        // Show preview
                        UpdateAnimationPreview();
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
            var motionType = (MotionTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "fade";
            var textAnimationTypeStr = (TextAnimationTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
            var textAnimationType = textAnimationTypeStr switch
            {
                "Fade" => Core.Models.TextAnimationType.Fade,
                "Slide" => Core.Models.TextAnimationType.Slide,
                "Typewriter" => Core.Models.TextAnimationType.Typewriter,
                _ => Core.Models.TextAnimationType.None
            };

            var motionEffects = new List<string>();
            if (motionType != "Fade" && motionType != "Slide")
            {
                motionEffects.Add(motionType);
            }

            var particleType = (ParticleTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
            var particleSettings = new Core.Models.ParticleSettings
            {
                ParticleType = particleType,
                ParticleCount = (int)(ParticleCountSlider?.Value ?? 100),
                ParticleSpeed = (float)(ParticleSpeedSlider?.Value ?? 50),
                ParticleColor = GetParticleColor(),
                Enabled = particleType != "None"
            };

            var interactiveSettings = new Core.Models.InteractiveSettings
            {
                MouseTrackingEnabled = MouseTrackingCheckBox?.IsChecked ?? false,
                ParallaxIntensity = (float)(ParallaxIntensitySlider?.Value ?? 0.5),
                ClockEffectsEnabled = ClockEffectsCheckBox?.IsChecked ?? false,
                TimeColorShiftEnabled = TimeColorShiftCheckBox?.IsChecked ?? true,
                TimeOpacityEnabled = TimeOpacityCheckBox?.IsChecked ?? false
            };

            var profile = new AnimationProfile
            {
                FramesPerSecond = fps,
                DurationSeconds = duration,
                MotionType = motionType,
                EasingType = GetEasingType(),
                Loop = LoopCheckBox.IsChecked ?? true,
                TextAnimationType = textAnimationType,
                MotionEffects = motionEffects,
                ParticleSettings = particleSettings,
                InteractiveSettings = interactiveSettings
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
                // Show preview of first frame
                UpdateAnimationPreview();
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

            // Create animation profile from UI settings (simplified for export)
            var motionType = (MotionTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "fade";
            var textAnimationTypeStr = (TextAnimationTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
            var textAnimationType = textAnimationTypeStr switch
            {
                "Fade" => Core.Models.TextAnimationType.Fade,
                "Slide" => Core.Models.TextAnimationType.Slide,
                "Typewriter" => Core.Models.TextAnimationType.Typewriter,
                _ => Core.Models.TextAnimationType.None
            };

            var motionEffects = new List<string>();
            if (motionType != "Fade" && motionType != "Slide")
            {
                motionEffects.Add(motionType);
            }

            var profile = new AnimationProfile
            {
                FramesPerSecond = int.TryParse(FpsTextBox.Text, out int fps) ? fps : 24,
                DurationSeconds = int.TryParse(DurationTextBox.Text, out int dur) ? dur : 6,
                MotionType = motionType,
                EasingType = GetEasingType(),
                Loop = LoopCheckBox.IsChecked ?? true,
                TextAnimationType = textAnimationType,
                MotionEffects = motionEffects,
                ParticleSettings = new Core.Models.ParticleSettings { Enabled = false },
                InteractiveSettings = new Core.Models.InteractiveSettings { MouseTrackingEnabled = false, ClockEffectsEnabled = false }
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

    /// <summary>
    /// Updates the animation preview and starts animating through all frames.
    /// </summary>
    private void UpdateAnimationPreview()
    {
        try
        {
            // Stop any existing animation
            StopPreviewAnimation();
            
            if (_generatedFrames == null || _generatedFrames.Count == 0)
            {
                if (NoAnimationPreviewBorder != null)
                    NoAnimationPreviewBorder.Visibility = Visibility.Visible;
                if (AnimationPreview != null)
                    AnimationPreview.Source = null;
                if (FrameNavigationBorder != null)
                    FrameNavigationBorder.Visibility = Visibility.Collapsed;
                if (FrameInfoText != null)
                    FrameInfoText.Visibility = Visibility.Collapsed;
                return;
            }

            // Reset frame index
            _currentPreviewFrameIndex = 0;

            // Show first frame immediately
            ShowPreviewFrame(0);

            // Update frame info display
            UpdateFrameInfo();

            // Show navigation controls
            if (FrameNavigationBorder != null)
                FrameNavigationBorder.Visibility = Visibility.Visible;
            if (FrameInfoText != null)
                FrameInfoText.Visibility = Visibility.Visible;

            // Start animation timer
            StartPreviewAnimation();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateAnimationPreview error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the frame information display (current frame, total frames, duration).
    /// </summary>
    private void UpdateFrameInfo()
    {
        try
        {
            if (_generatedFrames == null || _generatedFrames.Count == 0)
                return;

            // Update frame count displays
            if (CurrentFrameRun != null)
                CurrentFrameRun.Text = $"{_currentPreviewFrameIndex + 1}";
            if (TotalFramesRun != null)
                TotalFramesRun.Text = $"{_generatedFrames.Count}";
            if (MaxFrameText != null)
                MaxFrameText.Text = $"{_generatedFrames.Count}";
            if (FrameNumberTextBox != null)
                FrameNumberTextBox.Text = $"{_currentPreviewFrameIndex + 1}";

            // Calculate and display duration
            int fps = 24;
            if (FpsTextBox != null && int.TryParse(FpsTextBox.Text, out int parsedFps) && parsedFps > 0 && parsedFps <= 60)
            {
                fps = parsedFps;
            }

            var totalDuration = _generatedFrames.Count / (double)fps;
            if (DurationRun != null)
                DurationRun.Text = $"{totalDuration:F2}s";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateFrameInfo error: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows a specific frame in the preview.
    /// </summary>
    /// <param name="frameIndex">Index of the frame to show</param>
    private void ShowPreviewFrame(int frameIndex)
    {
        try
        {
            if (_generatedFrames == null || _generatedFrames.Count == 0)
                return;

            if (frameIndex < 0 || frameIndex >= _generatedFrames.Count)
                return;

            var framePath = _generatedFrames[frameIndex];
            if (!File.Exists(framePath))
                return;

            if (AnimationPreview == null || NoAnimationPreviewBorder == null)
                return;

            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(framePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                AnimationPreview.Source = bitmap;
                NoAnimationPreviewBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                if (NoAnimationPreviewText != null)
                    NoAnimationPreviewText.Text = $"Error loading preview: {ex.Message}";
                if (NoAnimationPreviewBorder != null)
                    NoAnimationPreviewBorder.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowPreviewFrame error: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts the preview animation timer to cycle through frames.
    /// </summary>
    private void StartPreviewAnimation()
    {
        try
        {
            if (_generatedFrames == null || _generatedFrames.Count == 0)
                return;

            // Stop any existing timer
            StopPreviewAnimation();

            // Get FPS from UI or use default
            int fps = 24;
            if (FpsTextBox != null && int.TryParse(FpsTextBox.Text, out int parsedFps) && parsedFps > 0 && parsedFps <= 60)
            {
                fps = parsedFps;
            }

            // Calculate frame delay in milliseconds (1000ms / fps)
            var frameDelay = TimeSpan.FromMilliseconds(1000.0 / fps);

            // Create and start timer
            _previewAnimationTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = frameDelay
            };
            _previewAnimationTimer.Tick += PreviewAnimationTimer_Tick;
            _previewAnimationTimer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartPreviewAnimation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the preview animation timer.
    /// </summary>
    private void StopPreviewAnimation()
    {
        try
        {
            if (_previewAnimationTimer != null)
            {
                _previewAnimationTimer.Stop();
                _previewAnimationTimer.Tick -= PreviewAnimationTimer_Tick;
                _previewAnimationTimer = null;
            }
            _currentPreviewFrameIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StopPreviewAnimation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the preview animation timer tick event.
    /// Advances to the next frame and loops back to the beginning when reaching the end.
    /// </summary>
    private void PreviewAnimationTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (_generatedFrames == null || _generatedFrames.Count == 0)
            {
                StopPreviewAnimation();
                return;
            }

            // Advance to next frame
            _currentPreviewFrameIndex++;
            
            // Loop back to beginning if we've reached the end
            if (_currentPreviewFrameIndex >= _generatedFrames.Count)
            {
                _currentPreviewFrameIndex = 0;
            }

            // Show the current frame
            ShowPreviewFrame(_currentPreviewFrameIndex);
            
            // Update frame info
            UpdateFrameInfo();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PreviewAnimationTimer_Tick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the First Frame button click event.
    /// </summary>
    private void FirstFrameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_generatedFrames == null || _generatedFrames.Count == 0)
            return;
        
        StopPreviewAnimation();
        _currentPreviewFrameIndex = 0;
        ShowPreviewFrame(0);
        UpdateFrameInfo();
    }

    /// <summary>
    /// Handles the Previous Frame button click event.
    /// </summary>
    private void PreviousFrameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_generatedFrames == null || _generatedFrames.Count == 0)
            return;
        
        StopPreviewAnimation();
        _currentPreviewFrameIndex--;
        if (_currentPreviewFrameIndex < 0)
            _currentPreviewFrameIndex = _generatedFrames.Count - 1;
        ShowPreviewFrame(_currentPreviewFrameIndex);
        UpdateFrameInfo();
    }

    /// <summary>
    /// Handles the Next Frame button click event.
    /// </summary>
    private void NextFrameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_generatedFrames == null || _generatedFrames.Count == 0)
            return;
        
        StopPreviewAnimation();
        _currentPreviewFrameIndex++;
        if (_currentPreviewFrameIndex >= _generatedFrames.Count)
            _currentPreviewFrameIndex = 0;
        ShowPreviewFrame(_currentPreviewFrameIndex);
        UpdateFrameInfo();
    }

    /// <summary>
    /// Handles the Last Frame button click event.
    /// </summary>
    private void LastFrameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_generatedFrames == null || _generatedFrames.Count == 0)
            return;
        
        StopPreviewAnimation();
        _currentPreviewFrameIndex = _generatedFrames.Count - 1;
        ShowPreviewFrame(_currentPreviewFrameIndex);
        UpdateFrameInfo();
    }

    /// <summary>
    /// Handles the Load Media button click event.
    /// Opens a file dialog to select images or videos for editing.
    /// </summary>
    private void LoadMediaButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Image or Video",
            Filter = "Media Files (*.png;*.jpg;*.jpeg;*.gif;*.mp4;*.webm;*.mov)|*.png;*.jpg;*.jpeg;*.gif;*.mp4;*.webm;*.mov|Image Files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|Video Files (*.mp4;*.webm;*.mov)|*.mp4;*.webm;*.mov",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _loadedMediaPath = dialog.FileName;
                
                // Load first frame for preview
                var extension = System.IO.Path.GetExtension(_loadedMediaPath).ToLowerInvariant();
                if (extension == ".gif" || extension == ".mp4" || extension == ".webm" || extension == ".mov")
                {
                    var frames = _mediaEditingService.ExtractVideoFrames(_loadedMediaPath, 1);
                    if (frames.Count > 0)
                    {
                        _currentEditedBitmap?.Dispose();
                        _currentEditedBitmap = frames[0];
                        UpdatePreviewFromBitmap(_currentEditedBitmap);
                    }
                }
                else
                {
                    _currentEditedBitmap?.Dispose();
                    _currentEditedBitmap = _mediaEditingService.LoadImage(_loadedMediaPath);
                    UpdatePreviewFromBitmap(_currentEditedBitmap);
                }

                // Update UI
                if (LoadedMediaText != null)
                    LoadedMediaText.Text = $"Loaded: {System.IO.Path.GetFileName(_loadedMediaPath)}";
                if (EditingControlsCard != null)
                    EditingControlsCard.Visibility = Visibility.Visible;
                if (TextOverlayCheckBox != null)
                    TextOverlayCheckBox.Checked += (s, args) => { if (ApplyTextOverlayButton != null) ApplyTextOverlayButton.IsEnabled = true; };
                if (TextOverlayCheckBox != null)
                    TextOverlayCheckBox.Unchecked += (s, args) => { if (ApplyTextOverlayButton != null) ApplyTextOverlayButton.IsEnabled = false; };
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load media: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Updates the preview image from a bitmap.
    /// </summary>
    private void UpdatePreviewFromBitmap(System.Drawing.Bitmap bitmap)
    {
        try
        {
            if (AnimationPreview == null || bitmap == null)
                return;

            using var ms = new System.IO.MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;

            var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            AnimationPreview.Source = bitmapImage;
            if (NoAnimationPreviewBorder != null)
                NoAnimationPreviewBorder.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating preview: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the Apply Crop button click event.
    /// </summary>
    private void ApplyCropButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEditedBitmap == null)
        {
            System.Windows.MessageBox.Show("Please load an image first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (int.TryParse(CropXTextBox?.Text, out int x) &&
                int.TryParse(CropYTextBox?.Text, out int y) &&
                int.TryParse(CropWidthTextBox?.Text, out int width) &&
                int.TryParse(CropHeightTextBox?.Text, out int height))
            {
                var cropArea = new System.Drawing.Rectangle(x, y, width, height);
                var cropped = _mediaEditingService.CropImage(_currentEditedBitmap, cropArea);
                _currentEditedBitmap?.Dispose();
                _currentEditedBitmap = cropped;
                UpdatePreviewFromBitmap(_currentEditedBitmap);
                System.Windows.MessageBox.Show("Crop applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter valid crop values.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to apply crop: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Apply Resize button click event.
    /// </summary>
    private void ApplyResizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEditedBitmap == null)
        {
            System.Windows.MessageBox.Show("Please load an image first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (int.TryParse(ResizeWidthTextBox?.Text, out int width) &&
                int.TryParse(ResizeHeightTextBox?.Text, out int height) &&
                width > 0 && height > 0)
            {
                var resized = _mediaEditingService.ResizeImage(_currentEditedBitmap, width, height);
                _currentEditedBitmap?.Dispose();
                _currentEditedBitmap = resized;
                UpdatePreviewFromBitmap(_currentEditedBitmap);
                System.Windows.MessageBox.Show("Resize applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter valid width and height values.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to apply resize: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Apply Filter button click event.
    /// </summary>
    private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEditedBitmap == null)
        {
            System.Windows.MessageBox.Show("Please load an image first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var filterType = FilterComboBox?.SelectedItem is System.Windows.Controls.ComboBoxItem item ? item.Content?.ToString() ?? "None" : "None";
            if (filterType == "None")
            {
                System.Windows.MessageBox.Show("Please select a filter.", "No Filter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var intensity = (float)(FilterIntensitySlider?.Value ?? 1.0);
            var filtered = _mediaEditingService.ApplyFilter(_currentEditedBitmap, filterType, intensity);
            _currentEditedBitmap?.Dispose();
            _currentEditedBitmap = filtered;
            UpdatePreviewFromBitmap(_currentEditedBitmap);
            System.Windows.MessageBox.Show($"Filter '{filterType}' applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to apply filter: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Apply Text Overlay button click event.
    /// </summary>
    private void ApplyTextOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEditedBitmap == null)
        {
            System.Windows.MessageBox.Show("Please load an image first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_quotes == null || _quotes.Count == 0)
        {
            System.Windows.MessageBox.Show("No quotes available. Please add quotes first.", "No Quotes", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var randomQuote = _quoteService.GetRandomQuote(_quotes);
            var (screenWidth, screenHeight) = AnimeQuoteWall.Core.Services.WindowsCompatibilityHelper.GetPrimaryScreenResolution();
            
            var settings = new Core.Models.WallpaperSettings
            {
                Width = _currentEditedBitmap.Width,
                Height = _currentEditedBitmap.Height,
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

            var withText = _mediaEditingService.AddTextOverlay(_currentEditedBitmap, randomQuote, settings);
            _currentEditedBitmap?.Dispose();
            _currentEditedBitmap = withText;
            UpdatePreviewFromBitmap(_currentEditedBitmap);
            System.Windows.MessageBox.Show("Text overlay applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to apply text overlay: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Frame Number TextBox key down event.
    /// Allows jumping to a specific frame by entering the frame number.
    /// </summary>
    private void FrameNumberTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && FrameNumberTextBox != null)
        {
            if (int.TryParse(FrameNumberTextBox.Text, out int frameNumber) && _generatedFrames != null && _generatedFrames.Count > 0)
            {
                StopPreviewAnimation();
                frameNumber = Math.Max(1, Math.Min(frameNumber, _generatedFrames.Count));
                _currentPreviewFrameIndex = frameNumber - 1;
                ShowPreviewFrame(_currentPreviewFrameIndex);
                UpdateFrameInfo();
            }
        }
    }

    /// <summary>
    /// Gets the particle color from the UI.
    /// </summary>
    private System.Drawing.Color GetParticleColor()
    {
        if (ParticleColorPreview != null && ParticleColorPreview.Background is System.Windows.Media.SolidColorBrush brush)
        {
            return System.Drawing.Color.FromArgb(brush.Color.R, brush.Color.G, brush.Color.B);
        }
        return System.Drawing.Color.White;
    }

    /// <summary>
    /// Handles particle color border click to open color picker.
    /// </summary>
    private void ParticleColorBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var colorDialog = new System.Windows.Forms.ColorDialog
        {
            Color = GetParticleColor(),
            FullOpen = true
        };

        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = colorDialog.Color;
            if (ParticleColorPreview != null)
            {
                ParticleColorPreview.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
            }
        }
    }
}

