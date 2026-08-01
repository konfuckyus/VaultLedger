using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VaultLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetCategoriesAndTransactionPin : Migration
    {
        private static readonly Guid GenelId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_account_requests_UserId_Pending",
                table: "account_requests");

            migrationBuilder.AddColumn<string>(
                name: "TransactionPinHash",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTransferable",
                table: "accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "account_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "budget_category_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefaultAllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsTransferable = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_category_definitions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "budget_category_definitions",
                columns: new[] { "Id", "CreatedAt", "DefaultAllocatedAmount", "IsActive", "IsSystemDefault", "IsTransferable", "Name" },
                values: new object[,]
                {
                    { GenelId, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0m, true, true, true, "Genel" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 250m, true, false, false, "Yemek" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 100m, true, false, false, "Kahve/Çay" }
                });

            // Existing user accounts / requests map to Genel (system clearing stays null).
            migrationBuilder.Sql($"""
                UPDATE accounts
                SET "CategoryId" = '{GenelId}'
                WHERE "AccountType" = 'User' AND "CategoryId" IS NULL;
                """);

            migrationBuilder.Sql($"""
                UPDATE account_requests
                SET "CategoryId" = '{GenelId}'
                WHERE "CategoryId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "account_requests",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CategoryId", "IsTransferable" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "TransactionPinHash",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_CategoryId",
                table: "accounts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_UserId_CategoryId",
                table: "accounts",
                columns: new[] { "UserId", "CategoryId" },
                unique: true,
                filter: "\"CategoryId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_account_requests_CategoryId",
                table: "account_requests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_account_requests_UserId_CategoryId_Pending",
                table: "account_requests",
                columns: new[] { "UserId", "CategoryId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_budget_category_definitions_Name",
                table: "budget_category_definitions",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_account_requests_budget_category_definitions_CategoryId",
                table: "account_requests",
                column: "CategoryId",
                principalTable: "budget_category_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_accounts_budget_category_definitions_CategoryId",
                table: "accounts",
                column: "CategoryId",
                principalTable: "budget_category_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_account_requests_budget_category_definitions_CategoryId",
                table: "account_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_accounts_budget_category_definitions_CategoryId",
                table: "accounts");

            migrationBuilder.DropTable(
                name: "budget_category_definitions");

            migrationBuilder.DropIndex(
                name: "IX_accounts_CategoryId",
                table: "accounts");

            migrationBuilder.DropIndex(
                name: "IX_accounts_UserId_CategoryId",
                table: "accounts");

            migrationBuilder.DropIndex(
                name: "IX_account_requests_CategoryId",
                table: "account_requests");

            migrationBuilder.DropIndex(
                name: "IX_account_requests_UserId_CategoryId_Pending",
                table: "account_requests");

            migrationBuilder.DropColumn(
                name: "TransactionPinHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "IsTransferable",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "account_requests");

            migrationBuilder.CreateIndex(
                name: "IX_account_requests_UserId_Pending",
                table: "account_requests",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }
    }
}
