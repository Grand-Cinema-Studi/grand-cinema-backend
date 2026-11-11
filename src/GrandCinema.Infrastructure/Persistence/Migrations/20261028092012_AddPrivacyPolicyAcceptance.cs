using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrandCinema.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyPolicyAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyPolicyAcceptedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyVersion",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivacyPolicyAcceptedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyVersion",
                table: "Users");
        }
    }
}
