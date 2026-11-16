using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Models;

public class AttendanceVersion
{
    public int AttendanceVersionId { get; set; }
    public int AttendanceRecordId { get; set; }
    public int VersionNumber { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
    public int? Grade { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }

    public AttendanceRecord AttendanceRecord { get; set; } = null!;
}
