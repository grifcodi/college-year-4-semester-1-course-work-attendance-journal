using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Models;

public class AuditEntry
{
    public int AuditEntryId { get; set; }
    public DateTime TimeStamp { get; set; }
    public AuditAction Action { get; set; }
    public AuditEntityType EntityType { get; set; }
    public int? EntityId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;

}
