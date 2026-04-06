using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UngDungOnThiBangLai.Migrations
{
    /// <inheritdoc />
    public partial class OTBL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$.288CIbK2aMITYsOLRVH4uCY/CwnQ8ieZk45St2OdoKI/6c.DDJWq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$XKT1ECVKwYdGNMjAxre5UuMH6XOg0uZMKijHcpm6QwuVH0JKV9AS2");
        }
    }
}
