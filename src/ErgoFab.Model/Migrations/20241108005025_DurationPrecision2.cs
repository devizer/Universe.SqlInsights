using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErgoFab.Model.Migrations
{
    /// <inheritdoc />
    public partial class DurationPrecision2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ProjectDuration_Start",
                table: "Project",
                type: "datetimeoffset(2)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ProjectDuration_Finish",
                table: "Project",
                type: "datetimeoffset(2)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Enrollment_Start",
                table: "Employee",
                type: "datetimeoffset(2)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Enrollment_Finish",
                table: "Employee",
                type: "datetimeoffset(2)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ProjectDuration_Start",
                table: "Project",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(2)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ProjectDuration_Finish",
                table: "Project",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Enrollment_Start",
                table: "Employee",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(2)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Enrollment_Finish",
                table: "Employee",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(2)",
                oldNullable: true);
        }
    }
}
