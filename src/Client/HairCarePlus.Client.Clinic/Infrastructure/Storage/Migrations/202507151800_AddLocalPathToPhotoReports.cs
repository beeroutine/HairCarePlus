using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using HairCarePlus.Client.Clinic.Infrastructure.Storage;

namespace HairCarePlus.Client.Clinic.Infrastructure.Storage.Migrations
{
    [Migration("202507151800_AddLocalPathToPhotoReports")]
    public partial class AddLocalPathToPhotoReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalPath",
                table: "PhotoReports",
                type: "TEXT",
                nullable: true);

            // Backfill schema changes for set grouping and type if they don't exist (idempotent for EnsureCreated fallback)
            try
            {
                migrationBuilder.AddColumn<string>(
                    name: "SetId",
                    table: "PhotoReports",
                    type: "TEXT",
                    nullable: true);
            }
            catch { }

            try
            {
                migrationBuilder.AddColumn<int>(
                    name: "Type",
                    table: "PhotoReports",
                    type: "INTEGER",
                    nullable: false,
                    defaultValue: 0);
            }
            catch { }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalPath",
                table: "PhotoReports");

            try
            {
                migrationBuilder.DropColumn(
                    name: "SetId",
                    table: "PhotoReports");
            }
            catch { }

            try
            {
                migrationBuilder.DropColumn(
                    name: "Type",
                    table: "PhotoReports");
            }
            catch { }
        }
    }
} 