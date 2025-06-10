using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogukanSite.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingPhoneToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingPhoneNumber",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingPhoneNumber",
                table: "Orders");
        }
    }
}
