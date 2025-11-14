using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogTable_IgnoreExisting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.CreateTable(
            //     name: "AuditLog",
            //     columns: table => new
            //     {
            //         Id = table.Column<long>(type: "bigint", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
            //         UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            //         UserIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
            //         ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //         ResourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            //         CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            //         ActionDetails = table.Column<string>(type: "nvarchar(max)", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_AuditLog", x => x.Id);
            //     });

            // migrationBuilder.CreateIndex(
            //     name: "IX_AuditLog_ActionType_TimestampUtc",
            //     table: "AuditLog",
            //     columns: new[] { "ActionType", "TimestampUtc" });

            // migrationBuilder.CreateIndex(
            //     name: "IX_AuditLog_TimestampUtc",
            //     table: "AuditLog",
            //     column: "TimestampUtc");

            // migrationBuilder.CreateIndex(
            //     name: "IX_AuditLog_UserId",
            //     table: "AuditLog",
            //     column: "UserId");

            // migrationBuilder.CreateIndex(
            //     name: "IX_AuditLog_UserId_TimestampUtc",
            //     table: "AuditLog",
            //     columns: new[] { "UserId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");
        }
    }
}
