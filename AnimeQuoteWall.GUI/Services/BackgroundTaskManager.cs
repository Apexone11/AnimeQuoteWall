using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AnimeQuoteWall.GUI.Services;

/// <summary>
/// Manages background tasks like animation generation that can continue while user navigates between pages
/// </summary>
public class BackgroundTaskManager
{
    private static BackgroundTaskManager? _instance;
    public static BackgroundTaskManager Instance => _instance ??= new BackgroundTaskManager();

    public event EventHandler<BackgroundTaskStatus>? TaskStatusChanged;

    private BackgroundTaskStatus? _currentAnimationTask;

    public bool IsAnimationGenerating => _currentAnimationTask != null && _currentAnimationTask.IsRunning;

    public BackgroundTaskStatus? CurrentAnimationTask => _currentAnimationTask;

    public void StartAnimationGeneration(Task<IReadOnlyList<string>> generationTask, CancellationTokenSource cancellationTokenSource)
    {
        _currentAnimationTask = new BackgroundTaskStatus
        {
            Task = generationTask,
            CancellationTokenSource = cancellationTokenSource,
            IsRunning = true,
            Progress = 0,
            StatusMessage = "Generating frames..."
        };

        TaskStatusChanged?.Invoke(this, _currentAnimationTask);

        // Monitor the task
        _ = MonitorTaskAsync(generationTask, cancellationTokenSource);
    }

    private async Task MonitorTaskAsync(Task<IReadOnlyList<string>> task, CancellationTokenSource cts)
    {
        try
        {
            await task;
            
            if (_currentAnimationTask != null)
            {
                _currentAnimationTask.IsRunning = false;
                _currentAnimationTask.Progress = 100;
                _currentAnimationTask.StatusMessage = "Generation complete!";
                _currentAnimationTask.Result = task.Result;
                TaskStatusChanged?.Invoke(this, _currentAnimationTask);
            }
        }
        catch (OperationCanceledException)
        {
            if (_currentAnimationTask != null)
            {
                _currentAnimationTask.IsRunning = false;
                _currentAnimationTask.StatusMessage = "Generation cancelled.";
                TaskStatusChanged?.Invoke(this, _currentAnimationTask);
            }
        }
        catch (Exception ex)
        {
            if (_currentAnimationTask != null)
            {
                _currentAnimationTask.IsRunning = false;
                _currentAnimationTask.StatusMessage = $"Error: {ex.Message}";
                TaskStatusChanged?.Invoke(this, _currentAnimationTask);
            }
        }
    }

    public void UpdateProgress(double progress, string message)
    {
        if (_currentAnimationTask != null)
        {
            _currentAnimationTask.Progress = progress;
            _currentAnimationTask.StatusMessage = message;
            TaskStatusChanged?.Invoke(this, _currentAnimationTask);
        }
    }

    public void CancelAnimationGeneration()
    {
        _currentAnimationTask?.CancellationTokenSource?.Cancel();
    }

    public void ClearAnimationTask()
    {
        _currentAnimationTask = null;
        TaskStatusChanged?.Invoke(this, new BackgroundTaskStatus { IsRunning = false });
    }
}

public class BackgroundTaskStatus
{
    public Task<IReadOnlyList<string>>? Task { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }
    public bool IsRunning { get; set; }
    public double Progress { get; set; }
    public string StatusMessage { get; set; } = "";
    public IReadOnlyList<string>? Result { get; set; }
}

