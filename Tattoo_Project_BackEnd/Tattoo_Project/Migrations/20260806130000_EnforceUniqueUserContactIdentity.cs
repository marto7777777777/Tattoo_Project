using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tattoo_Project.Data;

#nullable disable

namespace Tattoo_Project.Migrations
{
    [DbContext(typeof(TattooDbContext))]
    [Migration("20260806130000_EnforceUniqueUserContactIdentity")]
    public partial class EnforceUniqueUserContactIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing installations stored phone numbers only on profile rows.
            // Backfill Identity so User becomes the single owner of the number.
            migrationBuilder.Sql("""
                UPDATE users
                SET users.PhoneNumber = COALESCE(
                    (SELECT TOP (1) artist.PhoneNumber FROM TattooArtists artist WHERE artist.UserId = users.Id),
                    (SELECT TOP (1) client.PhoneNumber FROM Clients client WHERE client.UserId = users.Id))
                FROM AspNetUsers users
                WHERE users.PhoneNumber IS NULL;

                UPDATE AspNetUsers
                SET PhoneNumber = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(PhoneNumber, ' ', ''), '-', ''), '(', ''), ')', ''), '.', '')
                WHERE PhoneNumber IS NOT NULL;

                UPDATE AspNetUsers
                SET PhoneNumber = '+' + SUBSTRING(PhoneNumber, 3, LEN(PhoneNumber))
                WHERE PhoneNumber LIKE '00%';

                -- Normalize legacy Bulgarian national-format numbers where the
                -- user's existing Client profile identifies the country.
                UPDATE users
                SET users.PhoneNumber = '+359' + SUBSTRING(users.PhoneNumber, 2, LEN(users.PhoneNumber))
                FROM AspNetUsers users
                WHERE users.PhoneNumber LIKE '0%'
                  AND EXISTS (
                      SELECT 1 FROM Clients client
                      WHERE client.UserId = users.Id
                        AND LOWER(client.Country) IN (N'bulgaria', N'българия'));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "PhoneNumberIndex",
                table: "AspNetUsers",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "PhoneNumberIndex",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}
