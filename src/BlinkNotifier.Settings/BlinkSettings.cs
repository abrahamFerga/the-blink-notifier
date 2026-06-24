// BlinkNotifier.Settings — BlinkSettings POCO (ARCH.md § Data model)
using System.ComponentModel.DataAnnotations;

namespace BlinkNotifier.Settings;

public sealed class BlinkSettings
{
    public int SchemaVersion { get; init; } = 1;

    [Range(1, 60, ErrorMessage = "Reminder interval must be between 1 and 60 minutes.")]
    public int ReminderIntervalMinutes { get; init; } = 20;

    public bool ScheduleEnabled { get; init; } = false;
    public TimeSpan ScheduleStartTime { get; init; } = TimeSpan.FromHours(9);
    public TimeSpan ScheduleEndTime { get; init; } = TimeSpan.FromHours(18);

    public DayOfWeek[] ActiveDays { get; init; } =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday,
    ];

    public bool AutoLaunchEnabled { get; init; } = true;
    public int[] SnoozeOptionsMinutes { get; init; } = [5, 15, 60];
}
