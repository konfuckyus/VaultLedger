using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetCategorySelfRequestable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelfRequestable",
                table: "budget_category_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "budget_category_definitions",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                column: "IsSelfRequestable",
                value: true);

            migrationBuilder.UpdateData(
                table: "budget_category_definitions",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                column: "IsSelfRequestable",
                value: true);

            migrationBuilder.UpdateData(
                table: "budget_category_definitions",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
                column: "IsSelfRequestable",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelfRequestable",
                table: "budget_category_definitions");
        }
    }
}
