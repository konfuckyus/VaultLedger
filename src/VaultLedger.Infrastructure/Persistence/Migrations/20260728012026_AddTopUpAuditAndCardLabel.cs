using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTopUpAuditAndCardLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PerformedByUserId",
                table: "transaction_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "cards",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "card_requests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_records_PerformedByUserId",
                table: "transaction_records",
                column: "PerformedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_transaction_records_users_PerformedByUserId",
                table: "transaction_records",
                column: "PerformedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transaction_records_users_PerformedByUserId",
                table: "transaction_records");

            migrationBuilder.DropIndex(
                name: "IX_transaction_records_PerformedByUserId",
                table: "transaction_records");

            migrationBuilder.DropColumn(
                name: "PerformedByUserId",
                table: "transaction_records");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "card_requests");
        }
    }
}
