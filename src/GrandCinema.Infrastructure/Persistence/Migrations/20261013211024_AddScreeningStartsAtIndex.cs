using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrandCinema.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScreeningStartsAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Screenings_StartsAt",
                table: "Screenings",
                column: "StartsAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Screenings_StartsAt",
                table: "Screenings");
        }
    }
}
