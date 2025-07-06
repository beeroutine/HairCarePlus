using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HairCarePlus.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePhotoCommentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhotoComments_PhotoReports_PhotoReportId1",
                table: "PhotoComments");

            migrationBuilder.RenameColumn(
                name: "PhotoReportId1",
                table: "PhotoComments",
                newName: "PhotoReportId2");

            migrationBuilder.RenameIndex(
                name: "IX_PhotoComments_PhotoReportId1",
                table: "PhotoComments",
                newName: "IX_PhotoComments_PhotoReportId2");

            migrationBuilder.CreateTable(
                name: "Restrictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restrictions", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoComments_PhotoReports_PhotoReportId2",
                table: "PhotoComments",
                column: "PhotoReportId2",
                principalTable: "PhotoReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhotoComments_PhotoReports_PhotoReportId2",
                table: "PhotoComments");

            migrationBuilder.DropTable(
                name: "Restrictions");

            migrationBuilder.RenameColumn(
                name: "PhotoReportId2",
                table: "PhotoComments",
                newName: "PhotoReportId1");

            migrationBuilder.RenameIndex(
                name: "IX_PhotoComments_PhotoReportId2",
                table: "PhotoComments",
                newName: "IX_PhotoComments_PhotoReportId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoComments_PhotoReports_PhotoReportId1",
                table: "PhotoComments",
                column: "PhotoReportId1",
                principalTable: "PhotoReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
