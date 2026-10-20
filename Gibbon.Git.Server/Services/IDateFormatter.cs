using Microsoft.AspNetCore.Mvc.Rendering;

namespace Gibbon.Git.Server.Services;

public interface IDateFormatter
{
    string FormatDate(DateTime value);

    string FormatDateTime(DateTime? value);

    List<SelectListItem> GetDateFormatOptions();

    List<SelectListItem> GetTimeFormatOptions();
}
