using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HairCarePlus.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class chat_delivery_queue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhotoComments_PhotoReports_PhotoReportId2",
                table: "PhotoComments");

            migrationBuilder.DropIndex(
                name: "IX_PhotoComments_PhotoReportId2",
                table: "PhotoComments");

            migrationBuilder.RenameColumn(
                name: "PhotoReportId2",
                table: "PhotoComments",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PhotoComments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PhotoComments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TranslatedContent",
                table: "ChatMessages",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TargetLanguage",
                table: "ChatMessages",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SourceLanguage",
                table: "ChatMessages",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "DeliveryQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    BlobUrl = table.Column<string>(type: "TEXT", nullable: true),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceiversMask = table.Column<byte>(type: "INTEGER", nullable: false),
                    DeliveredMask = table.Column<byte>(type: "INTEGER", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryQueue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CreatedAt",
                table: "ChatMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReceiverId",
                table: "ChatMessages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQueue_ExpiresAtUtc",
                table: "DeliveryQueue",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryQueue_PatientId",
                table: "DeliveryQueue",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryQueue");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_CreatedAt",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ReceiverId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PhotoComments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PhotoComments");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PhotoComments",
                newName: "PhotoReportId2");

            migrationBuilder.AlterColumn<string>(
                name: "TranslatedContent",
                table: "ChatMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetLanguage",
                table: "ChatMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceLanguage",
                table: "ChatMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoComments_PhotoReportId2",
                table: "PhotoComments",
                column: "PhotoReportId2");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoComments_PhotoReports_PhotoReportId2",
                table: "PhotoComments",
                column: "PhotoReportId2",
                principalTable: "PhotoReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
