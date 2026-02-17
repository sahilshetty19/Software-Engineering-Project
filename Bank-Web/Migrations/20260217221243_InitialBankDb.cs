using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialBankDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankCustomers",
                columns: table => new
                {
                    BankCustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Nationality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PPSN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Eircode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Occupation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceOfFunds = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsPEP = table.Column<bool>(type: "boolean", nullable: false),
                    RiskRating = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCustomers", x => x.BankCustomerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankCustomers_FirstName_LastName_DateOfBirth",
                table: "BankCustomers",
                columns: new[] { "FirstName", "LastName", "DateOfBirth" });

            migrationBuilder.CreateIndex(
                name: "IX_BankCustomers_PPSN",
                table: "BankCustomers",
                column: "PPSN",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankCustomers");
        }
    }
}
