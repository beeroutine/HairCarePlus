using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HairCarePlus.Client.Patient.Infrastructure.Storage.Migrations;

[Migration("202507260001_AddLocalPathToPhotoReports")]
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