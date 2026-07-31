using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce_flashsale_backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryAndAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Addresses",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Addresses",
                newName: "RecipientName");

            migrationBuilder.RenameColumn(
                name: "Detail",
                table: "Addresses",
                newName: "DetailAddress");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "RecipientName",
                table: "Addresses",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Addresses",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "DetailAddress",
                table: "Addresses",
                newName: "Detail");
        }
    }
}
