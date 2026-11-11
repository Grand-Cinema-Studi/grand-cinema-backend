using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrandCinema.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Reservations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsOfSaleAcceptedAt",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsOfSaleVersion",
                table: "Reservations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "TermsOfSaleAcceptedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "TermsOfSaleVersion",
                table: "Reservations");
        }
    }
}
