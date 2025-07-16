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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalPath",
                table: "PhotoReports");
        }
    }
} 