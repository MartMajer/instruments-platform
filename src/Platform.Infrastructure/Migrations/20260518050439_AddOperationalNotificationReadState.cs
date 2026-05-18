using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalNotificationReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_operational_notification_status",
                table: "operational_notification");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "read_at",
                table: "operational_notification",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "operational_notification",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE operational_notification
                SET updated_at = created_at
                WHERE updated_at IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "operational_notification",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_operational_notification_status",
                table: "operational_notification",
                sql: "status IN ('unread','read')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_operational_notification_status",
                table: "operational_notification");

            migrationBuilder.DropColumn(
                name: "read_at",
                table: "operational_notification");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "operational_notification");

            migrationBuilder.AddCheckConstraint(
                name: "ck_operational_notification_status",
                table: "operational_notification",
                sql: "status IN ('unread')");
        }
    }
}
