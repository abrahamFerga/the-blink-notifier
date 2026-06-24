// BlinkNotifier.Settings — startup validation (ARCH.md § Cross-cutting wiring, ADR-0007)
using Microsoft.Extensions.Options;

namespace BlinkNotifier.Settings;

public sealed class BlinkSettingsValidator : IValidateOptions<BlinkSettings>
{
    public ValidateOptionsResult Validate(string? name, BlinkSettings options)
    {
        var errors = new List<string>();

        if (options.ReminderIntervalMinutes is < 1 or > 60)
            errors.Add("ReminderIntervalMinutes must be between 1 and 60.");

        if (options.ScheduleEnabled && options.ScheduleStartTime >= options.ScheduleEndTime)
            errors.Add("ScheduleStartTime must be earlier than ScheduleEndTime.");

        if (options.ScheduleEnabled && options.ActiveDays.Length == 0)
            errors.Add("ActiveDays must contain at least one day when schedule is enabled.");

        if (options.SnoozeOptionsMinutes.Length == 0)
            errors.Add("SnoozeOptionsMinutes must contain at least one duration.");

        if (options.SnoozeOptionsMinutes.Any(m => m <= 0))
            errors.Add("All snooze durations must be positive.");

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
