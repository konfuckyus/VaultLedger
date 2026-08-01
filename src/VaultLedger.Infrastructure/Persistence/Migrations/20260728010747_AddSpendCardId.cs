using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpendCardId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CardId",
                table: "transaction_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_records_CardId",
                table: "transaction_records",
                column: "CardId");

            migrationBuilder.AddForeignKey(
                name: "FK_transaction_records_cards_CardId",
                table: "transaction_records",
                column: "CardId",
                principalTable: "cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transaction_records_cards_CardId",
                table: "transaction_records");

            migrationBuilder.DropIndex(
                name: "IX_transaction_records_CardId",
                table: "transaction_records");

            migrationBuilder.DropColumn(
                name: "CardId",
                table: "transaction_records");
        }
    }
}
