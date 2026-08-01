using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestApprovalLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add nullable first so existing rows can be backfilled before the unique constraint.
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "accounts",
                type: "character(10)",
                fixedLength: true,
                maxLength: 10,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "AccountNumber",
                value: "0000000001");

            // Any non-seed accounts created before this migration get sequential numbers starting at 0000000002.
            migrationBuilder.Sql(
                """
                WITH numbered AS (
                    SELECT "Id",
                           LPAD((ROW_NUMBER() OVER (ORDER BY "CreatedAt") + 1)::text, 10, '0') AS num
                    FROM accounts
                    WHERE "AccountNumber" IS NULL
                )
                UPDATE accounts AS a
                SET "AccountNumber" = n.num
                FROM numbered AS n
                WHERE a."Id" = n."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "AccountNumber",
                table: "accounts",
                type: "character(10)",
                fixedLength: true,
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(10)",
                oldFixedLength: true,
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "account_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResultingAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_requests_accounts_ResultingAccountId",
                        column: x => x.ResultingAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_requests_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "card_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResultingCardId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_card_requests_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_card_requests_cards_ResultingCardId",
                        column: x => x.ResultingCardId,
                        principalTable: "cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_card_requests_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_card_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_AccountNumber",
                table: "accounts",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_requests_ResultingAccountId",
                table: "account_requests",
                column: "ResultingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_account_requests_ReviewedByUserId",
                table: "account_requests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_account_requests_UserId_Pending",
                table: "account_requests",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_card_requests_AccountId",
                table: "card_requests",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_card_requests_ResultingCardId",
                table: "card_requests",
                column: "ResultingCardId");

            migrationBuilder.CreateIndex(
                name: "IX_card_requests_ReviewedByUserId",
                table: "card_requests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_card_requests_UserId",
                table: "card_requests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_card_requests_UserId_AccountId_Pending",
                table: "card_requests",
                columns: new[] { "UserId", "AccountId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_requests");

            migrationBuilder.DropTable(
                name: "card_requests");

            migrationBuilder.DropIndex(
                name: "IX_accounts_AccountNumber",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "accounts");
        }
    }
}
