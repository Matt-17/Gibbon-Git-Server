using System.Globalization;

using Gibbon.Git.Server.Configuration;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Gibbon.Git.Server.Services;

public class DateFormatter(ServerSettings serverSettings, CurrentUserSettings currentUserSettings)
    : IDateFormatter
{
    private static readonly DateTime Sample = new(DateTime.Today.Year, 12, 24, 17, 30, 45);

    private readonly ServerSettings _serverSettings = serverSettings;
    private readonly CurrentUserSettings _currentUserSettings = currentUserSettings;

    private DateFormat DateFormat => _currentUserSettings.Value.DateFormat ?? _serverSettings.DateFormat;

    private TimeFormat TimeFormat => _currentUserSettings.Value.TimeFormat ?? _serverSettings.TimeFormat;

    public string FormatDate(DateTime value) => Format(value, DateFormat);

    public string FormatDateTime(DateTime? value) => value.HasValue ? Format(value.Value, DateFormat, TimeFormat) : string.Empty;

    public List<SelectListItem> GetDateFormatOptions() => Enum.GetValues<DateFormat>()
        .Select(format => new SelectListItem
        {
            Value = ((int)format).ToString(CultureInfo.InvariantCulture),
            Text = format == DateFormat.Language
                ? $"{Resources.Settings_Global_FormatLanguage} ({Format(Sample, format)})"
                : Format(Sample, format)
        })
        .ToList();

    public List<SelectListItem> GetTimeFormatOptions() => Enum.GetValues<TimeFormat>()
        .Select(format => new SelectListItem
        {
            Value = ((int)format).ToString(CultureInfo.InvariantCulture),
            Text = format switch
            {
                TimeFormat.Language => $"{Resources.Settings_Global_FormatLanguage} ({Format(Sample, format)})",
                TimeFormat.Hidden => Resources.Settings_Global_TimeFormat_Hidden,
                _ => Format(Sample, format)
            }
        })
        .ToList();

    internal static string Format(DateTime value, DateFormat format) => format switch
    {
        DateFormat.YearMonthDay => value.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture),
        DateFormat.DayMonthYearDots => value.ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture),
        DateFormat.DayMonthYearSlashes => value.ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture),
        DateFormat.MonthDayYearSlashes => value.ToString("MM'/'dd'/'yyyy", CultureInfo.InvariantCulture),
        DateFormat.DayMonthNameYear => value.ToString("d MMM yyyy", CultureInfo.CurrentCulture),
        DateFormat.MonthNameDayYear => value.ToString("MMM d',' yyyy", CultureInfo.CurrentCulture),
        _ => value.ToString("d", CultureInfo.CurrentCulture)
    };

    internal static string Format(DateTime value, TimeFormat format) => format switch
    {
        TimeFormat.Hours24 => value.ToString("HH':'mm", CultureInfo.InvariantCulture),
        TimeFormat.Hours24WithSeconds => value.ToString("HH':'mm':'ss", CultureInfo.InvariantCulture),
        TimeFormat.Hours12 => value.ToString("h':'mm tt", CultureInfo.InvariantCulture),
        TimeFormat.Hidden => string.Empty,
        _ => value.ToString("T", CultureInfo.CurrentCulture)
    };

    internal static string Format(DateTime value, DateFormat dateFormat, TimeFormat timeFormat)
    {
        var time = Format(value, timeFormat);

        return time.Length == 0 ? Format(value, dateFormat) : $"{Format(value, dateFormat)} {time}";
    }
}
