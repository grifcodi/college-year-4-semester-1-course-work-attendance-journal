using AttendanceJournal.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace AttendanceJournal.Services.Common;

internal static class AuditHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        WriteIndented = false
    };

    public static AuditEntry CreateEntry(
        AuditAction action,
        AuditEntityType entityType,
        int? entityId,
        object payload)
    {
        return new AuditEntry
        {
            TimeStamp = DateTime.UtcNow,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions)
        };
    }
}
