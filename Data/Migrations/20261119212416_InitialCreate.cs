using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceJournal.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditEntries",
            columns: table => new
            {
                AuditEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                Action = table.Column<string>(type: "TEXT", nullable: false),
                EntityType = table.Column<string>(type: "TEXT", nullable: false),
                EntityId = table.Column<int>(type: "INTEGER", nullable: true),
                PayloadJson = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEntries", x => x.AuditEntryId);
            });

        migrationBuilder.CreateTable(
            name: "Settings",
            columns: table => new
            {
                Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Settings", x => x.Key);
            });

        migrationBuilder.CreateTable(
            name: "StudentGroups",
            columns: table => new
            {
                StudentGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                AcademicYear = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StudentGroups", x => x.StudentGroupId);
            });

        migrationBuilder.CreateTable(
            name: "Subjects",
            columns: table => new
            {
                SubjectId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Subjects", x => x.SubjectId);
            });

        migrationBuilder.CreateTable(
            name: "Students",
            columns: table => new
            {
                StudentId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                StudentGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Students", x => x.StudentId);
                table.ForeignKey(
                    name: "FK_Students_StudentGroups_StudentGroupId",
                    column: x => x.StudentGroupId,
                    principalTable: "StudentGroups",
                    principalColumn: "StudentGroupId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Lessons",
            columns: table => new
            {
                LessonId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                StudentGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                PairNumber = table.Column<int>(type: "INTEGER", nullable: false),
                Topic = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Lessons", x => x.LessonId);
                table.CheckConstraint("CK_Lesson_PairNumber", "[PairNumber] >= 1 AND [PairNumber] <= 8");
                table.ForeignKey(
                    name: "FK_Lessons_StudentGroups_StudentGroupId",
                    column: x => x.StudentGroupId,
                    principalTable: "StudentGroups",
                    principalColumn: "StudentGroupId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Lessons_Subjects_SubjectId",
                    column: x => x.SubjectId,
                    principalTable: "Subjects",
                    principalColumn: "SubjectId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AttendanceRecords",
            columns: table => new
            {
                AttendanceRecordId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                LessonId = table.Column<int>(type: "INTEGER", nullable: false),
                StudentId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AttendanceRecords", x => x.AttendanceRecordId);
                table.ForeignKey(
                    name: "FK_AttendanceRecords_Lessons_LessonId",
                    column: x => x.LessonId,
                    principalTable: "Lessons",
                    principalColumn: "LessonId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AttendanceRecords_Students_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Students",
                    principalColumn: "StudentId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AttendanceVersions",
            columns: table => new
            {
                AttendanceVersionId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AttendanceRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Grade = table.Column<int>(type: "INTEGER", nullable: true),
                Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AttendanceVersions", x => x.AttendanceVersionId);
                table.CheckConstraint("CK_AttendanceVersions_Grade", "[Grade] IS NULL OR ([Grade] >= 0 AND [Grade] <= 100)");
                table.ForeignKey(
                    name: "FK_AttendanceVersions_AttendanceRecords_AttendanceRecordId",
                    column: x => x.AttendanceRecordId,
                    principalTable: "AttendanceRecords",
                    principalColumn: "AttendanceRecordId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "Settings",
            columns: ["Key", "Value"],
            values: new object[,]
            {
                { "Language", "Uk" },
                { "Theme", "Light" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_AttendanceRecords_LessonId_StudentId",
            table: "AttendanceRecords",
            columns: ["LessonId", "StudentId"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AttendanceRecords_StudentId",
            table: "AttendanceRecords",
            column: "StudentId");

        migrationBuilder.CreateIndex(
            name: "IX_AttendanceVersions_AttendanceRecordId_VersionNumber",
            table: "AttendanceVersions",
            columns: ["AttendanceRecordId", "VersionNumber"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuditEntries_EntityType_Action",
            table: "AuditEntries",
            columns: ["EntityType", "Action"]);

        migrationBuilder.CreateIndex(
            name: "IX_AuditEntries_TimeStamp",
            table: "AuditEntries",
            column: "TimeStamp");

        migrationBuilder.CreateIndex(
            name: "IX_Lessons_Date",
            table: "Lessons",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_Lessons_StudentGroupId_Date_PairNumber",
            table: "Lessons",
            columns: ["StudentGroupId", "Date", "PairNumber"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Lessons_SubjectId",
            table: "Lessons",
            column: "SubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_StudentGroups_NormalizedName",
            table: "StudentGroups",
            column: "NormalizedName",
            unique: true,
            filter: "[IsArchived] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_Students_StudentGroupId_IsArchived_FullName",
            table: "Students",
            columns: ["StudentGroupId", "IsArchived", "FullName"]);

        migrationBuilder.CreateIndex(
            name: "IX_Subjects_NormalizedName",
            table: "Subjects",
            column: "NormalizedName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AttendanceVersions");

        migrationBuilder.DropTable(
            name: "AuditEntries");

        migrationBuilder.DropTable(
            name: "Settings");

        migrationBuilder.DropTable(
            name: "AttendanceRecords");

        migrationBuilder.DropTable(
            name: "Lessons");

        migrationBuilder.DropTable(
            name: "Students");

        migrationBuilder.DropTable(
            name: "Subjects");

        migrationBuilder.DropTable(
            name: "StudentGroups");
    }
}
