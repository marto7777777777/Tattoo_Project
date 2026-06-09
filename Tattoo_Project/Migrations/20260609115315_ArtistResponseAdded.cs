using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tattoo_Project.Migrations
{
    /// <inheritdoc />
    public partial class ArtistResponseAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArtistResponse_TattooRequests_TattooRequestId",
                table: "ArtistResponse");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArtistResponse",
                table: "ArtistResponse");

            migrationBuilder.RenameTable(
                name: "ArtistResponse",
                newName: "ArtistResponses");

            migrationBuilder.RenameIndex(
                name: "IX_ArtistResponse_TattooRequestId",
                table: "ArtistResponses",
                newName: "IX_ArtistResponses_TattooRequestId");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Clients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArtistResponses",
                table: "ArtistResponses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ArtistResponses_TattooRequests_TattooRequestId",
                table: "ArtistResponses",
                column: "TattooRequestId",
                principalTable: "TattooRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArtistResponses_TattooRequests_TattooRequestId",
                table: "ArtistResponses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArtistResponses",
                table: "ArtistResponses");

            migrationBuilder.RenameTable(
                name: "ArtistResponses",
                newName: "ArtistResponse");

            migrationBuilder.RenameIndex(
                name: "IX_ArtistResponses_TattooRequestId",
                table: "ArtistResponse",
                newName: "IX_ArtistResponse_TattooRequestId");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArtistResponse",
                table: "ArtistResponse",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ArtistResponse_TattooRequests_TattooRequestId",
                table: "ArtistResponse",
                column: "TattooRequestId",
                principalTable: "TattooRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
