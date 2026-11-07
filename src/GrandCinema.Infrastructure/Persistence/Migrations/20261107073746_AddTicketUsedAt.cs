using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrandCinema.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketUsedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TicketUsedAt",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketUsedAt",
                table: "Reservations");
        }
    }
}
