using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Services.Common;

internal static class NameNormalizer
{
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
