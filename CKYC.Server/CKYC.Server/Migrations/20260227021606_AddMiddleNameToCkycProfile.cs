using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CKYC.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMiddleNameToCkycProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "CkycProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "CkycProfiles");
        }
    }
}
