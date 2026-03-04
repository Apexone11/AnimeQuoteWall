using System;
using System.Collections.Generic;
using System.Linq;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Protection;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for parsing schedules and calculating next execution times.
/// Handles different schedule types: Interval, Hourly, Daily, OnLaunch, and Custom.
/// </summary>
public class ScheduleService
{
    /// <summary>
    /// Calculates the next execution time for a playlist based on its schedule configuration.
    /// Protected method - proprietary scheduling algorithm.
    /// </summary>
    /// <param name="playlist">The playlist with schedule configuration.</param>
    /// <param name="currentTime">The current time (defaults to DateTime.Now).</param>
    /// <returns>The next execution time, or null if schedule is invalid or not applicable.</returns>
    [System.Diagnostics.DebuggerStepThrough]
    public DateTime? CalculateNextExecutionTime(Playlist playlist, DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.Now;

        return playlist.ScheduleType switch
        {
            "Interval" => CalculateIntervalNextTime(playlist, now),
            "Hourly" => CalculateHourlyNextTime(now),
            "Daily" => CalculateDailyNextTime(playlist, now),
            "OnLaunch" => null, // OnLaunch doesn't have a next time - it runs immediately
            "Custom" => CalculateCustomNextTime(playlist, now),
            _ => null
        };
    }

    /// <summary>
    /// Checks if a playlist should execute now based on its schedule.
    /// </summary>
    /// <param name="playlist">The playlist to check.</param>
    /// <param name="currentTime">The current time (defaults to DateTime.Now).</param>
    /// <returns>True if the playlist should execute now.</returns>
    public bool ShouldExecuteNow(Playlist playlist, DateTime? currentTime = null)
    {
        if (!playlist.Enabled || !playlist.IsValid())
            return false;

        var now = currentTime ?? DateTime.Now;

        return playlist.ScheduleType switch
        {
            "Interval" => true, // Interval-based is handled by timer, not schedule check
            "Hourly" => IsOnTheHour(now),
            "Daily" => IsAtScheduledTime(playlist, now),
            "OnLaunch" => true, // Always execute on launch
            "Custom" => IsAtScheduledTime(playlist, now) && IsOnScheduledDay(playlist, now),
            _ => false
        };
    }

    /// <summary>
    /// Calculates the next execution time for interval-based scheduling.
    /// </summary>
    private DateTime CalculateIntervalNextTime(Playlist playlist, DateTime now)
    {
        return now.AddSeconds(playlist.IntervalSeconds);
    }

    /// <summary>
    /// Calculates the next execution time for hourly scheduling.
    /// </summary>
    private DateTime CalculateHourlyNextTime(DateTime now)
    {
        // Next hour, at minute 0
        return now.Date.AddHours(now.Hour + 1);
    }

    /// <summary>
    /// Calculates the next execution time for daily scheduling.
    /// </summary>
    private DateTime? CalculateDailyNextTime(Playlist playlist, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(playlist.ScheduleTime))
            return null;

        if (!ParseTime(playlist.ScheduleTime, out var hour, out var minute))
            return null;

        var scheduledTime = now.Date.AddHours(hour).AddMinutes(minute);

        // If scheduled time has passed today, schedule for tomorrow
        if (scheduledTime <= now)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        return scheduledTime;
    }

    /// <summary>
    /// Calculates the next execution time for custom scheduling.
    /// </summary>
    private DateTime? CalculateCustomNextTime(Playlist playlist, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(playlist.ScheduleTime))
            return null;

        if (!ParseTime(playlist.ScheduleTime, out var hour, out var minute))
            return null;

        // If no days specified, treat as daily
        if (playlist.DaysOfWeek == null || playlist.DaysOfWeek.Count == 0)
        {
            return CalculateDailyNextTime(playlist, now);
        }

        // Find next scheduled day
        var scheduledTime = now.Date.AddHours(hour).AddMinutes(minute);
        var daysToAdd = 0;
        var maxDays = 7;

        for (int i = 0; i < maxDays; i++)
        {
            var checkDate = scheduledTime.AddDays(i);
            var dayOfWeek = (int)checkDate.DayOfWeek;

            if (playlist.DaysOfWeek.Contains(dayOfWeek) && checkDate > now)
            {
                daysToAdd = i;
                break;
            }
        }

        return scheduledTime.AddDays(daysToAdd);
    }

    /// <summary>
    /// Checks if the current time is on the hour (minute 0).
    /// </summary>
    private bool IsOnTheHour(DateTime now)
    {
        return now.Minute == 0 && now.Second < 5; // 5 second window
    }

    /// <summary>
    /// Checks if the current time matches the scheduled time.
    /// </summary>
    private bool IsAtScheduledTime(Playlist playlist, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(playlist.ScheduleTime))
            return false;

        if (!ParseTime(playlist.ScheduleTime, out var hour, out var minute))
            return false;

        // Check if we're within a 5-second window of the scheduled time
        return now.Hour == hour && 
               now.Minute == minute && 
               now.Second < 5;
    }

    /// <summary>
    /// Checks if the current day is one of the scheduled days.
    /// </summary>
    private bool IsOnScheduledDay(Playlist playlist, DateTime now)
    {
        if (playlist.DaysOfWeek == null || playlist.DaysOfWeek.Count == 0)
            return true; // No day restriction

        var currentDayOfWeek = (int)now.DayOfWeek;
        return playlist.DaysOfWeek.Contains(currentDayOfWeek);
    }

    /// <summary>
    /// Parses a time string in HH:mm format.
    /// </summary>
    /// <param name="timeString">Time string to parse (e.g., "14:30").</param>
    /// <param name="hour">Output hour (0-23).</param>
    /// <param name="minute">Output minute (0-59).</param>
    /// <returns>True if parsing was successful.</returns>
    private bool ParseTime(string timeString, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;

        if (string.IsNullOrWhiteSpace(timeString))
            return false;

        var parts = timeString.Split(':');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out hour) || hour < 0 || hour > 23)
            return false;

        if (!int.TryParse(parts[1], out minute) || minute < 0 || minute > 59)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a human-readable description of the schedule.
    /// </summary>
    /// <param name="playlist">The playlist with schedule configuration.</param>
    /// <returns>A description string.</returns>
    public string GetScheduleDescription(Playlist playlist)
    {
        return playlist.ScheduleType switch
        {
            "Interval" => $"Every {playlist.IntervalSeconds} seconds",
            "Hourly" => "Every hour",
            "Daily" => $"Daily at {playlist.ScheduleTime ?? "N/A"}",
            "OnLaunch" => "On application launch",
            "Custom" => GetCustomScheduleDescription(playlist),
            _ => "Unknown schedule"
        };
    }

    /// <summary>
    /// Gets a description for custom schedules.
    /// </summary>
    private string GetCustomScheduleDescription(Playlist playlist)
    {
        if (string.IsNullOrWhiteSpace(playlist.ScheduleTime))
            return "Custom (invalid)";

        var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        
        if (playlist.DaysOfWeek == null || playlist.DaysOfWeek.Count == 0)
        {
            return $"Daily at {playlist.ScheduleTime}";
        }

        // Validate day indices and map to names, filtering out invalid values
        var days = playlist.DaysOfWeek
            .Where(d => d >= 0 && d < dayNames.Length)
            .Select(d => dayNames[d])
            .ToList();
        
        if (days.Count == 0)
        {
            return $"Custom at {playlist.ScheduleTime} (no valid days)";
        }
        
        var daysStr = days.Count == 1 ? days[0] : string.Join(", ", days.Take(days.Count - 1)) + " and " + days.Last();

        return $"{daysStr} at {playlist.ScheduleTime}";
    }
}

