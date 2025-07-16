using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HairCarePlus.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUploadUrlToPhotoReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "PhotoReports",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ImageUploadUrl",
                table: "PhotoReports",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUploadUrl",
                table: "PhotoReports");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "PhotoReports",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
